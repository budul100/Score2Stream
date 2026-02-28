using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Avalonia.Platform.Storage;
using MsBox.Avalonia.Enums;
using Prism.Events;
using Prism.Ioc;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Events.Area;
using Score2Stream.Commons.Events.Input;
using Score2Stream.Commons.Events.Sample;
using Score2Stream.Commons.Events.Template;
using Score2Stream.Commons.Exceptions;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;
using Score2Stream.Commons.Models.Settings;

namespace Score2Stream.InputService
{
    public class Service
        : IInputService
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private readonly IDialogService dialogService;
        private readonly IInputEnumerator inputEnumerator;
        private readonly InputsChangedEvent inputsChangedEvent;
        private readonly InputSelectedEvent inputSelectedEvent;
        private readonly ILogger<Service> logger;
        private readonly ISettingsService<Session> settingsService;

        private ImmutableList<Input> inputs = [];
        private bool isInitializing;
        private IStorageFolder startLocation;
        private Task startLocationTask;

        #endregion Private Fields

        #region Public Constructors

        public Service(ISettingsService<Session> settingsService, IDialogService dialogService,
            IContainerProvider containerProvider, IEventAggregator eventAggregator,
            IInputEnumerator inputEnumerator, ILogger<Service> logger = default)
        {
            this.settingsService = settingsService;
            this.dialogService = dialogService;
            this.containerProvider = containerProvider;
            this.inputEnumerator = inputEnumerator;
            this.logger = logger;

            inputsChangedEvent = eventAggregator.GetEvent<InputsChangedEvent>();
            inputSelectedEvent = eventAggregator.GetEvent<InputSelectedEvent>();

            eventAggregator.GetEvent<InputStartedEvent>().Subscribe(
                action: UpdateInputs,
                keepSubscriberReferenceAlive: true);
            eventAggregator.GetEvent<InputEndedEvent>().Subscribe(
                action: EndInputs,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<AreasChangedEvent>().Subscribe(
                action: SaveAreas,
                keepSubscriberReferenceAlive: true);
            eventAggregator.GetEvent<AreasOrderedEvent>().Subscribe(
                action: SaveAreas,
                keepSubscriberReferenceAlive: true);
            eventAggregator.GetEvent<AreaModifiedEvent>().Subscribe(
                action: _ => SaveAreas(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<TemplatesChangedEvent>().Subscribe(
                action: SaveTemplates,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<SamplesChangedEvent>().Subscribe(
                action: SaveTemplates,
                keepSubscriberReferenceAlive: true);
            eventAggregator.GetEvent<SamplesOrderedEvent>().Subscribe(
                action: SaveTemplates,
                keepSubscriberReferenceAlive: true);
            eventAggregator.GetEvent<SampleModifiedEvent>().Subscribe(
                action: _ => SaveTemplates(),
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public Input Active { get; private set; }

        public IAreaService AreaService => Active?.AreaService;

        public IReadOnlyList<Input> Inputs => inputs;

        public bool IsActive => VideoService?.IsActive ?? false;

        public float Rotation
        {
            get
            {
                var result = 0f;

                if (Active != default)
                {
                    result = Active.IsDevice
                        ? settingsService.Contents.Inputs?
                            .SingleOrDefault(i => i.Name == Active.Name)?.Rotation ?? 0
                        : settingsService.Contents.Inputs?
                            .SingleOrDefault(i => i.FileName == Active.FileName)?.Rotation ?? 0;
                }

                return result;
            }
            set
            {
                if (Active != default)
                {
                    if (Active.IsDevice)
                    {
                        settingsService.Contents.Inputs
                            .SingleOrDefault(i => i.Name == Active.Name).Rotation = value;
                    }
                    else
                    {
                        settingsService.Contents.Inputs
                            .SingleOrDefault(i => i.FileName == Active.FileName).Rotation = value;
                    }

                    settingsService.Save();
                }
            }
        }

        public ISampleService SampleService => TemplateService?.Template?.SampleService;

        public ITemplateService TemplateService => Active?.TemplateService;

        public IVideoService VideoService => Active?.VideoService;

        #endregion Public Properties

        #region Public Methods

        public void Initialize()
        {
            isInitializing = true;

            startLocationTask = InitializeStartLocationAsync();

            RefreshInputs();

            foreach (var input in Inputs)
            {
                if (input?.Templates?.Count > 0)
                {
                    foreach (var template in input.Templates.ToArray())
                    {
                        try
                        {
                            input.TemplateService.Add(template);
                        }
                        catch (MaxCountExceededException)
                        { }
                    }
                }

                if (input?.Areas?.Count > 0)
                {
                    foreach (var area in input.Areas.ToArray())
                    {
                        area.Template = input.TemplateService.Templates?
                            .FirstOrDefault(t => t.Name == area.TemplateName
                                && t.Samples?.Count > 0);

                        try
                        {
                            input.AreaService.Add(area);
                        }
                        catch (MaxCountExceededException)
                        { }
                    }
                }
            }

            var relevant = Inputs.FirstOrDefault(i => i != default
                && settingsService.Contents.Inputs?.Contains(i) == true);

            SelectInput(relevant);

            isInitializing = false;

            SaveAreas();
            SaveTemplates();

            inputsChangedEvent.Publish();
        }

        public async Task SelectAsync(Input input)
        {
            if (input == null
                || (!input.IsDevice && (!input.IsActive || !File.Exists(input.FileName))))
            {
                try
                {
                    if (startLocationTask != null)
                    {
                        await startLocationTask;
                    }

                    input = await GetInputAsync();
                }
                catch (MaxCountExceededException exception)
                {
                    await dialogService.ShowMessageBoxAsync(
                        contentMessage: exception.Message,
                        contentTitle: "Maximum count exceeded",
                        icon: Icon.Error);

                    return;
                }
            }

            SelectInput(input);
        }

        public async Task StopAsync()
        {
            if (Active != default)
            {
                var result = await dialogService.GetMessageBoxResultAsync(
                    contentMessage: $"Shall {Active.Name} be stopped?",
                    contentTitle: "Stop input");

                if (result == ButtonResult.Yes)
                {
                    Active.VideoService.Stop();

                    SaveInputs();
                }
            }
        }

        public void Update()
        {
            RefreshDevices();
        }

        #endregion Public Methods

        #region Private Methods

        private void EndInputs()
        {
            Active = default;

            UpdateInputs();
        }

        private Input GetInput(string fileName)
        {
            if (!File.Exists(fileName)) return default;

            if (Inputs.Count >= Constants.MaxCountInputs)
            {
                throw new MaxCountExceededException(
                    type: typeof(Input),
                    maxCount: Constants.MaxCountInputs);
            }

            var result = Inputs?.FirstOrDefault(i => i != default
                && !i.IsDevice
                && !i.IsEnded
                && i.FileName == fileName);

            if (result == default)
            {
                result = settingsService.Contents.Inputs?.FirstOrDefault(i => i != default
                    && !i.IsDevice
                    && !i.IsEnded
                    && i.FileName == fileName);

                if (result != default)
                {
                    ImmutableInterlocked.Update(
                        location: ref inputs,
                        transformer: l => l.Add(result));
                }
            }

            if (result == default)
            {
                result = new Input(false)
                {
                    FileName = fileName,
                    Name = Path.GetFileName(fileName),
                };

                if (result != default)
                {
                    ImmutableInterlocked.Update(
                        location: ref inputs,
                        transformer: l => l.Add(result));
                }
            }

            result.Guid = Guid.NewGuid();

            return result;
        }

        private Input GetInput(int deviceId, string name)
        {
            if (Inputs.Count >= Constants.MaxCountInputs)
            {
                throw new MaxCountExceededException(
                    type: typeof(Input),
                    maxCount: Constants.MaxCountInputs);
            }

            var result = Inputs?.FirstOrDefault(i => i != default
                && i.IsDevice
                && !i.IsEnded
                && i.Name == name);

            if (result == default)
            {
                result = settingsService.Contents.Inputs?
                    .FirstOrDefault(i => i != default
                        && i.IsDevice
                        && !i.IsEnded
                        && i.Name == name);

                if (result != default)
                {
                    ImmutableInterlocked.Update(
                        location: ref inputs,
                        transformer: l => l.Add(result));
                }
            }

            if (result == default)
            {
                result = new Input(true)
                {
                    DeviceId = deviceId,
                    Name = name,
                };

                if (result != default)
                {
                    ImmutableInterlocked.Update(
                        location: ref inputs,
                        transformer: l => l.Add(result));
                }
            }

            result.Guid = Guid.NewGuid();
            result.DeviceId = deviceId;

            return result;
        }

        private async Task<Input> GetInputAsync()
        {
            if (Inputs.Count >= Constants.MaxCountInputs)
                return default;

            var paths = await dialogService.OpenFilePickerAsync(
                title: Texts.MenuInputFileText,
                allowMultiple: false,
                startLocation: startLocation);

            if (paths?.Any() != true) return default;

            var fileName = paths
                .Select(p => p.Path.LocalPath)
                .FirstOrDefault(File.Exists);

            if (string.IsNullOrWhiteSpace(fileName)) return default;

            startLocation = await dialogService.GetFolderAsync(fileName);

            settingsService.Contents.Video.FilePathVideo = fileName;
            settingsService.Save();

            return GetInput(fileName);
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

        private void RefreshDevices()
        {
            var currentInputs = inputEnumerator.GetDevices()
                .OrderBy(d => d.Value).ToArray();

            var removedInput = Inputs
                .Where(i => (i.IsDevice && !currentInputs.Any(d => d.Value == i.Name))
                    || (!i.IsDevice && i.IsEnded)).ToArray();

            foreach (var removedDevice in removedInput)
            {
                removedDevice?.AreaService?.Clear();
                removedDevice?.VideoService?.Dispose();

                ImmutableInterlocked.Update(
                    location: ref inputs,
                    transformer: l => l.Remove(removedDevice));
            }

            var hasChanges = removedInput.Length > 0;

            foreach (var currentDevice in currentInputs)
            {
                var currentInput = Inputs?.FirstOrDefault(i => i != default
                        && i.IsDevice
                        && !i.IsEnded
                        && i?.Name == currentDevice.Value);

                if (currentInput == default)
                {
                    GetInput(
                        deviceId: currentDevice.Key,
                        name: currentDevice.Value);

                    hasChanges = true;
                }
                else if (currentInput.DeviceId != currentDevice.Key)
                {
                    currentInput.DeviceId = currentDevice.Key;

                    hasChanges = true;
                }
            }

            if (Active != default
                && !Inputs.Contains(Active))
            {
                Active = Inputs.FirstOrDefault(i => i.IsActive);

                hasChanges = true;
            }

            if (hasChanges)
            {
                inputsChangedEvent.Publish();
            }
        }

        private void RefreshInputs()
        {
            RefreshDevices();

            if (settingsService.Contents.Inputs?.Count > 0)
            {
                var devices = Inputs
                    .Where(i => i != default
                        && i.IsDevice
                        && !i.IsEnded
                        && settingsService.Contents.Inputs.Any(s => s.Name == i.Name)).ToArray();

                foreach (var device in devices)
                {
                    _ = RunInputAsync(device);
                }

                var files = settingsService.Contents.Inputs
                    .Where(i => i?.IsDevice == false)
                    .Select(i => i.FileName).ToArray();

                try
                {
                    foreach (var file in files)
                    {
                        var input = GetInput(file);

                        if (input != default)
                        {
                            _ = RunInputAsync(input);
                        }
                    }
                }
                catch (MaxCountExceededException)
                { }
            }
        }

        private async Task RunInputAsync(Input input)
        {
            if (input == default) return;

            try
            {
                if (input.VideoService == default)
                {
                    input.VideoService = containerProvider.Resolve<IVideoService>();
                }

                if (!input.IsActive)
                {
                    await input.VideoService.RunAsync(input);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(
                    exception: ex,
                    message: "Failed to run input {Name}.",
                    args: input.Name);
            }
        }

        private void SaveAreas()
        {
            if (isInitializing || Active == default) return;

            Active.Areas = AreaService?.Areas;

            settingsService.Save();
        }

        private void SaveInputs()
        {
            if (isInitializing) return;

            var inputs = Inputs
                .Where(i => i.IsActive && !i.IsEnded).ToList();

            var settings = settingsService.Contents.Inputs;

            var hasChanges = settings == null
                || settings.Count != inputs.Count
                || !inputs.All(settings.Contains);

            if (hasChanges)
            {
                settingsService.Contents.Inputs = inputs;
                settingsService.Save();
            }
        }

        private void SaveTemplates()
        {
            if (isInitializing || Active == default)
                return;

            Active.Templates = TemplateService?.Templates;

            if (Active.Templates?.Count > 0)
            {
                foreach (var template in Active.Templates)
                {
                    var dirties = template.Samples?
                        .Where(s => s.Mat != default
                            && s.Image == null).ToArray();

                    if (dirties?.Length > 0)
                    {
                        foreach (var dirty in dirties)
                        {
                            dirty.Image = dirty.Mat.ToBytes();
                        }
                    }
                }
            }

            settingsService.Save();
        }

        private void SelectInput(Input input)
        {
            if (input != default)
            {
                _ = RunInputAsync(input);

                if (input != Active)
                {
                    Active = input;

                    if (TemplateService != default)
                    {
                        if (!(TemplateService.Templates?.Count > 0))
                        {
                            try
                            {
                                TemplateService.Create();
                            }
                            catch (MaxCountExceededException)
                            { }
                        }

                        TemplateService.Select(TemplateService.Templates?.FirstOrDefault());
                    }

                    inputSelectedEvent.Publish(Active);

                    SaveInputs();
                }
            }
        }

        private void UpdateInputs()
        {
            RefreshDevices();
            SaveInputs();

            inputsChangedEvent.Publish();
        }

        #endregion Private Methods
    }
}