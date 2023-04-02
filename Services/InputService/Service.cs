using Core.Events.Input;
using Core.Interfaces;
using Core.Models;
using Hompus.VideoInputDevices;
using Prism.Events;
using Prism.Ioc;
using System.Collections.Generic;
using System.Linq;

namespace InputService
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
        }

        #endregion Public Constructors

        #region Public Properties

        public IClipService ClipService => currentInput?.ClipService;

        public IList<Input> Inputs { get; } = new List<Input>();

        public bool IsActive => VideoService?.IsActive ?? false;

        public IVideoService VideoService => currentInput?.VideoService;

        #endregion Public Properties

        #region Public Methods

        public void Select(Input input)
        {
            if (input.VideoService == default)
            {
                InitializeService(input);
            }

            if (currentInput != input)
            {
                currentInput = input;

                eventAggregator
                    .GetEvent<InputSelectedEvent>()
                    .Publish(currentInput);
            }
        }

        public void Update()
        {
            UpdateDevices();
        }

        #endregion Public Methods

        #region Private Methods

        private void InitializeService(Input input)
        {
            var clipService = containerProvider.Resolve<IClipService>();

            input.VideoService = containerProvider.Resolve<IVideoService>();

            input.VideoService.RunAsync(
                deviceId: input.DeviceId,
                name: input.Name,
                clipService: clipService);
        }

        private void UpdateDevices()
        {
            using var deviceEnumerator = new SystemDeviceEnumerator();

            var devices = deviceEnumerator.ListVideoInputDevice()
                .OrderBy(d => d.Value).ToArray();

            var toBeRemoveds = Inputs
                .Where(i => !devices.Any(d => d.Key == i.DeviceId)).ToArray();

            foreach (var toBeRemoved in toBeRemoveds)
            {
                toBeRemoved.VideoService?.Dispose();
                Inputs.Remove(toBeRemoved);
            }

            var toBeAddeds = devices
                .Where(d => !Inputs.Any(i => d.Key == i.DeviceId)).ToArray();

            foreach (var toBeAdded in toBeAddeds)
            {
                var current = new Input
                {
                    DeviceId = toBeAdded.Key,
                    Name = toBeAdded.Value,
                };

                Inputs.Add(current);
            }

            if (toBeRemoveds.Any()
                || toBeAddeds.Any())
            {
                eventAggregator
                    .GetEvent<InputsChangedEvent>()
                    .Publish();
            }
        }

        #endregion Private Methods
    }
}