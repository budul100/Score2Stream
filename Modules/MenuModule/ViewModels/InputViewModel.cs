using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Avalonia.Platform.Storage;
using AvaloniaUI.Ribbon;
using MsBox.Avalonia.Enums;
using Prism.Commands;
using Prism.Events;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Events.Area;
using Score2Stream.Commons.Events.Input;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Prism;

namespace Score2Stream.MenuModule.ViewModels
{
    public partial class MenuViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private IDialogService dialogService;
        private IInputService inputService;
        private ILogger<MenuViewModel> logger;
        private IStorageFolder startLocation;
        private Task startLocationTask;

        #endregion Private Fields

        #region Public Properties

        public static int QueueSizeMax => Constants.ImageQueueSizeMax;

        public static int QueueSizeMin => Constants.ImageQueueSizeMin;

        public static string TabInputs => Constants.TabInputs;

        public DelegateCommand<string> AreaAddCommand { get; private set; }

        public DelegateCommand AreaOrderAllCommand { get; private set; }

        public DelegateCommand AreaRemoveAllCommand { get; private set; }

        public DelegateCommand AreaRemoveCommand { get; private set; }

        public DelegateCommand AreaUndoCommand { get; private set; }

        public int DelayProcessing
        {
            get { return settingsService.Contents.Video.DelayProcessing; }
            set
            {
                if (value >= 0
                    && value <= DelayMax
                    && settingsService.Contents.Video.DelayProcessing != value)
                {
                    settingsService.Contents.Video.DelayProcessing = value;
                    settingsService.Save();

                    RaisePropertyChanged(nameof(DelayProcessing));
                }
            }
        }

        public int ImagesQueueSize
        {
            get { return settingsService.Contents.Video.ImagesQueueSize; }
            set
            {
                if (value >= QueueSizeMin
                    && value <= QueueSizeMax
                    && value != settingsService.Contents.Video.ImagesQueueSize)
                {
                    settingsService.Contents.Video.ImagesQueueSize = value;
                    settingsService.Save();

                    RaisePropertyChanged(nameof(ImagesQueueSize));
                }
            }
        }

        public DelegateCommand InputCenterCommand { get; private set; }

        public DelegateCommand InputMoveDownCommand { get; private set; }

        public DelegateCommand InputMoveLeftCommand { get; private set; }

        public DelegateCommand InputMoveRightCommand { get; private set; }

        public DelegateCommand InputMoveUpCommand { get; private set; }

        public DelegateCommand InputRefreshCommand { get; private set; }

        public DelegateCommand InputRotateLeftCommand { get; private set; }

        public DelegateCommand InputRotateRightCommand { get; private set; }

        public ObservableCollection<RibbonDropDownItem> Inputs { get; } = [];

        public DelegateCommand<string> InputSelectCommand { get; private set; }

        public bool IsActive => inputService.IsActive;

        public bool NoCropping
        {
            get { return settingsService.Contents.Video.NoCropping; }
            set
            {
                if (settingsService.Contents.Video.NoCropping != value)
                {
                    settingsService.Contents.Video.NoCropping = value;
                    settingsService.Save();

                    RaisePropertyChanged(nameof(NoCropping));
                }
            }
        }

        public int ThresholdMatching
        {
            get { return settingsService.Contents.Detection.ThresholdMatching; }
            set
            {
                if (value >= 0
                    && value <= ThresholdMax
                    && settingsService.Contents.Detection.ThresholdMatching != value)
                {
                    settingsService.Contents.Detection.ThresholdMatching = value;
                    settingsService.Save();

                    RaisePropertyChanged(nameof(ThresholdMatching));
                }
            }
        }

        #endregion Public Properties

        #region Private Methods

        private void AddArea(string numberSegments)
        {
            if (inputService.AreaService != default
                && int.TryParse(
                    s: numberSegments,
                    result: out var size)
                && size >= Constants.SegmentsCountMin
                && size <= Constants.SegmentsCountMax)
            {
                inputService.AreaService.Create(size);
            }
        }

        private bool CanMoveDown()
        {
            var result = inputService.IsActive
                && inputService.OffsetY >= Constants.OffsetYMin;

            return result;
        }

        private bool CanMoveLeft()
        {
            var result = inputService.IsActive
                && inputService.OffsetX >= Constants.OffsetXMin;

            return result;
        }

        private bool CanMoveRight()
        {
            var result = inputService.IsActive
                && inputService.OffsetX <= Constants.OffsetXMax;

            return result;
        }

