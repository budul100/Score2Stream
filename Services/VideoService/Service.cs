using Avalonia.Media.Imaging;
using OpenCvSharp;
using Prism.Events;
using Score2Stream.Core;
using Score2Stream.Core.Events.Clip;
using Score2Stream.Core.Events.Sample;
using Score2Stream.Core.Events.Video;
using Score2Stream.Core.Extensions;
using Score2Stream.Core.Interfaces;
using Score2Stream.Core.Models.Contents;
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

        private const int DelayMin = 10;

        private readonly IDispatcherService dispatcherService;
        private readonly IEventAggregator eventAggregator;
        private readonly SampleUpdatedRelevanceEvent sampleUpdatedRelevanceEvent;
        private readonly VideoUpdatedEvent videoUpdatedEvent;

        private CancellationTokenSource cancellationTokenSource;
        private Mat frame;
        private bool isDisposed;
        private int maxHeight;
        private int maxWidth;
        private Task serviceTask;

        #endregion Private Fields

        #region Public Constructors

        public Service(IClipService clipService, IDispatcherService dispatcherService,
            IEventAggregator eventAggregator)
        {
            this.dispatcherService = dispatcherService;
            this.eventAggregator = eventAggregator;

            ClipService = clipService;

            videoUpdatedEvent = eventAggregator
                .GetEvent<VideoUpdatedEvent>();

            sampleUpdatedRelevanceEvent = eventAggregator
                .GetEvent<SampleUpdatedRelevanceEvent>();
        }

        #endregion Public Constructors

        #region Public Properties

        public Bitmap Bitmap { get; private set; }

        public IClipService ClipService { get; }

        public int ImagesQueueSize { get; set; }

        public bool IsActive { get; private set; }

        public string Name { get; private set; }

        public bool NoCentering { get; set; }

        public int ProcessingDelay { get; set; }

        public TimeSpan? ProcessingTime { get; private set; }

        public double ThresholdDetecting { get; set; }

        public double ThresholdMatching { get; set; }

        public TimeSpan WaitingDuration { get; set; }

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
                        Bitmap = new Bitmap(frame.ToMemoryStream());

                        foreach (var clip in ClipService.Clips)
                        {
                            UpdateRectangle(clip);
                        }
                    }

                    var clips = ClipService.Clips
                        .Where(c => c.Rect.HasValue).ToArray();

                    foreach (var clip in clips)
                    {
                        UpdateImage(clip);
                    }

                    if (!deviceId.HasValue)
                    {
                        if (frameCount == 0)
                        {
                            frameCount = video.Get(VideoCaptureProperties.FrameCount);
                        }

                        if (frameIndex++ > frameCount
                            || !hasContent)
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

        private void UpdateClip(Clip clip)
        {
            if (clip.Mat == default)
            {
                clip.SetValue(
                    value: clip.Template?.ValueEmpty,
                    similarity: 0,
                    waitingDuration: WaitingDuration);
            }
            else if (clip.Template != default)
            {
                var similarSamples = clip.GetSimilarSamples(
                    thresholdMatching: ThresholdMatching).ToArray();

                var matchingSample = similarSamples
                    .OrderByDescending(c => c.Key).FirstOrDefault();

                UpdateSamples(
                    clip: clip,
                    matchingSample: matchingSample.Value);

                var similarity = Convert.ToInt32(matchingSample.Key * Constants.DividerThreshold);

                if (matchingSample.Value != default)
                {
                    clip.SetValue(
                        value: matchingSample.Value.Value,
                        similarity: similarity,
                        waitingDuration: WaitingDuration);
                }
                else
                {
                    clip.SetValue(
                        value: clip.Template?.ValueEmpty,
                        similarity: similarity,
                        waitingDuration: WaitingDuration);
                }
            }
        }

        private void UpdateImage(Clip clip)
        {
            if (!frame.Empty())
            {
                var baseImage = frame
                    .Clone(clip.Rect.Value);

                clip.Images.Enqueue(baseImage);

                if (clip.Images.Count > ImagesQueueSize)
                {
                    clip.Images.Dequeue();
                }

                if (clip.Images.Count >= ImagesQueueSize)
                {
                    var thresholdMonochrome = clip.ThresholdMonochrome / Constants.DividerThreshold;

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
                        clip.Mat = currentImage;

                        clip.Width = maxWidth;
                        clip.Height = maxHeight;

                        var bitmapStream = currentImage.ToCentered(
                            fullWidth: maxWidth,
                            fullHeight: maxHeight);

                        clip.Bitmap = new Bitmap(bitmapStream.ToMemoryStream());

                        UpdateClip(clip);
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

        private void UpdateSamples(Clip clip, Sample matchingSample)
        {
            if (clip.Template?.Clip == clip)
            {
                var similarSample = default(Sample);

                if (clip.Template?.Samples?.Any() == true)
                {
                    foreach (var sample in clip.Template.Samples)
                    {
                        sample.Similarity = sample.Mat.GetSimilarityTo(clip.Mat);
                    }

                    similarSample = clip.Template.Samples
                        .OrderByDescending(c => c.Similarity).FirstOrDefault();

                    foreach (var sample in clip.Template.Samples)
                    {
                        if (sample.IsSimilar != (sample == similarSample))
                        {
                            sample.IsSimilar = sample == similarSample;
                            sampleUpdatedRelevanceEvent.Publish(sample);
                        }

                        if (sample.IsMatching != (sample == matchingSample))
                        {
                            sample.IsMatching = sample == matchingSample;
                            sampleUpdatedRelevanceEvent.Publish(sample);
                        }
                    }
                }

                if (similarSample == default || similarSample.Similarity < ThresholdDetecting)
                {
                    eventAggregator
                        .GetEvent<SampleDetectedEvent>()
                        .Publish(clip);
                }
            }
        }

        private async Task UpdateVideoAsync(DateTime? capturingStart = default)
        {
            videoUpdatedEvent.Publish();

            ProcessingTime = capturingStart.HasValue
                ? DateTime.Now - capturingStart
                : default;

            var currentDelay = (ProcessingDelay / ImagesQueueSize) - (ProcessingTime?.Milliseconds ?? 0);

            if (currentDelay < DelayMin)
            {
                currentDelay = DelayMin;
            }

            await Task.Delay(currentDelay);
        }

        #endregion Private Methods
    }
}