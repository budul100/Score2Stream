using Hompus.VideoInputDevices;
using Prism.Events;
using Prism.Ioc;
using Score2Stream.Core.Constants;
using Score2Stream.Core.Events.Input;
using Score2Stream.Core.Events.Video;
using Score2Stream.Core.Interfaces;
using Score2Stream.Core.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Score2Stream.InputService
{
    public class Service
        : IInputService
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private readonly IEventAggregator eventAggregator;
        private readonly UserSettings settings;
        private readonly ISettingsService<UserSettings> settingsService;

        private Core.Models.Input currentInput;

        #endregion Private Fields

        #region Public Constructors

        public Service(ISettingsService<UserSettings> settingsService, IContainerProvider containerProvider,
            IEventAggregator eventAggregator)
        {
            this.settingsService = settingsService;
            this.containerProvider = containerProvider;
            this.eventAggregator = eventAggregator;

            this.settings = settingsService.Get();

            eventAggregator.GetEvent<VideoStartedEvent>().Subscribe(
                action: OnVideoChanged,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<VideoEndedEvent>().Subscribe(
                action: OnVideoChanged,
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public IClipService ClipService => currentInput?.ClipService;

        public int ImagesQueueSize
        {
            get { return settings.Video.ImagesQueueSize; }
            set
            {
                if (value != settings.Video.ImagesQueueSize
                    && value > 0)
                {
                    settings.Video.ImagesQueueSize = value;

                    settingsService.Save();
                    UpdateInput();
                }
            }
        }

        public IList<Core.Models.Input> Inputs { get; } = new List<Core.Models.Input>();

        public bool IsActive => VideoService?.IsActive ?? false;

        public bool NoCentering
        {
            get { return settings.Video.NoCentering; }
            set
            {
                if (settings.Video.NoCentering != value)
                {
                    settings.Video.NoCentering = value;

                    settingsService.Save();
                    UpdateInput();
                }
            }
        }

        public int ProcessingDelay
        {
            get { return settings.Video.ProcessingDelay; }
            set
            {
                if (settings.Video.ProcessingDelay != value)
                {
                    settings.Video.ProcessingDelay = value;

                    settingsService.Save();
                    UpdateInput();
                }
            }
        }

        public ISampleService SampleService => currentInput?.SampleService;

        public ITemplateService TemplateService => currentInput?.TemplateService;

        public int ThresholdDetecting
        {
            get { return settings.Detection.ThresholdDetecting; }
            set
            {
                if (settings.Detection.ThresholdDetecting != value)
                {
                    settings.Detection.ThresholdDetecting = value;

                    settingsService.Save();
                    UpdateInput();
                }
            }
        }

        public int ThresholdMatching
        {
            get { return settings.Detection.ThresholdMatching; }
            set
            {
                if (settings.Detection.ThresholdMatching != value)
                {
                    settings.Detection.ThresholdMatching = value;

                    settingsService.Save();
                    UpdateInput();
                }
            }
        }

        public IVideoService VideoService => currentInput?.VideoService;

        public int WaitingDuration
        {
            get { return settings.Detection.WaitingDuration; }
            set
            {
                if (settings.Detection.WaitingDuration != value)
                {
                    settings.Detection.WaitingDuration = value;

                    settingsService.Save();
                    UpdateInput();
                }
            }
        }

        #endregion Public Properties

        #region Public Methods

        public void Initialize()
        {
            UpdateDevices();

            foreach (var input in settings.Video.Inputs)
            {
                if (input.IsFile)
                {
                    Select(input.FileName);
                }
                else
                {
                    var current = Inputs
                        .SingleOrDefault(i => i.Name == input.Name);

                    Select(current);
                }
            }
        }

        public void Select(Core.Models.Input input)
        {
            if (input.VideoService == default)
            {
                input.VideoService = containerProvider
                    .Resolve<IVideoService>();

                input.VideoService.RunAsync(input);
            }

            if (currentInput != input)
            {
                currentInput = input;
                UpdateInput();

                eventAggregator
                    .GetEvent<InputSelectedEvent>()
                    .Publish(currentInput);
            }

            UpdateSettings();
        }

        public void Select(string fileName)
        {
            var input = Inputs
                .SingleOrDefault(i => i.FileName == fileName);

            if (input == default)
            {
                input = new Core.Models.Input(true)
                {
                    FileName = fileName,
                    Name = Path.GetFileName(fileName),
                };

                Inputs.Add(input);
            }

            Select(input);
        }

        public void Update()
        {
            UpdateDevices();
        }

        #endregion Public Methods

        #region Private Methods

        private void AddFileSelection()
        {
            if (!Inputs.Any(i => i.IsFile
                && string.IsNullOrWhiteSpace(i.FileName)))
            {
                var input = new Core.Models.Input(true)
                {
                    Name = Constants.InputFileText,
                };

                Inputs.Add(input);

                eventAggregator
                    .GetEvent<InputsChangedEvent>()
                    .Publish();
            }
        }

        private void OnVideoChanged()
        {
            UpdateDevices();
            UpdateSettings();

            eventAggregator
                .GetEvent<InputsChangedEvent>()
                .Publish();
        }

        private void UpdateDevices()
        {
            using var deviceEnumerator = new SystemDeviceEnumerator();

            var devices = deviceEnumerator.ListVideoInputDevice()
                .OrderBy(d => d.Value).ToArray();

            var toBeRemoveds = Inputs
                .Where(i => !i.IsActive
                    && i.Name != Constants.InputFileText
                    && !devices.Any(d => d.Key == i.DeviceId)).ToArray();

            foreach (var toBeRemoved in toBeRemoveds)
            {
                toBeRemoved.ClipService.Clear();
                toBeRemoved.VideoService?.Dispose();
                Inputs.Remove(toBeRemoved);
            }

            var toBeAddeds = devices
                .Where(d => !Inputs.Any(i => d.Key == i.DeviceId)).ToArray();

            foreach (var toBeAdded in toBeAddeds)
            {
                var current = new Core.Models.Input(false)
                {
                    DeviceId = toBeAdded.Key,
                    Name = toBeAdded.Value,
                };

                Inputs.Add(current);
            }

            if (toBeRemoveds.Any()
                || toBeAddeds.Any())
            {
                AddFileSelection();

                eventAggregator
                    .GetEvent<InputsChangedEvent>()
                    .Publish();
            }
        }

        private void UpdateInput()
        {
            if (currentInput?.VideoService != default)
            {
                currentInput.VideoService.ImagesQueueSize = ImagesQueueSize;
                currentInput.VideoService.NoCentering = NoCentering;
                currentInput.VideoService.ProcessingDelay = ProcessingDelay;
                currentInput.VideoService.ThresholdDetecting = Math.Abs(ThresholdDetecting) / Constants.DividerThreshold;
                currentInput.VideoService.ThresholdMatching = Math.Abs(ThresholdMatching) / Constants.DividerThreshold;
                currentInput.VideoService.WaitingDuration = TimeSpan.FromMilliseconds(Math.Abs(WaitingDuration));
            }
        }

        private void UpdateSettings()
        {
            settings.Video.Inputs = Inputs
                .Where(i => i.IsActive).ToList();

            settingsService.Save();
        }

        #endregion Private Methods
    }
}