        private bool CanMoveUp()
        {
            var result = inputService.IsActive
                && inputService.OffsetY <= Constants.OffsetYMax;

            return result;
        }

        private bool CanRotateLeft()
        {
            var result = inputService.IsActive
                && inputService.Rotation >= Constants.RotateLeftMax;

            return result;
        }

        private bool CanRotateRight()
        {
            var result = inputService.IsActive
                && inputService.Rotation <= Constants.RotateRightMax;

            return result;
        }

        private void ChangeInputRotate(bool toLeft)
        {
            if (toLeft)
            {
                if (CanRotateLeft())
                {
                    inputService.Rotation -= Constants.RotateStep;
                }
            }
            else
            {
                if (CanRotateRight())
                {
                    inputService.Rotation += Constants.RotateStep;
                }
            }
        }

        private void ChangeOffsetX(bool toLeft)
        {
            if (toLeft)
            {
                if (CanMoveLeft())
                {
                    inputService.OffsetX -= Constants.OffsetStep;
                }
            }
            else
            {
                if (CanMoveRight())
                {
                    inputService.OffsetX += Constants.OffsetStep;
                }
            }
        }

        private void ChangeOffsetY(bool toUp)
        {
            if (toUp)
            {
                if (CanMoveUp())
                {
                    inputService.OffsetY -= Constants.OffsetStep;
                }
            }
            else
            {
                if (CanMoveDown())
                {
                    inputService.OffsetY += Constants.OffsetStep;
                }
            }
        }

        private async Task<string> GetInputFileAsync()
        {
            if (Inputs.Count >= Constants.MaxCountInputs)
            {
                await dialogService.ShowMessageBoxAsync(
                    contentMessage: $"Maximum number of {Constants.MaxCountInputs} inputs is exceeded.",
                    contentTitle: "Maximum inputs exceeded",
                    icon: Icon.Error);

                return default;
            }

            if (startLocationTask != null)
            {
                await startLocationTask;
            }

            var paths = await dialogService.OpenFilePickerAsync(
                title: Texts.MenuInputFileText,
                allowMultiple: false,
                startLocation: startLocation);

            if (paths?.Any() != true) return default;

            var result = paths
                .Select(p => p.Path.LocalPath)
                .FirstOrDefault(File.Exists);

            if (string.IsNullOrWhiteSpace(result)) return default;

            startLocation = await dialogService.GetFolderAsync(result);

            settingsService.Contents.Video.FilePathVideo = result;
            settingsService.Save();

            return result;
        }

        private async Task InitializeStartLocationAsync()
        {
            try
            {
                var filePathVideo = settingsService.Contents.Video.FilePathVideo;

                startLocation = await dialogService.GetFolderAsync(filePathVideo);
            }
            catch { }
        }

