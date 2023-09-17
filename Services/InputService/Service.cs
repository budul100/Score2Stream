using Hompus.VideoInputDevices;
using OpenCvSharp;
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

        private Input active;
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
                action: UpdateTemplates,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<SamplesChangedEvent>().Subscribe(
                action: UpdateTemplates,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<SampleUpdatedValueEvent>().Subscribe(
                action: _ => UpdateTemplates(),
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public IClipService ClipService => active?.ClipService;

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

        public HashSet<Input> Inputs { get; } = new HashSet<Input>();

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

        public ISampleService SampleService => active?.SampleService;

        public ITemplateService TemplateService => active?.TemplateService;

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

        public IVideoService VideoService => active?.VideoService;

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
            isInitializing = true;

            var inputs = GetInputs().ToArray();

            foreach (var input in inputs)
            {
                var current = input.IsDevice
                    ? settings?.Inputs?.SingleOrDefault(i => i.DeviceId == input.DeviceId)
                    : settings?.Inputs?.SingleOrDefault(i => i.FileName == input.FileName);

                if (current?.Templates?.Any() == true)
                {
                    var templates = current.Templates.ToArray();

                    foreach (var template in templates)
                    {
                        input.TemplateService.Add(template);

                        if (template.Samples?.Any() == true)
                        {
                            template.Samples = template.Samples
                                .Where(s => s.Image != default).ToList();

                            foreach (var sample in template.Samples)
                            {
                                sample.Mat = Mat.FromImageData(
                                    imageBytes: sample.Image,
                                    mode: ImreadModes.Unchanged);
                                sample.Template = template;

                                input.SampleService.Add(sample);
                            }
                        }
                    }
                }

                if (current?.Clips?.Any() == true)
                {
                    var clips = current.Clips.ToArray();

                    foreach (var clip in clips)
                    {
                        clip.Template = input.TemplateService?.Templates?
                            .SingleOrDefault(t => t.ClipDescription == clip.TemplateDescription);

                        input.ClipService.Add(clip);

                        var clipTemplate = input.TemplateService?.Templates?
                            .SingleOrDefault(t => t.ClipDescription == clip.Description);

                        if (clipTemplate != default)
                        {
                            clipTemplate.Clip = clip;
                        }
                    }
                }
            }

            isInitializing = false;

            var relevant = Inputs.FirstOrDefault(i => !i.IsDevice
                || settings?.Inputs?.Any(s => s.DeviceId == i.DeviceId) == true);

            Select(relevant);
            ClipService?.Select(ClipService?.Clips?.FirstOrDefault());
            TemplateService?.Select(TemplateService?.Templates?.FirstOrDefault());

            UpdateClips();
            UpdateTemplates();
        }

        public void Select(Input input)
        {
            if (input != default)
            {
                StartInput(input);

                if (active != input)
                {
                    active = input;
                    UpdateInput();

                    eventAggregator
                        .GetEvent<InputSelectedEvent>()
                        .Publish(active);
                }

                UpdateSettings();
            }
        }

        public void Select(string fileName)
        {
            var input = GetInput(fileName);

            Select(input);
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

        private Input GetInput(string fileName)
        {
            var result = default(Input);

            if (File.Exists(fileName))
            {
                result = Inputs
                    .SingleOrDefault(i => i.FileName == fileName);

                if (result == default)
                {
                    result = new Input(false)
                    {
                        FileName = fileName,
                        Name = Path.GetFileName(fileName),
                    };
                }
            }

            return result;
        }

        private IEnumerable<Input> GetInputs()
        {
            UpdateDevices();

            var devices = Inputs
                .Where(i => i.IsDevice).ToArray();

            foreach (var device in devices)
            {
                if (settings?.Inputs?.Any(i => i.DeviceId == device.DeviceId) == true)
                {
                    StartInput(device);
                }

                yield return device;
            }

            if (settings.Inputs?.Any() == true)
            {
                var fileNames = settings.Inputs
                    .Where(i => !i.IsDevice)
                    .Select(i => i.FileName).ToArray();

                foreach (var fileName in fileNames)
                {
                    var file = GetInput(fileName);

                    if (file != default)
                    {
                        StartInput(file);

                        yield return file;
                    }
                }
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

        private void StartInput(Input input)
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

                Inputs.Add(input);
            }
        }

        private void UpdateClips()
        {
            if (!isInitializing
                && active != default)
            {
                active.Clips = ClipService?.Clips;

                if (active.Clips?.Any() == true)
                {
                    foreach (var clip in active.Clips)
                    {
                        clip.TemplateDescription = clip.Template?.Clip?.Description;
                    }
                }

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
                var current = new Input(true)
                {
                    DeviceId = toBeAdded.Key,
                    Name = toBeAdded.Value,
                };

                Inputs.Add(current);
            }

            if (toBeRemoveds.Any() || toBeAddeds.Any())
            {
                eventAggregator
                    .GetEvent<InputsChangedEvent>()
                    .Publish();
            }
        }

        private void UpdateInput()
        {
            if (active?.VideoService != default)
            {
                active.VideoService.ImagesQueueSize = ImagesQueueSize;
                active.VideoService.NoCentering = NoCentering;
                active.VideoService.ProcessingDelay = ProcessingDelay;
                active.VideoService.ThresholdDetecting = Math.Abs(ThresholdDetecting) / Constants.DividerThreshold;
                active.VideoService.ThresholdMatching = Math.Abs(ThresholdMatching) / Constants.DividerThreshold;
                active.VideoService.WaitingDuration = TimeSpan.FromMilliseconds(Math.Abs(WaitingDuration));
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

        private void UpdateTemplates()
        {
            if (!isInitializing
                && active != default)
            {
                active.Templates = TemplateService?.Templates;

                if (active.Templates?.Any() == true)
                {
                    foreach (var template in active.Templates)
                    {
                        template.ClipDescription = template.Description;

                        if (template.Samples?.Any() == true)
                        {
                            foreach (var sample in template.Samples)
                            {
                                sample.Image = sample.Mat.ToBytes();
                            }
                        }
                    }
                }

                settingsService.Save();
            }
        }

        #endregion Private Methods
    }
}