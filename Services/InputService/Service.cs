using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Hompus.VideoInputDevices;
using MsBox.Avalonia.Enums;
using Prism.Events;
using Prism.Ioc;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Events.Area;
using Score2Stream.Commons.Events.Input;
using Score2Stream.Commons.Events.Sample;
using Score2Stream.Commons.Events.Template;
using Score2Stream.Commons.Events.Video;
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
        private readonly InputsChangedEvent inputsChangedEvent;
        private readonly InputSelectedEvent inputSelectedEvent;
        private readonly ISettingsService<Session> settingsService;

        private bool isInitializing;
        private IStorageFolder startLocation;

        #endregion Private Fields

        #region Public Constructors

        public Service(ISettingsService<Session> settingsService, IDialogService dialogService,
            IContainerProvider containerProvider, IEventAggregator eventAggregator)
        {
            this.settingsService = settingsService;
            this.dialogService = dialogService;
            this.containerProvider = containerProvider;

            inputsChangedEvent = eventAggregator.GetEvent<InputsChangedEvent>();
            inputSelectedEvent = eventAggregator.GetEvent<InputSelectedEvent>();

            eventAggregator.GetEvent<VideoStartedEvent>().Subscribe(
                action: OnVideoChanged,
                keepSubscriberReferenceAlive: true);
            eventAggregator.GetEvent<VideoEndedEvent>().Subscribe(
                action: OnVideoChanged,
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

        public HashSet<Input> Inputs { get; } = new HashSet<Input>();

        public bool IsActive => VideoService?.IsActive ?? false;

        public ISampleService SampleService => TemplateService?.Template?.SampleService;

        public ITemplateService TemplateService => Active?.TemplateService;

        public IVideoService VideoService => Active?.VideoService;

        #endregion Public Properties

        #region Public Methods

        public void Initialize()
        {
            isInitializing = true;

            Task.Run(async () => startLocation = await dialogService.GetFolderAsync(settingsService.Contents.Video.FilePathVideo));

            UpdateInputs();

            foreach (var input in Inputs)
            {
                var current = input.IsDevice
                    ? settingsService.Contents.Inputs?.SingleOrDefault(i => i.DeviceId == input.DeviceId)
                    : settingsService.Contents.Inputs?.SingleOrDefault(i => i.FileName == input.FileName);

                if (current?.Templates?.Any() == true)
                {
                    var templates = current.Templates.ToArray();

                    foreach (var template in templates)
                    {
                        try
                        {
                            input.TemplateService.Add(template);
                        }
                        catch (MaxCountExceededException)
                        { }
                    }
                }

                if (current?.Areas?.Any() == true)
                {
                    var areas = current.Areas.ToArray();

                    foreach (var area in areas)
                    {
                        area.Template = input.TemplateService.Templates?
                            .FirstOrDefault(t => t.Name == area.TemplateName
                                && t.Samples?.Any() == true);

                        try
                        {
                            input.AreaService.Add(area);
                        }
                        catch (MaxCountExceededException)
                        { }
                    }
                }
            }

            var relevant = Inputs.FirstOrDefault(i => !i.IsDevice
                || settingsService.Contents.Inputs?.Any(s => s.DeviceId == i.DeviceId) == true);

            SelectInput(relevant);

            isInitializing = false;

            SaveAreas();
            SaveTemplates();

            inputsChangedEvent.Publish();
        }

        public async Task SelectAsync(Input input)
        {
            if (input?.IsDevice != true
                && (input?.IsActive != true || !File.Exists(input?.FileName)))
            {
                try
                {
                    input = await GetInputAsync();
                }
                catch (MaxCountExceededException exception)
                {
                    await dialogService.ShowMessageBoxAsync(
                        contentMessage: exception.Message,
                        contentTitle: "Maximum count exceeded",
                        icon: Icon.Error);
                }
            }

            SelectInput(input);
        }

        public async Task StopAsync()
        {
            var relevants = Inputs
                .Where(i => i.IsActive).ToArray();

            if (relevants.Any())
            {
                var result = await dialogService.GetMessageBoxResultAsync(
                    contentMessage: "Shall all inputs be stopped?",
                    contentTitle: "Stop inputs");

                if (result == ButtonResult.Yes)
                {
                    foreach (var relevant in relevants)
                    {
                        relevant.VideoService.Stop();
                    }

                    SaveInputs();
                }
            }
        }

        public void Update()
        {
            UpdateDevices();
        }

        #endregion Public Methods

        #region Private Methods

        private Input GetInput(string fileName)
        {
            var result = default(Input);

            if (File.Exists(fileName))
            {
                if (Inputs.Count >= Constants.MaxCountInputs)
                {
                    throw new MaxCountExceededException(
                        type: typeof(Input),
                        maxCount: Constants.MaxCountInputs);
                }

                result = Inputs.SingleOrDefault(i => i.FileName == fileName);

                if (result == default)
                {
                    result = new Input(false)
                    {
                        FileName = fileName,
                        Guid = Guid.NewGuid(),
                        Name = Path.GetFileName(fileName),
                    };

                    Inputs.Add(result);
                }
            }

            return result;
        }

        private Input GetInput(int deviceId, string name)
        {
            var result = Inputs
                .SingleOrDefault(i => i.DeviceId == deviceId);

            if (result == default)
            {
                result = new Input(true)
                {
                    DeviceId = deviceId,
                    Guid = Guid.NewGuid(),
                    Name = name,
                };

                Inputs.Add(result);
            }

            return result;
        }

        private async Task<Input> GetInputAsync()
        {
            var result = default(Input);

            if (Inputs.Count < Constants.MaxCountInputs)
            {
                var paths = await dialogService.OpenFilePickerAsync(
                    title: Texts.MenuInputFileText,
                    allowMultiple: false,
                    startLocation: startLocation);

                if (paths?.Any() == true)
                {
                    var fileName = paths
                        .Select(p => p.Path.LocalPath)
                        .FirstOrDefault(p => File.Exists(p));

                    if (!string.IsNullOrWhiteSpace(fileName))
                    {
                        startLocation = await dialogService.GetFolderAsync(fileName);

                        settingsService.Contents.Video.FilePathVideo = fileName;
                        settingsService.Save();

                        result = GetInput(fileName);
                    }
                }
            }

            return result;
        }

        private void OnVideoChanged()
        {
            UpdateDevices();
            SaveInputs();

            inputsChangedEvent.Publish();
        }

        private void RunInput(Input input)
        {
            if (input != default)
            {
                if (input.VideoService == default)
                {
                    input.VideoService = containerProvider
                        .Resolve<IVideoService>();
                }

                if (!input.IsActive)
                {
                    input.VideoService.RunAsync(input);
                }
            }
        }

        private void SaveAreas()
        {
            if (!isInitializing
                && Active != default)
            {
                Active.Areas = AreaService?.Areas;

                settingsService.Save();
            }
        }

        private void SaveInputs()
        {
            if (!isInitializing)
            {
                var inputs = Inputs
                    .Where(i => i.IsActive
                        && !i.IsEnded).ToList();

                if (settingsService.Contents.Inputs != inputs)
                {
                    settingsService.Contents.Inputs = inputs;

                    settingsService.Save();
                }
            }
        }

        private void SaveTemplates()
        {
            if (!isInitializing
                && Active != default)
            {
                Active.Templates = TemplateService?.Templates;

                if (Active.Templates?.Any() == true)
                {
                    foreach (var template in Active.Templates)
                    {
                        var relevants = template.Samples?
                            .Where(s => s.Mat != default).ToArray();

                        if (relevants?.Any() == true)
                        {
                            foreach (var relevant in relevants)
                            {
                                relevant.Image = relevant.Mat.ToBytes();
                            }
                        }
                    }
                }

                settingsService.Save();
            }
        }

        private void SelectInput(Input input)
        {
            if (input != default)
            {
                RunInput(input);

                if (input != Active)
                {
                    Active = input;

                    if (TemplateService != default)
                    {
                        if (TemplateService.Templates?.Any() != true)
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

        private void UpdateDevices()
        {
            using var deviceEnumerator = new SystemDeviceEnumerator();

            var devices = deviceEnumerator.ListVideoInputDevice()
                .OrderBy(d => d.Value).ToArray();

            var toBeRemoveds = Inputs
                .Where(i => i.IsEnded
                    || (i.IsDevice && !devices.Any(d => d.Key == i.DeviceId))).ToArray();

            foreach (var toBeRemoved in toBeRemoveds)
            {
                toBeRemoved.AreaService.Clear();
                toBeRemoved.VideoService?.Dispose();
                Inputs.Remove(toBeRemoved);
            }

            var toBeAddeds = devices
                .Where(d => !Inputs.Any(i => d.Key == i.DeviceId)).ToArray();

            foreach (var toBeAdded in toBeAddeds)
            {
                GetInput(
                    deviceId: toBeAdded.Key,
                    name: toBeAdded.Value);
            }

            if (toBeRemoveds.Any() || toBeAddeds.Any())
            {
                inputsChangedEvent.Publish();
            }
        }

        private void UpdateInputs()
        {
            UpdateDevices();

            if (settingsService.Contents.Inputs?.Any() == true)
            {
                var devices = Inputs
                    .Where(i => i.IsDevice)
                    .Where(d => settingsService.Contents.Inputs.Any(i => i.DeviceId == d.DeviceId)).ToArray();

                foreach (var device in devices)
                {
                    RunInput(device);
                }

                var files = settingsService.Contents.Inputs
                    .Where(i => !i.IsDevice)
                    .Select(i => i.FileName).ToArray();

                try
                {
                    foreach (var file in files)
                    {
                        var input = GetInput(file);

                        RunInput(input);
                    }
                }
                catch (MaxCountExceededException)
                { }
            }
        }

        #endregion Private Methods
    }
}