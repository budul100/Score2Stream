using Hompus.VideoInputDevices;
using Prism.Events;
using Prism.Ioc;
using Score2Stream.Core.Constants;
using Score2Stream.Core.Events.Input;
using Score2Stream.Core.Events.Video;
using Score2Stream.Core.Interfaces;
using Score2Stream.Core.Models;
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

        private Input currentInput;

        private int imagesQueueSize;
        private bool noCentering;
        private int processingDelay;
        private int thresholdDetecting;
        private int thresholdMatching;
        private int waitingDuration;

        #endregion Private Fields

        #region Public Constructors

        public Service(IContainerProvider containerProvider, IEventAggregator eventAggregator)
        {
            ProcessingDelay = Constants.DefaultProcessingDelay;
            ImagesQueueSize = Constants.DefaultImagesQueueSize;
            ThresholdDetecting = Constants.DefaultThresholdDetecting;
            ThresholdMatching = Constants.DefaultThresholdMatching;
            WaitingDuration = Constants.DefaultWaitingDuration;

            this.containerProvider = containerProvider;
            this.eventAggregator = eventAggregator;

            eventAggregator.GetEvent<VideoEndedEvent>().Subscribe(
                action: UpdateDevices,
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public IClipService ClipService => currentInput?.ClipService;

        public int ImagesQueueSize
        {
            get { return imagesQueueSize; }
            set
            {
                if (value != imagesQueueSize
                    && value > 0)
                {
                    imagesQueueSize = value;
                    UpdateInput();
                }
            }
        }

        public IList<Input> Inputs { get; } = new List<Input>();

        public bool IsActive => VideoService?.IsActive ?? false;

        public bool NoCentering
        {
            get { return noCentering; }
            set
            {
                if (noCentering != value)
                {
                    noCentering = value;
                    UpdateInput();
                }
            }
        }

        public int ProcessingDelay
        {
            get { return processingDelay; }
            set
            {
                if (processingDelay != value)
                {
                    processingDelay = value;
                    UpdateInput();
                }
            }
        }

        public ISampleService SampleService => currentInput?.SampleService;

        public ITemplateService TemplateService => currentInput?.TemplateService;

        public int ThresholdDetecting
        {
            get { return thresholdDetecting; }
            set
            {
                if (thresholdDetecting != value)
                {
                    thresholdDetecting = value;
                    UpdateInput();
                }
            }
        }

        public int ThresholdMatching
        {
            get { return thresholdMatching; }
            set
            {
                if (thresholdMatching != value)
                {
                    thresholdMatching = value;
                    UpdateInput();
                }
            }
        }

        public IVideoService VideoService => currentInput?.VideoService;

        public int WaitingDuration
        {
            get { return waitingDuration; }
            set
            {
                if (waitingDuration != value)
                {
                    waitingDuration = value;
                    UpdateInput();
                }
            }
        }

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
                UpdateInput();

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

        #endregion Private Methods
    }
}