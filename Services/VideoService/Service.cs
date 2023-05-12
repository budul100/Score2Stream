using Avalonia.Media.Imaging;
using OpenCvSharp;
using Prism.Events;
using Score2Stream.Core.Events.Clip;
using Score2Stream.Core.Events.Sample;
using Score2Stream.Core.Events.Video;
using Score2Stream.Core.Interfaces;
using Score2Stream.Core.Models;
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

        private const int DelayDefault = 100;
        private const int DelayMin = 10;
        private const double DividerThreshold = 100;
        private const int ImagesQueueSizeDefault = 3;
        private const int ThresholdDetectingDefault = 90;
        private const int ThresholdMatchingDefault = 40;
        private const int WaitingDurationDefault = 100;

        private readonly IDispatcherService dispatcherService;
        private readonly IEventAggregator eventAggregator;
        private readonly VideoUpdatedEvent videoUpdatedEvent;

        private CancellationTokenSource cancellationTokenSource;
        private Mat frame;
        private bool isDisposed;
        private int maxHeight;
        private int maxWidth;
        private Task serviceTask;
        private double thresholdDetecting;
        private double thresholdMatching;

        #endregion Private Fields

        #region Public Constructors

        public Service(IClipService clipService, IDispatcherService dispatcherService, IEventAggregator eventAggregator)
        {
            ThresholdDetecting = ThresholdDetectingDefault;
            ThresholdMatching = ThresholdMatchingDefault;

            ClipService = clipService;

            this.dispatcherService = dispatcherService;
            this.eventAggregator = eventAggregator;

            videoUpdatedEvent = eventAggregator
                .GetEvent<VideoUpdatedEvent>();
        }

        #endregion Public Constructors

        #region Public Properties

        public Bitmap Bitmap { get; private set; }

        public IClipService ClipService { get; }

        public int Delay { get; set; } = DelayDefault;

        public int ImagesQueueSize { get; set; } = ImagesQueueSizeDefault;

        public bool IsActive { get; private set; }

        public string Name { get; private set; }

        public bool NoCentering { get; set; }

        public TimeSpan? ProcessingTime { get; private set; }

        public int ThresholdDetecting
        {
            get
            {
                return Convert.ToInt32(thresholdDetecting * DividerThreshold);
            }
            set
            {
                thresholdDetecting = Math.Abs(value) / DividerThreshold;
            }
        }

        public int ThresholdMatching
        {
            get
            {
                return Convert.ToInt32(thresholdMatching * DividerThreshold);
            }
            set
            {
                thresholdMatching = Math.Abs(value) / DividerThreshold;
            }
        }

        public int WaitingDuration { get; set; } = WaitingDurationDefault;

        #endregion Public Properties

        #region Public Methods

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task RunAsync(Input input)
        {
            input.VideoService = this;
            this.Name = input.Name;

            eventAggregator.GetEvent<ClipUpdatedEvent>().Subscribe(
                action: c => UpdateRectangle(c),
                keepSubscriberReferenceAlive: true);

            await StartAsync(
                deviceId: input.DeviceId,
                fileName: input.FileName);
        }

        public void StopAll()
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

                using var currentFrame = new Mat();

                do
                {
                    var capturingStart = DateTime.Now;

                    hasContent = video.Read(currentFrame);

                    if (!currentFrame.Empty())
                    {
                        frame = currentFrame;
                        Bitmap = new Bitmap(frame.ToMemoryStream());

                        var clips = ClipService.Clips
                            .Where(c => c.Rect.HasValue).ToArray();

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

                        if (frameIndex++ >= frameCount
                            || !hasContent)
                        {
                            frameIndex = 0;
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
            IsActive = false;

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

        private void UpdateDetecting(Clip clip)
        {
            var detectingSample = clip.Template?.Samples?
                .OrderByDescending(c => c.Similarity).FirstOrDefault();

            if ((clip.Template?.Samples.Any() != true)
                || ((detectingSample != default) && (detectingSample.Similarity < thresholdDetecting)))
            {
                eventAggregator
                    .GetEvent<SampleDetectedEvent>()
                    .Publish(clip);
            }
        }

        private void UpdateImage(Clip clip)
        {
            var baseImage = frame
                .Clone(clip.Rect.Value);

            clip.Images.Enqueue(baseImage);

            if (clip.Images.Count > 1
                && clip.Images.Count > ImagesQueueSize)
            {
                clip.Images.Dequeue();
            }

            if (ImagesQueueSize <= 1 || clip.Images.Count >= ImagesQueueSize)
            {
                var thresholdMonochrome = clip.ThresholdMonochrome / DividerThreshold;

                var monochromeImage = clip.Images.AsBlended()
                    .ToMonochrome(thresholdMonochrome);

                var contourRectangle = !NoCentering
                    ? monochromeImage.GetContour()
                    : default;

                var currentImage = contourRectangle.HasValue
                    ? monochromeImage.ToCropped(contourRectangle.Value)
                    : monochromeImage;

                if (currentImage != default)
                {
                    var similarity = currentImage.GetSimilarityTo(clip.Image);

                    if (similarity < thresholdDetecting || similarity == 1)
                    {
                        clip.Image = currentImage;

                        var centredImage = clip.Image
                            .ToCentered(
                                fullWidth: maxWidth,
                                fullHeight: maxHeight);

                        clip.Bitmap = new Bitmap(centredImage.ToMemoryStream());

                        UpdateSample(clip);
                    }
                }
            }
        }

        private void UpdateRectangle(Clip clip)
        {
            if (frame != default
                && clip.HasDimensions
                && ClipService.Clips.Contains(clip))
            {
                var firstX = Convert.ToInt32(clip.RelativeX1 * frame.Size().Width);
                var secondX = Convert.ToInt32(clip.RelativeX2 * frame.Size().Width);

                var firstY = Convert.ToInt32(clip.RelativeY1 * frame.Size().Height);
                var secondY = Convert.ToInt32(clip.RelativeY2 * frame.Size().Height);

                var rectangle = frame.Size().GetRectangle(
                    firstX: firstX,
                    firstY: firstY,
                    secondX: secondX,
                    secondY: secondY);

                clip.Rect = rectangle;

                if (rectangle.HasValue)
                {
                    var relevants = ClipService.Clips
                        .Where(c => c.Rect.HasValue);

                    maxHeight = relevants.Max(r => r.Rect.Value.Height);
                    maxWidth = relevants.Max(r => r.Rect.Value.Width);
                }
            }
        }

        private void UpdateSample(Clip clip)
        {
            var waitingSpan = TimeSpan.FromMilliseconds(WaitingDuration);

            if (clip.Image == default)
            {
                clip.SetValue(
                    value: clip.Template?.ValueEmpty,
                    similarity: 0,
                    waitingSpan: waitingSpan);
            }
            else if (clip.Template != default)
            {
                clip.SetSimilarities();

                UpdateDetecting(clip);

                var matchingSample = clip.Template?.Samples?
                    .Where(s => !string.IsNullOrWhiteSpace(s.Value))
                    .OrderByDescending(c => c.Similarity).FirstOrDefault();

                if ((matchingSample != default)
                    && (matchingSample.Similarity >= thresholdMatching))
                {
                    var similarity = Convert.ToInt32(matchingSample.Similarity * DividerThreshold);

                    clip.SetValue(
                        value: matchingSample.Value,
                        similarity: similarity,
                        waitingSpan: waitingSpan);
                }
                else
                {
                    clip.SetValue(
                        value: clip.Template?.ValueEmpty,
                        similarity: 0,
                        waitingSpan: waitingSpan);
                }
            }
        }

        private async Task UpdateVideoAsync(DateTime? capturingStart = default)
        {
            videoUpdatedEvent.Publish();

            ProcessingTime = capturingStart.HasValue
                ? DateTime.Now - capturingStart
                : default;

            var currentDelay = ImagesQueueSize > 1
                ? (Delay / ImagesQueueSize) - (ProcessingTime?.Milliseconds ?? 0)
                : Delay - (ProcessingTime?.Milliseconds ?? 0);

            if (currentDelay < DelayMin)
            {
                currentDelay = DelayMin;
            }

            await Task.Delay(currentDelay);
        }

        #endregion Private Methods
    }
}