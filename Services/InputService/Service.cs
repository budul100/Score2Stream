﻿using Hompus.VideoInputDevices;
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

            eventAggregator.GetEvent<VideoEndedEvent>().Subscribe(
                action: UpdateDevices,
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
            Update();

            var inputSettings = settings.Video.Inputs.ToArray();

            foreach (var inputSetting in inputSettings)
            {
                if (!string.IsNullOrWhiteSpace(inputSetting.FileName)
                    && File.Exists(inputSetting.FileName))
                {
                    Select(inputSetting.FileName);
                }
                else
                {
                    var input = Inputs
                        .SingleOrDefault(i => i.Name == inputSetting.DeviceName);

                    if (input?.DeviceId.HasValue == true)
                    {
                        Select(input.DeviceId.Value);
                    }
                }
            }
        }

        public void Select(int deviceId)
        {
            var input = Inputs
                .SingleOrDefault(i => i.DeviceId == deviceId);

            var inputSettings = new Core.Settings.Input
            {
                DeviceName = input.Name,
            };

            settings.Video.Inputs.RemoveAll(i => i.DeviceName == input.Name);
            settings.Video.Inputs.Add(inputSettings);

            settingsService.Save();

            SelectInput(input);
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

            var inputSettings = new Core.Settings.Input
            {
                FileName = input.FileName,
            };

            settings.Video.Inputs.RemoveAll(i => i.FileName == input.FileName);
            settings.Video.Inputs.Add(inputSettings);

            settingsService.Save();

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

        private void SelectInput(Core.Models.Input input)
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

        #endregion Private Methods
    }
}