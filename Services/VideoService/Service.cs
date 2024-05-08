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
using Score2Stream.Commons.Events.Area;
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
        private readonly SampleUpdatedEvent sampleUpdatedEvent;
        private readonly SegmentDrawnEvent segmentDrawnEvent;
        private readonly SegmentUpdatedEvent segmentUpdatedEvent;
        private readonly ISettingsService<Session> settingsService;
        private readonly VideoEndedEvent videoEndedEvent;
        private readonly VideoStartedEvent videoStartedEvent;
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

        public Service(ISettingsService<Session> settingsService, IAreaService areaService,
            IDispatcherService dispatcherService, IEventAggregator eventAggregator)
        {
            this.dispatcherService = dispatcherService;
            this.settingsService = settingsService;

            AreaService = areaService;

            videoStartedEvent = eventAggregator.GetEvent<VideoStartedEvent>();
            videoEndedEvent = eventAggregator.GetEvent<VideoEndedEvent>();
            videoUpdatedEvent = eventAggregator.GetEvent<VideoUpdatedEvent>();

            segmentDrawnEvent = eventAggregator.GetEvent<SegmentDrawnEvent>();
            segmentUpdatedEvent = eventAggregator.GetEvent<SegmentUpdatedEvent>();

            sampleUpdatedEvent = eventAggregator.GetEvent<SampleUpdatedEvent>();

            eventAggregator.GetEvent<AreaModifiedEvent>().Subscribe(
                action: a => UpdateRectangles(a),
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public IAreaService AreaService { get; }

        public Bitmap Bitmap { get; private set; }

        public bool IsActive { get; private set; }

        public bool IsEnded { get; private set; }

        public string Name { get; private set; }

        public TimeSpan? ProcessingTime { get; private set; }

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

                videoStartedEvent.Publish();

                var hasContent = false;

                do
                {
                    using var currentFrame = new Mat();
                    hasContent = video.Read(currentFrame);

                    var capturingStart = DateTime.Now;

                    if (!currentFrame.Empty())
                    {
                        var clone = currentFrame.Clone();
                        frame = clone.AsRotated(settingsService.Contents.Video.Rotation);

                        var size = frame.Size();

                        if (size.Width != widthLast || size.Height != heightLast)
                        {
                            foreach (var area in AreaService.Areas)
                            {
                                UpdateRectangles(area);
                            }
                        }

                        heightLast = size.Height;
                        widthLast = size.Width;

                        Bitmap = new Bitmap(frame.ToMemoryStream());
                    }

                    var clips = AreaService?.Areas?
                        .SelectMany(a => a.Segments)
                        .Where(c => c.Rect.HasValue).ToArray();

                    if (clips?.Any() == true)
                    {
                        heightMax = clips.Max(a => a.Rect.Value.Height);
                        widthMax = clips.Max(a => a.Rect.Value.Width);

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
                        && settingsService.Contents.Video.ProcessingDelay > 0
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
            catch when (!System.Diagnostics.Debugger.IsAttached)
            { }

            frame = default;
            Bitmap = default;

            IsActive = false;
            IsEnded = true;

            await UpdateVideoAsync();

            videoEndedEvent.Publish();
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

        private void UpdateImage(Segment clip)
        {
            if (!frame.Empty())
            {
                var current = frame
                    .Clone(clip.Rect.Value);

                clip.Images.Enqueue(current);

                if (clip.Images.Count >= settingsService.Contents.Video.ImagesQueueSize)
                {
                    if (clip.Images.Count > settingsService.Contents.Video.ImagesQueueSize)
                    {
                        clip.Images.Dequeue();
                    }

                    var blendedImage = clip.Images.AsBlended();

                    var noiselessImage = clip.Area.NoiseRemoval == 0
                        ? blendedImage
                        : blendedImage.WithoutNoise(
                            erodeIterations: clip.Area.NoiseRemoval,
                            dilateIterations: clip.Area.NoiseRemoval);

                    var thresholdMonochrome = clip.Area.ThresholdMonochrome / Constants.ThresholdDivider;

                    var monochromeImage = noiselessImage
                        .ToMonochrome(thresholdMonochrome);

                    var contourRectangle = !settingsService.Contents.Video.NoCropping
                        ? monochromeImage.GetContour()
                        : default;

                    clip.Mat = contourRectangle.HasValue
                        ? monochromeImage.ToCropped(contourRectangle.Value)
                        : monochromeImage;

                    if (clip.Mat != default
                        && widthMax > 0
                        && heightMax > 0)
                    {
                        var centeredImage = clip.Mat.ToCentered(
                            fullWidth: widthMax,
                            fullHeight: heightMax);

                        if (centeredImage.HasValue())
                        {
                            var bitmapStream = centeredImage.ToMemoryStream();

                            clip.Bitmap = new Bitmap(bitmapStream);
                        }
                    }

                    segmentDrawnEvent.Publish(clip);

                    UpdateValue(clip);
                }
            }
        }

        private void UpdateRectangles(Area area)
        {
            if (frame != default
                && area?.HasDimensions == true
                && AreaService.Areas.Contains(area))
            {
                var size = frame.Size();

                var areaY1 = area.Y1 * size.Height;
                var areaY2 = area.Y2 * size.Height;

                var areaX1 = area.X1 * size.Width;
                var areaX2 = area.X2 * size.Width;

                var width = (areaX2 - areaX1) / (double)area.Segments.Count();

                var index = 0;

                foreach (var clip in area.Segments)
                {
                    var clipX1 = areaX1 + (width * index);
                    var clipX2 = clip != area.Segments.Last()
                        ? areaX1 + (width * ++index)
                        : areaX2;

                    clip.Rect = size.GetRectangle(
                        firstX: clipX1,
                        firstY: areaY1,
                        secondX: clipX2,
                        secondY: areaY2);
                }
            }
        }

        private void UpdateValue(Segment clip)
        {
            var waitingDuration = TimeSpan.FromMilliseconds(Math.Abs(settingsService.Contents.Detection.WaitingDuration));
            var thresholdMatching = Math.Abs(settingsService.Contents.Detection.ThresholdMatching) / Constants.ThresholdDivider;

            var given = clip.Value;

            if (clip.Mat == default)
            {
                clip.SetValue(
                    value: clip.Area.Template?.Empty,
                    similarity: 0,
                    waitingDuration: waitingDuration);
            }
            else if (clip.Area.Template != default)
            {
                var match = clip
                    .GetMatches(settingsService.Contents.Detection.NoMultiComparison)
                    .OrderByDescending(m => m.Key >= thresholdMatching)
                    .ThenByDescending(m => m.Key).FirstOrDefault();

                var similarity = Convert.ToInt32(match.Key * Constants.ThresholdDivider);

                if (match.Value != default
                    && match.Key >= thresholdMatching)
                {
                    clip.SetValue(
                        value: match.Value.Value,
                        similarity: similarity,
                        waitingDuration: waitingDuration);

                    if (AreaService.Segment == clip
                        && match.Value.Type != SampleType.Similar)
                    {
                        match.Value.Type = SampleType.Similar;

                        sampleUpdatedEvent.Publish(match.Value);
                    }
                }
                else
                {
                    clip.SetValue(
                        value: clip.Area.Template.Empty,
                        similarity: similarity,
                        waitingDuration: waitingDuration);
                }
            }

            if (clip.Value != given)
            {
                segmentUpdatedEvent.Publish(clip);
            }
        }

        private async Task UpdateVideoAsync(DateTime? capturingStart = default)
        {
            videoUpdatedEvent.Publish();

            var delay = settingsService.Contents.Video.ProcessingDelay + Constants.DelayUpdateMin;

            await Task.Delay(delay);

            ProcessingTime = capturingStart.HasValue
                ? DateTime.Now - capturingStart
                : default;
        }

        #endregion Private Methods
    }
}