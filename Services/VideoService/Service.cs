using Avalonia.Media.Imaging;
using OpenCvSharp;
using Prism.Events;
using Score2Stream.Core;
using Score2Stream.Core.Enums;
using Score2Stream.Core.Events.Clip;
using Score2Stream.Core.Events.Sample;
using Score2Stream.Core.Events.Video;
using Score2Stream.Core.Extensions;
using Score2Stream.Core.Interfaces;
using Score2Stream.Core.Models.Contents;
using Score2Stream.Core.Models.Settings;
using Score2Stream.VideoService.Extensions;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Score2Stream.VideoService
{
    public class Service
        : IVideoService
    {
        #region Private Fields

        private readonly IDispatcherService dispatcherService;
        private readonly IEventAggregator eventAggregator;
        private readonly Session settings;
        private readonly ISettingsService<Session> settingsService;
        private readonly VideoUpdatedEvent videoUpdatedEvent;

        private CancellationTokenSource cancellationTokenSource;
        private Mat frame;
        private int heightLast;
        private int heightMax;
        private bool isDisposed;
        private Task serviceTask;
        private int widthLast;
        private int widthMax;

        #endregion Private Fields

        #region Public Constructors

        public Service(ISettingsService<Session> settingsService, IClipService clipService,
            IDispatcherService dispatcherService, IEventAggregator eventAggregator)
        {
            this.dispatcherService = dispatcherService;
            this.eventAggregator = eventAggregator;
            this.settingsService = settingsService;

            ClipService = clipService;

            this.settings = settingsService.Get();

            videoUpdatedEvent = eventAggregator
                .GetEvent<VideoUpdatedEvent>();
        }

        #endregion Public Constructors

        #region Public Properties

        public Bitmap Bitmap { get; private set; }

        public IClipService ClipService { get; }

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
                }
            }
        }

        public bool IsActive { get; private set; }

        public bool IsEnded { get; private set; }

        public string Name { get; private set; }

        public bool NoCentering
        {
            get { return settings.Video.NoCentering; }
            set
            {
                if (settings.Video.NoCentering != value)
                {
                    settings.Video.NoCentering = value;
                    settingsService.Save();
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
                }
            }
        }

        public TimeSpan? ProcessingTime { get; private set; }

        public int ThresholdMatching
        {
            get { return settings.Detection.ThresholdMatching; }
            set
            {
                if (settings.Detection.ThresholdMatching != value)
                {
                    settings.Detection.ThresholdMatching = value;
                    settingsService.Save();
                }
            }
        }

        public int WaitingDuration
        {
            get { return settings.Detection.WaitingDuration; }
            set
            {
                if (settings.Detection.WaitingDuration != value)
                {
                    settings.Detection.WaitingDuration = value;
                    settingsService.Save();
                }
            }
        }

        #endregion Public Properties

        #region Public Methods

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task RunAsync(Input input)
        {
            this.Name = input.Name;

            eventAggregator.GetEvent<ClipUpdatedEvent>().Subscribe(
                action: c => UpdateRectangle(c),
                keepSubscriberReferenceAlive: true);

            await StartAsync(
                deviceId: input.DeviceId,
                fileName: input.FileName);
        }

        public void Stop()
        {
            cancellationTokenSource.Cancel();
        }

        #endregion Public Methods

        #region Protected Methods

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    cancellationTokenSource?.Cancel();
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private async void RunAsync(int? deviceId, string fileName)
        {
            var frameCount = 0.0;
            var frameIndex = 0.0;

            try
            {
                //// Creation and disposal of this object should be done in the same thread
                //// because if not it throws disconnectedContext exception

                await UpdateVideoAsync();

                using var video = new VideoCapture();

                if (deviceId.HasValue)
                {
                    if (!video.Open(deviceId.Value))
                    {
                        throw new ApplicationException(
                            message: $"Cannot connect to camera {Name}.");
                    }
                }
                else
                {
                    if (!File.Exists(fileName))
                    {
                        throw new FileNotFoundException(
                            message: $"The file {fileName} coud not be found.");
                    }
                    else if (!video.Open(fileName))
                    {
                        throw new ApplicationException(
                            message: $"Cannot open file {fileName}.");
                    }
                }

                IsActive = true;

                eventAggregator
                    .GetEvent<VideoStartedEvent>()
                    .Publish();

                var hasContent = false;

                do
                {
                    using var currentFrame = new Mat();
                    hasContent = video.Read(currentFrame);

                    var capturingStart = DateTime.Now;

                    if (!currentFrame.Empty())
                    {
                        frame = currentFrame.Clone();
                        var size = frame.Size();

                        if (size.Width != widthLast || size.Height != heightLast)
                        {
                            foreach (var clip in ClipService.Clips)
                            {
                                UpdateRectangle(clip);
                            }
                        }

                        heightLast = size.Height;
                        widthLast = size.Width;

                        Bitmap = new Bitmap(frame.ToMemoryStream());
                    }

                    var clips = ClipService.Clips
                        .Where(c => c.Rect.HasValue).ToArray();

                    if (clips?.Any() == true)
                    {
                        heightMax = clips.Max(r => r.Rect.Value.Height);
                        widthMax = clips.Max(r => r.Rect.Value.Width);

                        foreach (var clip in clips)
                        {
                            UpdateImage(clip);
                        }
                    }

                    if (!deviceId.HasValue)
                    {
                        if (frameCount == 0)
                        {
                            frameCount = video.Get(VideoCaptureProperties.FrameCount);
                        }

                        if (frameIndex++ > frameCount || !hasContent)
                        {
                            frameIndex = 1;
                            hasContent = true;

                            video.Set(
                                propertyId: VideoCaptureProperties.PosFrames,
                                value: frameIndex);
                        }
                    }

                    await UpdateVideoAsync(
                        capturingStart: capturingStart);
                }
                while (hasContent
                    && !cancellationTokenSource.IsCancellationRequested);
            }
            catch
            { }

            frame = default;
            Bitmap = default;

            IsActive = false;
            IsEnded = true;

            await UpdateVideoAsync();

            eventAggregator
                .GetEvent<VideoEndedEvent>()
                .Publish();
        }

        private async Task StartAsync(int? deviceId, string fileName)
        {
            if (serviceTask?.IsCompleted == false)
            {
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();

            async Task runTask() => await dispatcherService.InvokeAsync(() => RunAsync(
                deviceId: deviceId,
                fileName: fileName)).ConfigureAwait(false);

            serviceTask = Task.Run(
                function: runTask,
                cancellationToken: cancellationTokenSource.Token);

            if (serviceTask.IsFaulted)
            {
                await serviceTask;
            }
        }

        private void UpdateImage(Clip clip)
        {
            if (!frame.Empty())
            {
                var baseImage = frame
                    .Clone(clip.Rect.Value);

                clip.Images.Enqueue(baseImage);

                if (clip.Images.Count >= ImagesQueueSize)
                {
                    if (clip.Images.Count > ImagesQueueSize)
                    {
                        clip.Images.Dequeue();
                    }

                    var blendedImage = clip.Images.AsBlended();

                    var noiselessImage = clip.NoiseRemoval == 0
                        ? blendedImage
                        : blendedImage.WithoutNoise(
                            erodeIterations: clip.NoiseRemoval,
                            dilateIterations: clip.NoiseRemoval);

                    var thresholdMonochrome = clip.ThresholdMonochrome / Constants.DividerThreshold;

                    var monochromeImage = noiselessImage
                        .ToMonochrome(thresholdMonochrome);

                    var contourRectangle = !NoCentering
                        ? monochromeImage.GetContour()
                        : default;

                    var croppedImage = contourRectangle.HasValue
                        ? monochromeImage.ToCropped(contourRectangle.Value)
                        : monochromeImage;

                    clip.Mat = croppedImage;
                    clip.Width = widthMax;
                    clip.Height = heightMax;

                    if (clip.Mat != default)
                    {
                        var bitmapStream = clip.Mat.ToCentered(
                            fullWidth: clip.Width,
                            fullHeight: clip.Height);

                        clip.Bitmap = new Bitmap(bitmapStream.ToMemoryStream());
                    }

                    UpdateValue(clip);
                }
            }
        }

        private void UpdateRectangle(Clip clip)
        {
            if (frame != default
                && clip.HasDimensions
                && ClipService.Clips.Contains(clip))
            {
                var size = frame.Size();

                var firstX = Convert.ToInt32(clip.RelativeX1 * size.Width);
                var secondX = Convert.ToInt32(clip.RelativeX2 * size.Width);

                var firstY = Convert.ToInt32(clip.RelativeY1 * size.Height);
                var secondY = Convert.ToInt32(clip.RelativeY2 * size.Height);

                var rectangle = size.GetRectangle(
                    firstX: firstX,
                    firstY: firstY,
                    secondX: secondX,
                    secondY: secondY);

                clip.Rect = rectangle;
            }
        }

        private void UpdateValue(Clip clip)
        {
            var matchSample = default(Sample);

            var waitingDuration = TimeSpan.FromMilliseconds(Math.Abs(WaitingDuration));

            if (clip.Mat == default)
            {
                clip.SetValue(
                    value: clip.Template?.Empty,
                    similarity: 0,
                    waitingDuration: waitingDuration);
            }
            else if (clip.Template != default)
            {
                var match = clip.GetMatches()
                    .OrderByDescending(c => c.Key).FirstOrDefault();

                var similarity = Convert.ToInt32(match.Key * Constants.DividerThreshold);
                var thresholdMatching = Math.Abs(ThresholdMatching) / Constants.DividerThreshold;

                if (match.Value != default
                    && match.Key >= thresholdMatching)
                {
                    matchSample = match.Value;

                    clip.SetValue(
                        value: matchSample.Value,
                        similarity: similarity,
                        waitingDuration: waitingDuration);
                }
                else
                {
                    clip.SetValue(
                        value: clip.Template.Empty,
                        similarity: similarity,
                        waitingDuration: waitingDuration);
                }
            }

            if (clip == ClipService.Active)
            {
                ClipService.TemplateService?.SampleService?.Update(clip);

                if (matchSample != default)
                {
                    matchSample.Type = SampleType.Similar;
                }
            }

            eventAggregator.GetEvent<SamplesUpdatedEvent>().Publish();
        }

        private async Task UpdateVideoAsync(DateTime? capturingStart = default)
        {
            videoUpdatedEvent.Publish();

            await Task.Delay(ProcessingDelay);

            ProcessingTime = capturingStart.HasValue
                ? DateTime.Now - capturingStart
                : default;
        }

        #endregion Private Methods
    }
}