        private partial void InitializeViewInput(IInputService inputService, IDialogService dialogService,
            IEventAggregator eventAggregator, ILogger<MenuViewModel> logger)
        {
            this.inputService = inputService;
            this.dialogService = dialogService;
            this.logger = logger;

            startLocationTask = InitializeStartLocationAsync();

            this.InputRefreshCommand = new DelegateCommand(
                executeMethod: RefreshInputs);
            this.InputSelectCommand = new DelegateCommand<string>(
                executeMethod: async param => await SelectInputAsync(param));

            this.InputCenterCommand = new DelegateCommand(
                executeMethod: () => eventAggregator.GetEvent<InputCenteringEvent>().Publish(),
                canExecuteMethod: () => inputService.IsActive);

            this.InputMoveLeftCommand = new DelegateCommand(
                executeMethod: () => ChangeOffsetX(true),
                canExecuteMethod: CanMoveLeft);
            this.InputMoveRightCommand = new DelegateCommand(
                executeMethod: () => ChangeOffsetX(false),
                canExecuteMethod: CanMoveRight);
            this.InputMoveUpCommand = new DelegateCommand(
                executeMethod: () => ChangeOffsetY(true),
                canExecuteMethod: CanMoveUp);
            this.InputMoveDownCommand = new DelegateCommand(
                executeMethod: () => ChangeOffsetY(false),
                canExecuteMethod: CanMoveDown);
            this.InputRotateLeftCommand = new DelegateCommand(
                executeMethod: () => ChangeInputRotate(true),
                canExecuteMethod: CanRotateLeft);
            this.InputRotateRightCommand = new DelegateCommand(
                executeMethod: () => ChangeInputRotate(false),
                canExecuteMethod: CanRotateRight);

            this.AreaAddCommand = new DelegateCommand<string>(
                executeMethod: AddArea,
                canExecuteMethod: _ => inputService.IsActive);
            this.AreaRemoveCommand = new DelegateCommand(
                executeMethod: () => inputService.AreaService?.RemoveAsync(),
                canExecuteMethod: () => inputService.AreaService?.Active != default);
            this.AreaRemoveAllCommand = new DelegateCommand(
                executeMethod: () => inputService.AreaService?.ClearAsync(),
                canExecuteMethod: () => inputService.AreaService?.Areas?.Count > 0);
            this.AreaUndoCommand = new DelegateCommand(
                executeMethod: () => inputService.AreaService?.Undo(),
                canExecuteMethod: () => inputService.AreaService?.CanUndo == true);
            this.AreaOrderAllCommand = new DelegateCommand(
                executeMethod: () => inputService.AreaService?.Order(true),
                canExecuteMethod: () => inputService.AreaService?.Areas?.Count > 0);

            eventAggregator.GetEvent<InputSelectedEvent>().Subscribe(
                action: _ => OnInputChanged());
            eventAggregator.GetEvent<InputStartedEvent>().Subscribe(
                action: _ => OnInputChanged());

            eventAggregator.GetEvent<AreaSelectedEvent>().Subscribe(
                action: _ => OnAreasChanged());
            eventAggregator.GetEvent<AreasChangedEvent>().Subscribe(
                action: OnAreasChanged);
            eventAggregator.GetEvent<AreaModifiedEvent>().Subscribe(
                action: _ => OnAreasModified());
        }

        private void OnAreasChanged()
        {
            AreaRemoveCommand.RaiseCanExecuteChanged();
            AreaRemoveAllCommand.RaiseCanExecuteChanged();

            AreaOrderAllCommand.RaiseCanExecuteChanged();

            SampleAddCommand.RaiseCanExecuteChanged();
        }

        private void OnAreasModified()
        {
            AreaUndoCommand.RaiseCanExecuteChanged();
            AreaOrderAllCommand.RaiseCanExecuteChanged();
        }

        private void OnInputChanged()
        {
            RaisePropertyChanged(nameof(IsActive));
            RaisePropertyChanged(nameof(IsActiveSample));
            RaisePropertyChanged(nameof(NoCropping));
            RaisePropertyChanged(nameof(DelayProcessing));
            RaisePropertyChanged(nameof(ThresholdDetecting));
            RaisePropertyChanged(nameof(ThresholdMatching));
            RaisePropertyChanged(nameof(WaitingDuration));

            InputCenterCommand.RaiseCanExecuteChanged();

            InputMoveLeftCommand.RaiseCanExecuteChanged();
            InputMoveRightCommand.RaiseCanExecuteChanged();
            InputMoveUpCommand.RaiseCanExecuteChanged();
            InputMoveDownCommand.RaiseCanExecuteChanged();
            InputRotateLeftCommand.RaiseCanExecuteChanged();
            InputRotateRightCommand.RaiseCanExecuteChanged();

            AreaAddCommand.RaiseCanExecuteChanged();
        }

        private void RefreshInputs()
        {
            Inputs.Clear();

            var devices = inputService.GetDevices();

            var ordereds = devices
                .OrderBy(i => i.Value).ToArray();

            foreach (var ordered in ordereds)
            {
                var input = new RibbonDropDownItem
                {
                    Command = InputSelectCommand,
                    CommandParameter = ordered.Value,
                    Text = ordered.Value
                };

                Inputs.Add(input);
            }

            var fileInput = new RibbonDropDownItem
            {
                Command = InputSelectCommand,
                Text = Texts.MenuInputFileText,
            };

            Inputs.Add(fileInput);
        }

        private async Task SelectInputAsync(string deviceName)
        {
            var fileName = default(string);

            try
            {
                if (string.IsNullOrWhiteSpace(deviceName))
                {
                    fileName = await GetInputFileAsync();
                    inputService.SelectFile(fileName);
                }
                else
                {
                    inputService.SelectDevice(deviceName);
                }
            }
            catch (FileNotFoundException exception)
            {
                logger?.LogError(
                    exception: exception,
                    message: "Failed to find file {FileName}.",
                    args: fileName);

                return;
            }
        }

        #endregion Private Methods
    }
}