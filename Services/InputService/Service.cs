using Hompus.VideoInputDevices;
using MessageBox.Avalonia.Enums;
using Prism.Events;
using Prism.Ioc;
using Score2Stream.Core.Events.Clip;
using Score2Stream.Core.Events.Input;
using Score2Stream.Core.Events.Sample;
using Score2Stream.Core.Events.Template;
using Score2Stream.Core.Events.Video;
using Score2Stream.Core.Interfaces;
using Score2Stream.Core.Models.Contents;
using Score2Stream.Core.Models.Settings;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Score2Stream.InputService
{
    public class Service
        : IInputService
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private readonly IEventAggregator eventAggregator;
        private readonly IMessageBoxService messageBoxService;
        private readonly Session settings;
        private readonly ISettingsService<Session> settingsService;

        private bool isInitializing;

        #endregion Private Fields

        #region Public Constructors

        public Service(ISettingsService<Session> settingsService, IMessageBoxService messageBoxService,
            IContainerProvider containerProvider, IEventAggregator eventAggregator)
        {
            this.settingsService = settingsService;
            this.messageBoxService = messageBoxService;
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
                action: SaveClips,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<ClipUpdatedEvent>().Subscribe(
                action: _ => SaveClips(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<TemplatesChangedEvent>().Subscribe(
                action: SaveTemplates,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<SamplesChangedEvent>().Subscribe(
                action: SaveTemplates,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<SampleUpdatedEvent>().Subscribe(
                action: _ => SaveTemplates(),
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public Input Active { get; private set; }

        public IClipService ClipService => Active?.ClipService;

        public HashSet<Input> Inputs { get; } = new HashSet<Input>();

        public bool IsActive => VideoService?.IsActive ?? false;

        public ISampleService SampleService => TemplateService?.Active?.SampleService;

        public ITemplateService TemplateService => Active?.TemplateService;

        public IVideoService VideoService => Active?.VideoService;

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
                    }
                }

                if (current?.Clips?.Any() == true)
                {
                    var clips = current.Clips.ToArray();

                    foreach (var clip in clips)
                    {
                        clip.Template = input.TemplateService.Templates?
                            .SingleOrDefault(t => t.Name == clip.TemplateName);

                        input.ClipService.Add(clip);
                    }
                }
            }

            isInitializing = false;

            var relevant = Inputs.FirstOrDefault(i => !i.IsDevice
                || settings?.Inputs?.Any(s => s.DeviceId == i.DeviceId) == true);

            Select(relevant);

            SaveClips();
            SaveTemplates();
        }

        public void Select(Input input)
        {
            if (input != default)
            {
                AddInput(input);

                if (input != Active)
                {
                    Active = input;

                    if (TemplateService != default)
                    {
                        if (TemplateService.Templates?.Any() != true)
                        {
                            TemplateService.Create();
                        }

                        TemplateService.Select(TemplateService.Templates?.FirstOrDefault());
                    }

                    eventAggregator
                        .GetEvent<InputSelectedEvent>()
                        .Publish(Active);

                    SaveInputs();
                }
            }
        }

        public void Select(string fileName)
        {
            var input = GetInput(fileName);

            Select(input);
        }

        public async Task StopAsync()
        {
            var relevants = Inputs
                .Where(i => i.IsActive).ToArray();

            if (relevants.Any())
            {
                var result = await messageBoxService.GetMessageBoxResultAsync(
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

        private void AddInput(Input input)
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
                    AddInput(device);
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
                        AddInput(file);

                        yield return file;
                    }
                }
            }
        }

        private void OnVideoChanged()
        {
            UpdateDevices();
            SaveInputs();

            eventAggregator
                .GetEvent<InputsChangedEvent>()
                .Publish();
        }

        private void SaveClips()
        {
            if (!isInitializing
                && Active != default)
            {
                Active.Clips = ClipService?.Clips;

                settingsService.Save();
            }
        }

        private void SaveInputs()
        {
            if (!isInitializing)
            {
                settings.Inputs = Inputs
                    .Where(i => i.IsActive).ToList();

                settingsService.Save();
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

        #endregion Private Methods
    }
}