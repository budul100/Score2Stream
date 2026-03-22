using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MsBox.Avalonia.Enums;
using OpenCvSharp;
using Prism.Events;
using Prism.Ioc;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Events.Area;
using Score2Stream.Commons.Events.Input;
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

        private readonly AreasChangedEvent areasChangedEvent;
        private readonly IContainerProvider containerProvider;
        private readonly IDeviceEnumerator deviceEnumerator;
        private readonly IDialogService dialogService;
        private readonly InputSelectedEvent inputSelectedEvent;
        private readonly ILogger<Service> logger;
        private readonly ISettingsService<Session> settingsService;
        private readonly ITemplateService templateService;

        private ImmutableList<Input> inputs = [];
        private bool isInitializing;

        #endregion Private Fields

        #region Public Constructors

        public Service(ISettingsService<Session> settingsService, IDialogService dialogService,
            ITemplateService templateService, IDeviceEnumerator deviceEnumerator,
            IContainerProvider containerProvider, IEventAggregator eventAggregator,
            ILogger<Service> logger = default)
        {
            this.settingsService = settingsService;
            this.dialogService = dialogService;
            this.templateService = templateService;
            this.deviceEnumerator = deviceEnumerator;
            this.containerProvider = containerProvider;
            this.logger = logger;

            inputSelectedEvent = eventAggregator.GetEvent<InputSelectedEvent>();
            areasChangedEvent = eventAggregator.GetEvent<AreasChangedEvent>();

            eventAggregator.GetEvent<InputStartedEvent>().Subscribe(
                action: ActivateInput,
                keepSubscriberReferenceAlive: true);
            eventAggregator.GetEvent<InputEndedEvent>().Subscribe(
                action: RemoveInput,
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
                            input = CreateFromDevice(device.DeviceName);
                        }
                        catch (DeviceNotFoundException)
                        { }

                        AddInput(input);
                    }

                    var files = settingsService.Contents.Inputs
                        .Where(i => !i.IsDevice
                            && i.IsActive).ToArray();

                    foreach (var file in files)
                    {
                        var input = default(Input);

                        try
                        {
                            input = CreateFromFile(file.FileName);
                        }
                        catch (FileNotFoundException)
                        { }

                        AddInput(input);
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

        public async Task RemoveAsync(Input input = default)
        {
            input ??= Active;

            if (input != default)
            {
                var result = await dialogService.GetMessageBoxResultAsync(
                    contentMessage: $"Shall {input.Name} be removed?",
                    contentTitle: "Remove input");

                if (result == ButtonResult.Yes)
                {
                    if (input.VideoService != default)
                    {
                        await input.VideoService.StopAsync();
                        await input.VideoService.DisposeAsync();

                        input.VideoService = default;
                    }

                    RemoveInput(input);
                }
            }
        }

        public void Select(Input input)
        {
            if (input == Active
                && Active.IsStarted) return;

            Active = input
                ?? inputs.FirstOrDefault(i => i.IsActive);

            inputSelectedEvent.Publish(Active);
        }

        public void SelectDevice(string deviceName)
        {
            try
            {
                var input = CreateFromDevice(deviceName);

                AddInput(input);

                Select(input);
            }
            catch (MaxCountExceededException exception)
            {
                dialogService.ShowMessageBoxAsync(
                    contentMessage: exception.Message,
                    contentTitle: "Maximum count exceeded",
                    icon: Icon.Error);
            }
        }

        public void SelectFile(string fileName)
        {
            try
            {
                var input = CreateFromFile(fileName);

                AddInput(input);

                Select(input);
            }
            catch (MaxCountExceededException exception)
            {
                dialogService.ShowMessageBoxAsync(
                    contentMessage: exception.Message,
                    contentTitle: "Maximum count exceeded",
                    icon: Icon.Error);
            }
        }

        #endregion Public Methods

        #region Private Methods

        private void ActivateInput(Input input)
        {
            input.IsActive = true;

            SaveInputs();
        }

        private void AddInput(Input input)
        {
            if (input?.IsStarted != false) return;

            if (Inputs.Count >= Constants.MaxCountInputs)
            {
                throw new MaxCountExceededException(
                    type: typeof(Input),
                    maxCount: Constants.MaxCountInputs);
            }

            _ = InitializeServiceAsync(input);

            InitializeAreas(input);

            ImmutableList<Input> add(ImmutableList<Input> c) => !c.Contains(input)
                ? c.Add(input)
                : c;

            ImmutableInterlocked.Update(
                location: ref inputs,
                transformer: add);
        }

        private Input CreateFromDevice(string deviceName)
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

        private Input CreateFromFile(string fileName)
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

        private void InitializeAreas(Input input)
        {
            if (input?.Areas?.Count > 0)
            {
                try
                {
                    var areas = input.Areas.ToArray();

                    foreach (var area in areas)
                    {
                        area.Template = templateService.Templates?
                            .SingleOrDefault(t => t.Name == area.TemplateName);

                        input.AreaService.Add(area);
                    }
                }
                catch (MaxCountExceededException)
                { }

                input.AreaService.Order();

                areasChangedEvent.Publish();
            }
        }

        private async Task InitializeServiceAsync(Input input)
        {
            if (input?.IsStarted != false) return;

            try
            {
                if (input.VideoService == default)
                {
                    input.VideoService = containerProvider.Resolve<IVideoService>();

                    input.AreaService.Initialize(input);
                }

                await input.VideoService.RunAsync(input);
            }
            catch (Exception exception)
            {
                logger?.LogError(
                    exception: exception,
                    message: "Failed to run input {Name}.",
                    args: input.Name);
            }
        }

        private void RemoveInput(Input input)
        {
            if (input != default)
            {
                input.IsActive = false;

                if (input == Active)
                {
                    var relevants = Inputs
                        .Where(i => i != input
                            && i.IsActive).ToArray();

                    var next = relevants.Length > 1
                        ? relevants.GetNext(input)
                        : default;

                    Select(next);
                }

                SaveInputs();
            }
        }

        private void SaveInputs()
        {
            if (isInitializing) return;

            settingsService.Contents.Inputs = Inputs?.ToList();
            settingsService.Save();
        }

        #endregion Private Methods
    }
}