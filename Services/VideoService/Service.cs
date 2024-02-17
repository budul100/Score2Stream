using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using OpenCvSharp;
using Prism.Events;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Enums;
using Score2Stream.Commons.Events.Clip;
using Score2Stream.Commons.Events.Sample;
using Score2Stream.Commons.Events.Video;
using Score2Stream.Commons.Extensions;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;
using Score2Stream.Commons.Models.Settings;
using Score2Stream.VideoService.Extensions;

namespace Score2Stream.VideoService
{
    public class Service
        : IVideoService
    {
        #region Private Fields

        private readonly IDispatcherService dispatcherService;
        private readonly IEventAggregator eventAggregator;
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

            videoUpdatedEvent = eventAggregator
                .GetEvent<VideoUpdatedEvent>();
        }

        #endregion Public Constructors

        #region Public Properties

        public Bitmap Bitmap { get; private set; }

        public IClipService ClipService { get; }

        public int ImagesQueueSize
        {
            get { return settingsService.Contents.Video.ImagesQueueSize; }
            set
            {
                if (value != settingsService.Contents.Video.ImagesQueueSize
                    && value > 0)
                {
                    settingsService.Contents.Video.ImagesQueueSize = value;
                    settingsService.Save();
                }
            }
        }

        public bool IsActive { get; private set; }

        public bool IsEnded { get; private set; }

        public string Name { get; private set; }

        public bool NoCropping
        {
            get { return settingsService.Contents.Video.NoCropping; }
            set
            {
                if (settingsService.Contents.Video.NoCropping != value)
                {
                    settingsService.Contents.Video.NoCropping = value;
                    settingsService.Save();
                }
            }
        }

        public bool PreferNeighbors
        {
            get { return settingsService.Contents.Detection.PreferNeighbors; }
            set
            {
                if (settingsService.Contents.Detection.PreferNeighbors != value)
                {
                    settingsService.Contents.Detection.PreferNeighbors = value;
                    settingsService.Save();
                }
            }
        }

        public int ProcessingDelay
        {
            get { return settingsService.Contents.Video.ProcessingDelay; }
            set
            {
                if (settingsService.Contents.Video.ProcessingDelay != value)
                {
                    settingsService.Contents.Video.ProcessingDelay = value;
                    settingsService.Save();
                }
            }
        }

        public TimeSpan? ProcessingTime { get; private set; }

        public float Rotation
        {
            get { return settingsService.Contents.Video.Rotation; }
            set
            {
                if (settingsService.Contents.Video.Rotation != value)
                {
                    settingsService.Contents.Video.Rotation = value;
                    settingsService.Save();
                }
            }
        }

        public bool RotationLeftPossible => Rotation >= Constants.RotateLeftMax;

        public bool RotationRightPossible => Rotation <= Constants.RotateRightMax;

        public int ThresholdMatching
        {
            get { return settingsService.Contents.Detection.ThresholdMatching; }
            set
            {
                if (settingsService.Contents.Detection.ThresholdMatching != value)
                {
                    settingsService.Contents.Detection.ThresholdMatching = value;
                    settingsService.Save();
                }
            }
        }

        public int WaitingDuration
        {
            get { return settingsService.Contents.Detection.WaitingDuration; }
            set
            {
                if (settingsService.Contents.Detection.WaitingDuration != value)
                {
                    settingsService.Contents.Detection.WaitingDuration = value;
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
                        var clone = currentFrame.Clone();
                        frame = clone.AsRotated(Rotation);

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

                    var position = 0.0;

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

                        position = video.Get(VideoCaptureProperties.PosMsec);
                    }

                    await UpdateVideoAsync(
                        capturingStart: capturingStart);

                    if (!deviceId.HasValue
                        && ProcessingDelay > 0
                        && ProcessingTime?.TotalMilliseconds > 0)
                    {
                        video.Set(
                            propertyId: VideoCaptureProperties.PosMsec,
                            value: position + ProcessingTime.Value.TotalMilliseconds);
                    }
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

                    var thresholdMonochrome = clip.ThresholdMonochrome / Constants.ThresholdDivider;

                    var monochromeImage = noiselessImage
                        .ToMonochrome(thresholdMonochrome);

                    var contourRectangle = !NoCropping
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

                var firstX = Convert.ToInt32(clip.X1 * size.Width);
                var secondX = Convert.ToInt32(clip.X2 * size.Width);

                var firstY = Convert.ToInt32(clip.Y1 * size.Height);
                var secondY = Convert.ToInt32(clip.Y2 * size.Height);

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
                var thresholdMatching = Math.Abs(ThresholdMatching) / Constants.ThresholdDivider;

                var match = clip
                    .GetMatches(settingsService.Contents.Detection.NoMultiComparison)
                    .OrderByDescending(m => m.Key >= thresholdMatching
                        && (!PreferNeighbors || clip.IsNeighbour(m.Value?.Value)))
                    .ThenByDescending(m => m.Key).FirstOrDefault();

                var similarity = Convert.ToInt32(match.Key * Constants.ThresholdDivider);

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

            var delay = ProcessingDelay + Constants.DelayUpdate;

            await Task.Delay(delay);

            ProcessingTime = capturingStart.HasValue
                ? DateTime.Now - capturingStart
                : default;
        }

        #endregion Private Methods
    }
}