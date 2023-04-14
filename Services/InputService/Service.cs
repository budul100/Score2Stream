using Hompus.VideoInputDevices;
using Prism.Events;
using Prism.Ioc;
using Score2Stream.Core.Constants;
using Score2Stream.Core.Events.Input;
using Score2Stream.Core.Events.Video;
using Score2Stream.Core.Interfaces;
using Score2Stream.Core.Models;
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

        private Input currentInput;

        #endregion Private Fields

        #region Public Constructors

        public Service(IContainerProvider containerProvider, IEventAggregator eventAggregator)
        {
            this.containerProvider = containerProvider;
            this.eventAggregator = eventAggregator;

            eventAggregator.GetEvent<VideoEndedEvent>().Subscribe(
                action: UpdateDevices,
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public IClipService ClipService => currentInput?.ClipService;

        public IList<Input> Inputs { get; } = new List<Input>();

        public bool IsActive => VideoService?.IsActive ?? false;

        public ISampleService SampleService => currentInput?.SampleService;

        public ITemplateService TemplateService => currentInput?.TemplateService;

        public IVideoService VideoService => currentInput?.VideoService;

        #endregion Public Properties

        #region Public Methods

        public void Select(int deviceId)
        {
            var input = Inputs
                .SingleOrDefault(i => i.DeviceId == deviceId);

            SelectInput(input);
        }

        public void Select(string fileName)
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

            SelectInput(input);
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

        private void SelectInput(Input input)
        {
            if (input.VideoService == default)
            {
                containerProvider
                    .Resolve<IVideoService>()
                    .RunAsync(input);
            }

            if (currentInput != input)
            {
                currentInput = input;

                eventAggregator
                    .GetEvent<InputSelectedEvent>()
                    .Publish(currentInput);
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

        #endregion Private Methods
    }
}