using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Avalonia.Media.Imaging;
using OpenCvSharp;
using Prism.Events;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Enums;
using Score2Stream.Commons.Events.Area;
using Score2Stream.Commons.Events.Clip;
using Score2Stream.Commons.Events.Input;
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

        private readonly object ctsLock = new();
        private readonly IDispatcherService dispatcherService;
        private readonly ReaderWriterLockSlim frameLock = new();
        private readonly InputEndedEvent inputEndedEvent;
        private readonly InputStartedEvent inputStartedEvent;
        private readonly InputUpdatedEvent inputUpdatedEvent;
        private readonly ILogger<Service> logger;
        private readonly IRecognitionService recognitionService;
        private readonly SegmentDrawnEvent segmentDrawnEvent;
        private readonly SegmentUpdatedEvent segmentUpdatedEvent;
        private readonly ISettingsService<Session> settingsService;
        private readonly Func<IVideoCapture> videoCaptureFactory;
        private CancellationTokenSource cancellationTokenSource;
        private Mat frame;
        private int heightLast;
        private int heightMax;
        private Input input;
        private volatile bool isDisposed;
        private Task serviceTask;
        private int widthLast;
        private int widthMax;

        #endregion Private Fields

        #region Public Constructors

        public Service(ISettingsService<Session> settingsService, IAreaService areaService,
            Func<IVideoCapture> videoCaptureFactory, IDispatcherService dispatcherService,
            IEventAggregator eventAggregator, IRecognitionService recognitionService,
            ILogger<Service> logger = default)
        {
            this.dispatcherService = dispatcherService;
            this.recognitionService = recognitionService;
            this.settingsService = settingsService;
            this.logger = logger;

            AreaService = areaService;
            this.videoCaptureFactory = videoCaptureFactory;
            inputStartedEvent = eventAggregator.GetEvent<InputStartedEvent>();
            inputEndedEvent = eventAggregator.GetEvent<InputEndedEvent>();
            inputUpdatedEvent = eventAggregator.GetEvent<InputUpdatedEvent>();

            segmentDrawnEvent = eventAggregator.GetEvent<SegmentDrawnEvent>();
            segmentUpdatedEvent = eventAggregator.GetEvent<SegmentUpdatedEvent>();

            eventAggregator.GetEvent<AreaModifiedEvent>().Subscribe(
                action: UpdateRectangles,
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public IAreaService AreaService { get; }

        public Bitmap Bitmap { get; private set; }

        public bool IsActive { get; private set; }

        public bool IsStarted { get; private set; }

        public string Name => input?.Name;

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
            if (serviceTask?.IsCompleted != false)
            {
                this.input = input;

                CancellationTokenSource oldTokenSource;

                lock (ctsLock)
                {
                    oldTokenSource = cancellationTokenSource;
                    cancellationTokenSource = new CancellationTokenSource();
                }

                oldTokenSource?.Cancel();
                oldTokenSource?.Dispose();

                IsStarted = true;

                serviceTask = Task.Run(
                    function: () => RunAsync(
                        deviceId: input.DeviceId,
                        fileName: input.FileName),
                    cancellationToken: cancellationTokenSource.Token);

                try
                {
                    await serviceTask;
                }
                catch (Exception ex)
                {
                    logger?.LogError(
                        exception: ex,
                        message: "Start capturing failed.");
                }
            }
        }

        public void Stop()
        {
            try
            {
                lock (ctsLock)
                {
                    cancellationTokenSource?.Cancel();
                }
            }
            catch (ObjectDisposedException) { }
        }

        #endregion Public Methods

        #region Protected Methods

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                isDisposed = true;

                if (disposing)
                {
                    CancellationTokenSource cts;

                    lock (ctsLock)
                    {
                        cts = cancellationTokenSource;
                        cancellationTokenSource = default;
                    }

                    if (cts != default
                        && !cts.IsCancellationRequested)
                    {
                        try
                        {
                            cts.Cancel();
                        }
                        catch (ObjectDisposedException) { }
                    }

                    // Warten bis serviceTask beendet ist, bevor frameLock disposed wird
                    try
                    {
                        serviceTask?.Wait(TimeSpan.FromSeconds(5));
                    }
                    catch { }

                    cts?.Dispose();
                    frameLock?.Dispose();
                }
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private async Task CaptureAsync(int? deviceId, IVideoCapture video,
            CancellationToken cancellationToken)
        {
            var hasContent = false;

            var frameCount = 0.0;
            var frameIndex = 0.0;

            do
            {
                if (isDisposed) break;

                using var currentFrame = new Mat();

                hasContent = video.Read(currentFrame);

                var capturingStart = DateTime.Now;

                if (!currentFrame.Empty())
                {
                    var rotated = currentFrame.Clone()
                        .AsRotated(input.Rotation);

                    frameLock.EnterWriteLock();

                    try
                    {
                        frame?.Dispose();
                        frame = rotated;
                    }
                    finally
                    {
                        frameLock.ExitWriteLock();
                    }

                    var size = rotated.Size();

                    if (size.Width != widthLast || size.Height != heightLast)
                    {
                        foreach (var area in AreaService.Areas)
                        {
                            UpdateRectangles(area);
                        }
                    }

                    heightLast = size.Height;
                    widthLast = size.Width;

                    using var converted = new Mat();

                    var bitmap = converted.GetBitmap(rotated);

                    Bitmap = await dispatcherService.InvokeAsync(
                        function: () => bitmap,
                        cancellationToken: cancellationToken);
                }

                var clips = AreaService?.Areas?
                    .SelectMany(a => a.Segments)
                    .Where(c => c.Rect.HasValue).ToArray();

                if (clips?.Length > 0)
                {
                    Interlocked.Exchange(
                        location1: ref heightMax,
                        value: clips.Max(a => a.Rect.Value.Height));

                    Interlocked.Exchange(
                        location1: ref widthMax,
                        value: clips.Max(a => a.Rect.Value.Width));

                    await Task.WhenAll(clips.Select(clip => UpdateBitmapAsync(
                        segment: clip,
                        cancellationToken: cancellationToken)));
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
                && !cancellationToken.IsCancellationRequested);
        }

        private Mat GetImage(Segment clip)
        {
            if (isDisposed) return default;

            var clipImage = default(Mat);

            frameLock.EnterReadLock();

            try
            {
                if (frame == null || frame.Empty())
                    return default;

                clipImage = frame.Clone(clip.Rect.Value);
            }
            finally
            {
                frameLock.ExitReadLock();
            }

            var noiselessImage = clip.Area.NoiseRemoval == 0
                ? clipImage
                : clipImage.WithoutNoise(
                    erodeIterations: clip.Area.NoiseRemoval,
                    dilateIterations: clip.Area.NoiseRemoval);

            if (!ReferenceEquals(noiselessImage, clipImage))
            {
                clipImage.Dispose();
            }

            var thresholdMonochrome = clip.Area.ThresholdMonochrome / Constants.ThresholdDivider;
            var monochromeImage = noiselessImage.AsMonochrome(thresholdMonochrome);

            noiselessImage.Dispose();

            var contourRectangle = !settingsService.Contents.Video.NoCropping
                ? monochromeImage.GetContour()
                : default;

            var contourImage = contourRectangle.HasValue
                ? monochromeImage.AsCropped(contourRectangle.Value)
                : monochromeImage;

            if (!ReferenceEquals(
                objA: contourImage,
                objB: monochromeImage))
            {
                monochromeImage.Dispose();
            }

            if (!contourImage.HasValue() || widthMax <= 0 || heightMax <= 0)
            {
                contourImage.Dispose();

                return default;
            }

            var centeredImage = contourImage.AsCentered(
                fullWidth: widthMax,
                fullHeight: heightMax);

            contourImage.Dispose();

            return centeredImage;
        }

        private async Task RunAsync(int? deviceId, string fileName)
        {
            CancellationToken cancellationToken;

            lock (ctsLock)
            {
                if (cancellationTokenSource == null)
                    return;

                cancellationToken = cancellationTokenSource.Token;
            }

            try
            {
                await UpdateVideoAsync();

                using var video = videoCaptureFactory();

                if (deviceId.HasValue)
                {
                    if (!video.Open(deviceId.Value))
                    {
                        throw new ApplicationException(
                            message: $"Cannot connect to device {Name}.");
                    }
                }
                else
                {
                    if (!System.IO.File.Exists(fileName))
                    {
                        throw new System.IO.FileNotFoundException(
                            message: $"The file {fileName} could not be found.");
                    }
                    else if (!video.Open(fileName))
                    {
                        throw new ApplicationException(
                            message: $"Cannot open file {fileName}.");
                    }
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    IsActive = true;

                    await dispatcherService.InvokeAsync(() => inputStartedEvent.Publish(input));

                    await CaptureAsync(
                        deviceId: deviceId,
                        video: video,
                        cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger?.LogError(
                    exception: ex,
                    message: "Input capturing failed.");

                Debugger.Break();
            }
            finally
            {
                Bitmap = default;

                IsStarted = false;
                IsActive = false;

                if (!isDisposed)
                {
                    frameLock.EnterWriteLock();

                    try
                    {
                        frame?.Dispose();
                        frame = null;
                    }
                    finally
                    {
                        frameLock.ExitWriteLock();
                    }

                    await UpdateVideoAsync();

                    await dispatcherService.InvokeAsync(() => inputEndedEvent.Publish(input));
                }
                else
                {
                    frame?.Dispose();
                    frame = null;
                }
            }
        }

        private async Task UpdateBitmapAsync(Segment segment, CancellationToken cancellationToken)
        {
            if (isDisposed) return;

            segment.Mat = default;

            var current = GetImage(segment);

            if (current.HasValue())
            {
                segment.Images.Enqueue(current);

                if (segment.Images.Count >= settingsService.Contents.Video.ImagesQueueSize)
                {
                    if (segment.Images.Count > settingsService.Contents.Video.ImagesQueueSize)
                    {
                        segment.Images.Dequeue();
                    }

                    segment.Mat = segment.Images.AsBlended();
                }
            }

            if (segment.Mat.HasValue() == true)
            {
                var bitmapStream = segment.Mat.ToMemoryStream();

                segment.Bitmap = await dispatcherService.InvokeAsync(
                    function: () => new Bitmap(bitmapStream),
                    cancellationToken: cancellationToken);
            }
            else
            {
                segment.Bitmap = await dispatcherService.InvokeAsync(
                    function: () => default(Bitmap),
                    cancellationToken: cancellationToken);
            }

            await dispatcherService.InvokeAsync(
                action: () => segmentDrawnEvent.Publish(segment),
                cancellationToken: cancellationToken);

            await UpdateValueAsync(
                segment: segment,
                cancellationToken: cancellationToken);
        }

        private void UpdateRectangles(Area area)
        {
            if (isDisposed) return;

            frameLock.EnterReadLock();

            try
            {
                if (frame == null
                    || area?.HasDimensions != true
                    || area?.Segments?.Count() == 0
                    || !AreaService.Areas.Contains(area)) return;

                var size = frame.Size();

                var areaY1 = area.Y1 * size.Height;
                var areaY2 = area.Y2 * size.Height;

                var areaX1 = area.X1 * size.Width;
                var areaX2 = area.X2 * size.Width;

                var width = (areaX2 - areaX1) / (double)area.Segments.Count();

                var index = 0;

                foreach (var segement in area.Segments)
                {
                    var clipX1 = areaX1 + (width * index);
                    var clipX2 = segement != area.Segments.Last()
                        ? areaX1 + (width * ++index)
                        : areaX2;

                    segement.Rect = size.GetRectangle(
                        firstX: clipX1,
                        firstY: areaY1,
                        secondX: clipX2,
                        secondY: areaY2);
                }
            }
            finally
            {
                frameLock.ExitReadLock();
            }
        }

        private async Task UpdateValueAsync(Segment segment, CancellationToken cancellationToken)
        {
            segment.Matches = recognitionService
                .GetMatches(segment.Mat).ToArray();

            var match = segment.Matches?
                .Where(m => m.Type == MatchType.Similar)
                .OrderByDescending(m => m.Similarity).FirstOrDefault();

            if (match != default)
            {
                match.Type = MatchType.Match;
            }
            else
            {
                match = recognitionService.GetValue(segment.Mat);

                if (match != default)
                {
                    match.Type = MatchType.Match;
                }
            }

            var waitingDurationMS = Math.Abs(settingsService.Contents.Detection.DurationDetectionWait);
            var waitingDuration = TimeSpan.FromMilliseconds(waitingDurationMS);

            if (match != default)
            {
                segment.SetValue(
                    value: match.Value,
                    hasValue: true,
                    similarity: match.Similarity,
                    waitingDuration: waitingDuration);
            }
            else
            {
                var value = segment.Area?.Template?.Empty;

                segment.SetValue(
                    value: value,
                    hasValue: false,
                    similarity: 0.0,
                    waitingDuration: waitingDuration);
            }

            await dispatcherService.InvokeAsync(
                action: () => segmentUpdatedEvent.Publish(segment),
                cancellationToken: cancellationToken);
        }

        private async Task UpdateVideoAsync(DateTime? capturingStart = default)
        {
            await dispatcherService.InvokeAsync(
                action: inputUpdatedEvent.Publish);

            var delay = settingsService.Contents.Video.ProcessingDelay + Constants.UpdateDelay;

            await Task.Delay(delay);

            ProcessingTime = capturingStart.HasValue
                ? DateTime.Now - capturingStart
                : default;
        }

        #endregion Private Methods
    }
}