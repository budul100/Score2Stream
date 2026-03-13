using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MsBox.Avalonia.Enums;
using Prism.Events;
using Prism.Ioc;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Events.Area;
using Score2Stream.Commons.Events.Input;
using Score2Stream.Commons.Events.Sample;
using Score2Stream.Commons.Events.Template;
using Score2Stream.Commons.Exceptions;
using Score2Stream.Commons.Extensions;
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
        private readonly IDeviceEnumerator deviceEnumerator;
        private readonly IDialogService dialogService;
        private readonly InputSelectedEvent inputSelectedEvent;
        private readonly ILogger<Service> logger;
        private readonly ISettingsService<Session> settingsService;

        private ImmutableList<Input> inputs = [];
        private bool isInitializing;

        #endregion Private Fields

        #region Public Constructors

        public Service(ISettingsService<Session> settingsService, IDialogService dialogService,
            IDeviceEnumerator deviceEnumerator, IContainerProvider containerProvider,
            IEventAggregator eventAggregator, ILogger<Service> logger = default)
        {
            this.settingsService = settingsService;
            this.dialogService = dialogService;
            this.deviceEnumerator = deviceEnumerator;
            this.containerProvider = containerProvider;
            this.logger = logger;

            inputSelectedEvent = eventAggregator.GetEvent<InputSelectedEvent>();

            eventAggregator.GetEvent<InputStartedEvent>().Subscribe(
                action: _ => SaveInputs(),
                keepSubscriberReferenceAlive: true);
            eventAggregator.GetEvent<InputEndedEvent>().Subscribe(
                action: _ => SaveInputs(),
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
                            .SingleOrDefault(i => i.DeviceName == Active.DeviceName)?.Rotation ?? 0
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
                            .SingleOrDefault(i => i.DeviceName == Active.DeviceName).Rotation = value;
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

        public IReadOnlyDictionary<int, string> GetDevices()
        {
            return deviceEnumerator.GetVideoDevices()
                .Where(d => !string.IsNullOrWhiteSpace(d.Value))
                .ToDictionary(d => d.Key, d => d.Value);
        }

        public void Initialize()
        {
            isInitializing = true;

            InitializeInputs();
            InitialzeAreas();
            InitialzeTemplates();

            if (Inputs.Count > 0)
            {
                var relevant = Inputs[0];
                Select(relevant);
            }

            isInitializing = false;

            SaveAreas();
            SaveTemplates();
        }

        public void Select(Input input)
        {
            if (input != default)
            {
                ImmutableList<Input> transformer(ImmutableList<Input> i) => i.Contains(input)
                    ? i
                    : i.Add(input);

                ImmutableInterlocked.Update(
                    location: ref inputs,
                    transformer: transformer);

                _ = RunAsync(input);

                if (input != Active)
                {
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

                    SetActive(input);

                    SaveInputs();
                }
            }
        }

        public void SelectDevice(string deviceName)
        {
            var input = GetDevice(deviceName);

            Select(input);
        }

        public void SelectFile(string fileName)
        {
            var input = GetFile(fileName);

            Select(input);
        }

        public async Task StopAsync(Input input = default)
        {
            input ??= Active;

            if (input != default)
            {
                var result = await dialogService.GetMessageBoxResultAsync(
                    contentMessage: $"Shall {input.Name} be stopped?",
                    contentTitle: "Stop input");

                if (result == ButtonResult.Yes)
                {
                    if (input.VideoService != default)
                    {
                        input.VideoService.Stop();
                        input.VideoService.Dispose();
                        input.VideoService = default;
                    }

                    if (input == Active)
                    {
                        SetActive();
                    }

                    inputSelectedEvent.Publish(Active);

                    SaveInputs();
                }
            }
        }

        #endregion Public Methods

        #region Private Methods

        private Input GetDevice(string deviceName)
        {
            var devices = GetDevices();

            if (!devices.Values.Contains(deviceName))
            {
                throw new DeviceNotFoundException(deviceName);
            }

            var result = Inputs?
                .FirstOrDefault(i => i.IsDevice
                    && i.DeviceName == deviceName);

            if (result == default)
            {
                if (Inputs.Count >= Constants.MaxCountInputs)
                {
                    throw new MaxCountExceededException(
                        type: typeof(Input),
                        maxCount: Constants.MaxCountInputs);
                }

                result = settingsService.Contents.Inputs?
                    .FirstOrDefault(i => i.IsDevice
                        && i.DeviceName == deviceName);

                if (result == default)
                {
                    result = new Input
                    {
                        DeviceName = deviceName,
                        IsDevice = true,
                        Name = deviceName,
                    };
                }
            }

            result.DeviceId = devices
                .First(d => d.Value == deviceName).Key;

            return result;
        }

        private Input GetFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException(fileName);
            }

            var result = Inputs?
                .FirstOrDefault(i => !i.IsDevice
                    && i.FileName == fileName);

            if (result == default)
            {
                if (Inputs.Count >= Constants.MaxCountInputs)
                {
                    throw new MaxCountExceededException(
                        type: typeof(Input),
                        maxCount: Constants.MaxCountInputs);
                }

                result = settingsService.Contents.Inputs?
                    .FirstOrDefault(i => !i.IsDevice
                        && i.FileName == fileName);

                if (result == default)
                {
                    var name = Path.GetFileNameWithoutExtension(fileName);

                    result = new Input
                    {
                        FileName = fileName,
                        IsDevice = false,
                        Name = name,
                    };
                }
            }

            return result;
        }

        private void InitializeInputs()
        {
            if (settingsService.Contents.Inputs?.Count > 0)
            {
                try
                {
                    var devices = settingsService.Contents.Inputs
                        .Where(i => i.IsDevice
                            && !i.IsEnded).ToArray();

                    foreach (var device in devices)
                    {
                        var input = default(Input);

                        try
                        {
                            input = GetDevice(device.DeviceName);
                        }
                        catch (DeviceNotFoundException)
                        { }

                        Select(input);
                    }

                    var files = settingsService.Contents.Inputs
                        .Where(i => !i.IsDevice
                            && !i.IsEnded).ToArray();

                    foreach (var file in files)
                    {
                        var input = default(Input);

                        try
                        {
                            input = GetFile(file.FileName);
                        }
                        catch (FileNotFoundException)
                        { }

                        Select(input);
                    }
                }
                catch (MaxCountExceededException)
                { }
            }
        }

        private void InitialzeAreas()
        {
            foreach (var input in Inputs)
            {
                if (input?.Areas?.Count > 0)
                {
                    foreach (var area in input.Areas.ToArray())
                    {
                        try
                        {
                            input.AreaService.Add(area);

                            area.Template = input.TemplateService.Templates?
                                .FirstOrDefault(t => t.Name == area.TemplateName
                                    && t.Samples?.Count > 0);
                        }
                        catch (MaxCountExceededException)
                        { }
                    }
                }
            }
        }

        private void InitialzeTemplates()
        {
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
            }
        }

        private async Task RunAsync(Input input)
        {
            if (input == default) return;

            try
            {
                if (!input.IsActive)
                {
                    if (input.VideoService == default)
                    {
                        input.VideoService = containerProvider.Resolve<IVideoService>();
                    }

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
                .Where(i => !i.IsEnded).ToList();

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

        private void SetActive(Input input = default)
        {
            if (input == default)
            {
                input = Inputs
                    .FirstOrDefault(i => !i.IsEnded);
            }

            Active = input;

            inputSelectedEvent.Publish(Active);
        }

        #endregion Private Methods
    }
}