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
                action: StartInput,
                keepSubscriberReferenceAlive: true);
            eventAggregator.GetEvent<InputEndedEvent>().Subscribe(
                action: StopInput,
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
                    var current = Active.IsDevice
                        ? settingsService.Contents.Inputs?
                            .SingleOrDefault(i => i.DeviceName == Active.DeviceName)
                        : settingsService.Contents.Inputs?
                            .SingleOrDefault(i => i.FileName == Active.FileName);

                    if (current != default)
                    {
                        current.Rotation = value;
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

            if (settingsService.Contents.Inputs?.Count > 0)
            {
                try
                {
                    var devices = settingsService.Contents.Inputs
                        .Where(i => i.IsDevice
                            && i.IsActive).ToArray();

                    foreach (var device in devices)
                    {
                        var input = default(Input);

                        try
                        {
                            input = GetDevice(device.DeviceName);
                        }
                        catch (DeviceNotFoundException)
                        { }

                        InitializeInput(input);
                    }

                    var files = settingsService.Contents.Inputs
                        .Where(i => !i.IsDevice
                            && i.IsActive).ToArray();

                    foreach (var file in files)
                    {
                        var input = default(Input);

                        try
                        {
                            input = GetFile(file.FileName);
                        }
                        catch (FileNotFoundException)
                        { }

                        InitializeInput(input);
                    }
                }
                catch (MaxCountExceededException)
                { }
            }

            if (Inputs.Count > 0)
            {
                var relevant = Inputs[0];
                Select(relevant);
            }

            isInitializing = false;
        }

        public void Select(Input input)
        {
            if (input == default) return;

            InitializeInput(input);

            if (input != Active || !Active.IsStarted)
            {
                SetActive(input);
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
                        await input.VideoService.StopAsync();
                        await input.VideoService.DisposeAsync();

                        input.VideoService = default;
                    }

                    StopInput(input);
                }
            }
        }

        #endregion Public Methods

        #region Private Methods

        private static void InitializeAreas(Input input)
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

        private static void InitializeTemplates(Input input)
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
            else
            {
                try
                {
                    input.TemplateService.Create();
                }
                catch (MaxCountExceededException)
                { }
            }
        }

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
                    };
                }
            }

            result.Name = deviceName;
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
                    result = new Input
                    {
                        FileName = fileName,
                        IsDevice = false,
                    };
                }
            }

            result.Name = Path.GetFileNameWithoutExtension(fileName);

            return result;
        }

        private void InitializeInput(Input input)
        {
            if (input?.IsStarted != false) return;

            ImmutableList<Input> transformer(ImmutableList<Input> currents) => currents.Contains(input)
                ? currents
                : currents.Add(input);

            ImmutableInterlocked.Update(
                location: ref inputs,
                transformer: transformer);

            _ = InitializeInputAsync(input);

            InitializeTemplates(input);
            InitializeAreas(input);
        }

        private async Task InitializeInputAsync(Input input)
        {
            if (input?.IsStarted != false) return;

            try
            {
                if (input.VideoService == default)
                {
                    input.VideoService = containerProvider.Resolve<IVideoService>();
                }

                await input.VideoService.RunAsync(input);
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

            settingsService.Contents.Inputs = Inputs.ToList();
            settingsService.Save();
        }

        private void SaveTemplates()
        {
            if (isInitializing || Active == default) return;

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
            Active = input
                ?? Inputs.FirstOrDefault(i => i.IsActive);

            inputSelectedEvent.Publish(Active);

            SaveInputs();
        }

        private void StartInput(Input input)
        {
            input.IsActive = true;

            SaveInputs();
        }

        private void StopInput(Input input)
        {
            if (input == default) return;

            input.IsActive = false;

            if (input == Active)
            {
                Active = default;

                SetActive();
            }
            else
            {
                SaveInputs();
            }
        }

        #endregion Private Methods
    }
}