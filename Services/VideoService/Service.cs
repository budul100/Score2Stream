using System;
using System.Collections.Generic;
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
using Score2Stream.Commons.Events.Input;
using Score2Stream.Commons.Events.Sample;
using Score2Stream.Commons.Events.Segment;
using Score2Stream.Commons.Extensions;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;
using Score2Stream.Commons.Models.Settings;
using Score2Stream.VideoService.Extensions;

namespace Score2Stream.VideoService
{
    public sealed class Service
        : IVideoService
    {
        #region Private Fields

        private readonly object ctsLock = new();
        private readonly IDispatcherService dispatcherService;
        private readonly ReaderWriterLockSlim frameLock = new();
        private readonly InputDrawnEvent inputDrawnEvent;
        private readonly InputEndedEvent inputEndedEvent;
        private readonly InputStartedEvent inputStartedEvent;
        private readonly InputUpdatedEvent inputUpdatedEvent;
        private readonly ILogger<Service> logger;
        private readonly SampleUpdatedEvent sampleUpdatedEvent;
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
        private Segment lastActive;
        private Task serviceTask;
        private int widthLast;
        private int widthMax;

        #endregion Private Fields

        #region Public Constructors

        public Service(ISettingsService<Session> settingsService, IAreaService areaService,
            ITemplateService templateService, Func<IVideoCapture> videoCaptureFactory,
            IDispatcherService dispatcherService, IEventAggregator eventAggregator,
            IRecognitionService recognitionService, ILogger<Service> logger = default)
        {
            AreaService = areaService;
            TemplateService = templateService;
            RecognitionService = recognitionService;

            this.dispatcherService = dispatcherService;
            this.settingsService = settingsService;
            this.videoCaptureFactory = videoCaptureFactory;
            this.logger = logger;

            inputStartedEvent = eventAggregator.GetEvent<InputStartedEvent>();
            inputEndedEvent = eventAggregator.GetEvent<InputEndedEvent>();
            inputDrawnEvent = eventAggregator.GetEvent<InputDrawnEvent>();
            inputUpdatedEvent = eventAggregator.GetEvent<InputUpdatedEvent>();

            segmentDrawnEvent = eventAggregator.GetEvent<SegmentDrawnEvent>();
            segmentUpdatedEvent = eventAggregator.GetEvent<SegmentUpdatedEvent>();

            sampleUpdatedEvent = eventAggregator.GetEvent<SampleUpdatedEvent>();

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

        public IRecognitionService RecognitionService { get; }

        public ITemplateService TemplateService { get; }

        #endregion Public Properties

        #region Public Methods

        void IDisposable.Dispose()
        {
            DisposeAsync().AsTask()
                .GetAwaiter()
                .GetResult();
        }

        public async ValueTask DisposeAsync()
        {
            if (isDisposed) return;

            isDisposed = true;

            CancellationTokenSource cts;

            lock (ctsLock)
            {
                cts = cancellationTokenSource;
                cancellationTokenSource = default;
            }

            if (cts != default && !cts.IsCancellationRequested)
            {
                try { cts.Cancel(); }
                catch (ObjectDisposedException) { }
            }

            if (serviceTask != null)
            {
                try
                {
                    await serviceTask
                        .WaitAsync(TimeSpan.FromSeconds(5))
                        .ConfigureAwait(false);
                }
                catch { }
            }

            cts?.Dispose();
            frameLock?.Dispose();

            GC.SuppressFinalize(this);
        }

        public async Task RunAsync(Input input)
        {
            lock (ctsLock)
            {
                if (serviceTask?.IsCompleted == false)
                    return;

                this.input = input;

                var oldTokenSource = cancellationTokenSource;
                cancellationTokenSource = new CancellationTokenSource();

                oldTokenSource?.Cancel();
                oldTokenSource?.Dispose();

                IsStarted = true;

                serviceTask = Task.Run(
                    function: () => RunAsync(
                        deviceId: input.DeviceId,
                        fileName: input.FileName),
                    cancellationToken: cancellationTokenSource.Token);
            }

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

        public async Task StopAsync()
        {
            try
            {
                lock (ctsLock)
                {
                    cancellationTokenSource?.Cancel();
                }
            }
            catch (ObjectDisposedException) { }

            if (serviceTask != null)
            {
                var timeout = TimeSpan.FromSeconds(Constants.ShutdownTimeoutSecs);

                try
                {
                    await serviceTask.WaitAsync(timeout);
                }
                catch { }
            }
        }

        #endregion Public Methods

        #region Private Methods

        private async Task CaptureAsync(int? deviceId, IVideoCapture video, CancellationToken cancellationToken)
        {
            var hasContent = false;

            var frameCount = 0.0;
            var frameIndex = 0.0;

            var stopwatch = new Stopwatch();

            do
            {
                if (isDisposed) break;

                stopwatch.Restart();

                using var currentFrame = new Mat();

                hasContent = video.Read(currentFrame);

                if (!currentFrame.Empty())
                {
                    var rotated = currentFrame
                        .AsRotated(input.Rotation)
                        .AsTranslated(
                            offsetX: input.OffsetX,
                            offsetY: input.OffsetY);

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

                    await dispatcherService.InvokeAsync(
                        action: () => RefreshInput(bitmap),
                        cancellationToken: cancellationToken);
                }

                var segments = AreaService?.Areas?
                    .SelectMany(a => a.Segments)
                    .Where(c => c.Rect.HasValue).ToArray();

                if (segments?.Length > 0)
                {
                    Interlocked.Exchange(
                        location1: ref heightMax,
                        value: segments.Max(a => a.Rect.Value.Height));

                    Interlocked.Exchange(
                        location1: ref widthMax,
                        value: segments.Max(a => a.Rect.Value.Width));

                    var segmentRefreshs = RefreshSegments(segments).ToArray();

                    await dispatcherService.InvokeAsync(
                        actions: segmentRefreshs,
                        cancellationToken: cancellationToken);

                    UdpateSegments(segments);
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

                inputUpdatedEvent.Publish();

                var delay = settingsService.Contents.Video.DelayProcessing + Constants.UpdateDelay;

                await Task.Delay(
                    millisecondsDelay: delay,
                    cancellationToken: cancellationToken);

                stopwatch.Stop();

                ProcessingTime = stopwatch.Elapsed;

                if (!deviceId.HasValue
                    && settingsService.Contents.Video.DelayProcessing > 0
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

        private Mat GetImage(Segment segment)
        {
            if (isDisposed) return default;

            Mat segmentImage;

            frameLock.EnterReadLock();

            try
            {
                if (frame == null || frame.Empty())
                    return default;

                segmentImage = frame.Clone(segment.Rect.Value);
            }
            finally
            {
                frameLock.ExitReadLock();
            }

            var grayImage = segmentImage.Channels() > 1
                ? segmentImage.CvtColor(ColorConversionCodes.BGR2GRAY)
                : segmentImage;

            if (!ReferenceEquals(
                objA: grayImage,
                objB: segmentImage))
            {
                segmentImage.Dispose();
            }

            if (segment.Area.NoiseRemoval > 0)
            {
                var noiselessImage = grayImage.WithoutNoise(
                    erodeIterations: segment.Area.NoiseRemoval,
                    dilateIterations: segment.Area.NoiseRemoval);

                grayImage.Dispose();

                grayImage = noiselessImage;
            }

            var thresh = 255 *
                (segment.Area.ThresholdMonochrome / Constants.ThresholdDivider);

            var monochromeImage = grayImage.Threshold(
                thresh: thresh,
                maxval: 255,
                type: ThresholdTypes.Binary);

            grayImage.Dispose();

            if (!settingsService.Contents.Video.NoCropping)
            {
                var contourRectangle = monochromeImage.GetContour();

                if (contourRectangle.HasValue)
                {
                    var croppedImage = monochromeImage.Clone(contourRectangle.Value);

                    monochromeImage.Dispose();

                    monochromeImage = croppedImage;
                }
            }

            if (!monochromeImage.HasValue() || widthMax <= 0 || heightMax <= 0)
            {
                monochromeImage.Dispose();

                return default;
            }

            var centeredImage = monochromeImage.AsCentered(
                fullWidth: widthMax,
                fullHeight: heightMax);

            if (!ReferenceEquals(
                objA: centeredImage,
                objB: monochromeImage))
            {
                monochromeImage.Dispose();
            }

            return centeredImage;
        }

        private void RefreshInput(Bitmap bitmap)
        {
            var given = Bitmap;

            Bitmap = bitmap;

            inputDrawnEvent.Publish();

            given?.Dispose();
        }

        private void RefreshSegment(Segment segment, Bitmap bitmap)
        {
            var given = segment.Bitmap;

            segment.Bitmap = bitmap;

            segmentDrawnEvent.Publish(segment);

            given?.Dispose();
        }

        private IEnumerable<Action> RefreshSegments(IEnumerable<Segment> segments)
        {
            if (!isDisposed)
            {
                foreach (var segment in segments)
                {
                    segment.Image = default;

                    var current = GetImage(segment);

                    if (current.HasValue())
                    {
                        segment.Images.Enqueue(current);

                        if (segment.Images.Count >= settingsService.Contents.Video.ImagesQueueSize)
                        {
                            while (segment.Images.Count > settingsService.Contents.Video.ImagesQueueSize)
                            {
                                var oldImages = segment.Images.Dequeue();
                                oldImages?.Dispose();
                            }

                            segment.Image = segment.Images.AsBlended();
                        }
                    }

                    var bitmap = default(Bitmap);

                    if (segment.Image.HasValue() == true)
                    {
                        using var stream = segment.Image.ToMemoryStream();
                        bitmap = new Bitmap(stream);
                    }

                    yield return () => RefreshSegment(
                        segment: segment,
                        bitmap: bitmap);
                }
            }
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
                using var video = videoCaptureFactory.Invoke();

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

                    await dispatcherService.InvokeAsync(() => inputEndedEvent.Publish(input));
                }
                else
                {
                    frame?.Dispose();
                    frame = null;
                }
            }
        }

        private void UdpateSamples(Segment segment, Sample[] samples,
            Match[] matches)
        {
            var templateSamples = TemplateService.Active?.Samples?.ToArray();

            if (templateSamples?.Length > 0)
            {
                if (AreaService?.ActiveSegment == segment)
                {
                    if (samples?.Length > 0
                        && templateSamples?.SequenceEqual(samples) == true)
                    {
                        for (var index = 0; index < samples.Length; index++)
                        {
                            var sample = samples[index];

                            sample.Match = matches?.Length > 0
                                ? matches[index]
                                : default;

                            sampleUpdatedEvent.Publish(sample);
                        }
                    }
                    else
                    {
                        var templateMatches = RecognitionService.GetMatches(
                            segment: segment,
                            samples: templateSamples).ToArray();

                        var templateMatch = templateMatches?
                            .Where(m => m?.Type != MatchType.None)
                            .OrderByDescending(m => m?.Similarity)?.FirstOrDefault();

                        if (templateMatch != default)
                        {
                            templateMatch.Type = MatchType.Match;
                        }

                        for (var index = 0; index < templateSamples.Length; index++)
                        {
                            var sample = templateSamples[index];

                            sample.Match = templateMatches?.Length > 0
                                ? templateMatches[index]
                                : default;

                            sampleUpdatedEvent.Publish(sample);
                        }
                    }

                    lastActive = segment;
                }
                else if (AreaService?.ActiveSegment == default
                    && lastActive == segment)
                {
                    foreach (var templateSample in templateSamples)
                    {
                        templateSample.Match = default;

                        sampleUpdatedEvent.Publish(templateSample);
                    }

                    lastActive = default;
                }
            }
        }

        private void UdpateSegments(IEnumerable<Segment> segments)
        {
            if (!isDisposed)
            {
                foreach (var segment in segments)
                {
                    RecognitionService.Bind(segment);

                    var samples = segment?.Area?.Template?.Samples?.ToArray();

                    var matches = RecognitionService.GetMatches(
                        segment: segment,
                        samples: samples).ToArray();

                    UpdateMatch(
                        segment: segment,
                        matches: matches);

                    segmentUpdatedEvent.Publish(segment);

                    UdpateSamples(
                        segment: segment,
                        samples: samples,
                        matches: matches);
                }
            }
        }

        private void UpdateMatch(Segment segment, Match[] matches)
        {
            var match = matches?
                .Where(m => m?.Type != MatchType.None)
                .OrderByDescending(m => m?.Similarity)?.FirstOrDefault();

            match ??= RecognitionService.Detect(segment);

            var waitingDurationMS = Math.Abs(settingsService.Contents
                .Detection.DurationDetectionWait);
            var waitingDuration = TimeSpan.FromMilliseconds(waitingDurationMS);

            if (match != default)
            {
                match.Type = MatchType.Match;

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

                foreach (var segment in area.Segments)
                {
                    var segmentX1 = areaX1 + (width * index);
                    var segmentX2 = segment != area.Segments.Last()
                        ? areaX1 + (width * ++index)
                        : areaX2;

                    segment.Rect = size.GetRectangle(
                        firstX: segmentX1,
                        firstY: areaY1,
                        secondX: segmentX2,
                        secondY: areaY2);
                }
            }
            finally
            {
                frameLock.ExitReadLock();
            }
        }

        #endregion Private Methods
    }
}