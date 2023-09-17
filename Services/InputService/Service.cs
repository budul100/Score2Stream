using Hompus.VideoInputDevices;
using Prism.Events;
using Prism.Ioc;
using Score2Stream.Core.Constants;
using Score2Stream.Core.Events.Clip;
using Score2Stream.Core.Events.Input;
using Score2Stream.Core.Events.Sample;
using Score2Stream.Core.Events.Template;
using Score2Stream.Core.Events.Video;
using Score2Stream.Core.Interfaces;
using Score2Stream.Core.Models.Contents;
using Score2Stream.Core.Models.Settings;
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

        private Input currentInput;
        private bool isInitializing;

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

            eventAggregator.GetEvent<ClipsChangedEvent>().Subscribe(
                action: UpdateClips,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<ClipUpdatedEvent>().Subscribe(
                action: _ => UpdateClips(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<TemplatesChangedEvent>().Subscribe(
                action: UpdateClips,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<SamplesChangedEvent>().Subscribe(
                action: UpdateClips,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<SampleUpdatedEvent>().Subscribe(
                action: _ => UpdateClips(),
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public IClipService ClipService => currentInput?.ClipService;

        public int ImagesQueueSize
        {
            get { return settings.Session.ImagesQueueSize; }
            set
            {
                if (value != settings.Session.ImagesQueueSize
                    && value > 0)
                {
                    settings.Session.ImagesQueueSize = value;

                    if (!isInitializing)
                    {
                        settingsService.Save();
                        UpdateInput();
                    }
                }
            }
        }

        public IList<Input> Inputs { get; } = new List<Input>();

        public bool IsActive => VideoService?.IsActive ?? false;

        public bool NoCentering
        {
            get { return settings.Session.NoCentering; }
            set
            {
                if (settings.Session.NoCentering != value)
                {
                    settings.Session.NoCentering = value;

                    if (!isInitializing)
                    {
                        settingsService.Save();
                        UpdateInput();
                    }
                }
            }
        }

        public int ProcessingDelay
        {
            get { return settings.Session.ProcessingDelay; }
            set
            {
                if (settings.Session.ProcessingDelay != value)
                {
                    settings.Session.ProcessingDelay = value;

                    if (!isInitializing)
                    {
                        settingsService.Save();
                        UpdateInput();
                    }
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

                    if (!isInitializing)
                    {
                        settingsService.Save();
                        UpdateInput();
                    }
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

                    if (!isInitializing)
                    {
                        settingsService.Save();
                        UpdateInput();
                    }
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

                    if (!isInitializing)
                    {
                        settingsService.Save();
                        UpdateInput();
                    }
                }
            }
        }

        #endregion Public Properties

        #region Public Methods

        public void Initialize()
        {
            UpdateDevices();

            isInitializing = true;

            if (settings.Inputs?.Any() == true)
            {
                var inputs = settings.Inputs.ToArray();

                foreach (var input in inputs)
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

                    if (input.Clips?.Any() == true)
                    {
                        var clips = input.Clips.ToArray();

                        foreach (var clip in clips)
                        {
                            if (clip.Template != default)
                            {
                                if (clip.Template.Samples?.Any() == true)
                                {
                                    clip.Template.Samples = clip.Template.Samples
                                        .Where(s => s.Full != default).ToList();

                                    var samples = clip.Template.Samples.ToArray();

                                    foreach (var sample in samples)
                                    {
                                        currentInput.SampleService.Add(sample);
                                    }
                                }

                                clip.Template.Clip = clip;

                                currentInput.TemplateService.Add(clip.Template);
                            }

                            currentInput.ClipService.Add(clip);
                        }
                    }
                }
            }

            isInitializing = false;

            UpdateInput();
            UpdateClips();
            UpdateSettings();
        }

        public void Select(Input input)
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
            if (File.Exists(fileName))
            {
                var input = Inputs
                    .SingleOrDefault(i => i.FileName == fileName);

                if (input == default)
                {
                    input = new Input(true)
                    {
                        FileName = fileName,
                        Name = Path.GetFileName(fileName),
                    };

                    Inputs.Add(input);
                }

                Select(input);
            }
        }

        public void StopAll()
        {
            var relevants = Inputs
                .Where(i => i.IsActive).ToArray();

            foreach (var relevant in relevants)
            {
                relevant.VideoService.Stop();
            }

            UpdateSettings();
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
                var input = new Input(true)
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

        private void UpdateClips()
        {
            if (!isInitializing)
            {
                currentInput.Clips = ClipService?.Clips;

                settingsService.Save();
            }
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
                var current = new Input(false)
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
            if (!isInitializing)
            {
                settings.Inputs = Inputs
                    .Where(i => i.IsActive).ToList();

                settingsService.Save();
            }
        }

        #endregion Private Methods
    }
}