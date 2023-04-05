using Core.Events.Clips;
using Core.Events.Video;
using Core.Interfaces;
using Core.Models;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using VideoService.Extensions;
using VideoService.Models;

namespace VideoService
{
    public class Service
        : IVideoService
    {
        #region Private Fields

        private const int DefaultDelay = 300;
        private const int DelayMin = 10;
        private const double ThresholdDivider = 100;

        private readonly IDispatcherService dispatcherService;
        private readonly IEventAggregator eventAggregator;
        private readonly List<RecClip> recClips = new();
        private readonly VideoUpdatedEvent videoUpdatedEvent;

        private CancellationTokenSource cancellationTokenSource;
        private Mat frame;
        private bool isDisposed;
        private int maxHeight;
        private int maxWidth;
        private Task serviceTask;

        #endregion Private Fields

        #region Public Constructors

        public Service(IClipService clipService, IDispatcherService dispatcherService, IEventAggregator eventAggregator)
        {
            ClipService = clipService;

            this.dispatcherService = dispatcherService;
            this.eventAggregator = eventAggregator;

            videoUpdatedEvent = eventAggregator
                .GetEvent<VideoUpdatedEvent>();
        }

        #endregion Public Constructors

        #region Public Properties

        public BitmapSource Bitmap { get; private set; }

        public IClipService ClipService { get; }

        public int Delay { get; set; } = DefaultDelay;

        public bool IsActive => Bitmap != default;

        public string Name { get; private set; }

        public bool NoCentering { get; set; }

        public int ThresholdCompare { get; set; }

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
                action: c => CreateRecClip(c),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<ClipsChangedEvent>().Subscribe(
                action: () => recClips.RemoveAll(c => !ClipService.Clips.Contains(c.Clip)),
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

        private void CreateRecClip(Clip clip)
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

                if (rectangle.HasValue)
                {
                    var value = new RecClip
                    {
                        Clip = clip,
                        Rect = rectangle.Value,
                    };

                    recClips.RemoveAll(c => c.Clip == clip);
                    recClips.Add(value);

                    maxHeight = recClips.Max(r => r.Rect.Height);
                    maxWidth = recClips.Max(r => r.Rect.Width);
                }
            }
        }

        private async void RunAsync(int? deviceId, string fileName)
        {
            try
            {
                //// Creation and disposal of this object should be done in the same thread
                //// because if not it throws disconnectedContext exception

                await UpdateVideoAsync(
                    isEnded: false);

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

                using var currentFrame = new Mat();

                var hasContent = false;

                do
                {
                    hasContent = video.Read(currentFrame);

                    if (!currentFrame.Empty())
                    {
                        frame = currentFrame;

                        Bitmap = currentFrame.ToBitmapSource();

                        foreach (var contentClip in recClips)
                        {
                            UpdateClip(contentClip);
                        }
                    }

                    await UpdateVideoAsync(
                        isEnded: false);
                }
                while (hasContent
                    && !cancellationTokenSource.IsCancellationRequested);
            }
            catch (Exception ex)
            {
                Debugger.Break();
            }

            frame = default;
            Bitmap = default;

            await UpdateVideoAsync(
                isEnded: true);
        }

        private async Task StartAsync(int? deviceId, string fileName)
        {
            if (serviceTask?.IsCompleted == false)
            {
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();

            async Task runTask() => dispatcherService.Invoke(() => RunAsync(
                deviceId: deviceId,
                fileName: fileName));

            serviceTask = Task.Run(
                function: runTask,
                cancellationToken: cancellationTokenSource.Token);

            if (serviceTask.IsFaulted)
            {
                // To let the exceptions exit
                await serviceTask;
            }
        }

        private void UpdateClip(RecClip contentClip)
        {
            var thresholdMonochrome = contentClip.Clip.ThresholdMonochrome / ThresholdDivider;
            var thresholdCompare = ThresholdCompare / ThresholdDivider;

            var monochromImage = frame
                .Clone(contentClip.Rect)
                .ToMonochrome(thresholdMonochrome);

            var contourRectangle = !NoCentering
                ? monochromImage.GetContour()
                : default;

            if (contourRectangle.HasValue)
            {
                contentClip.Clip.Image = monochromImage.ToCentered(
                    contourRectangle: contourRectangle.Value,
                    fullWidth: maxWidth,
                    fullHeight: maxHeight);
            }
            else
            {
                contentClip.Clip.Image = monochromImage;
            }

            if (contentClip.Clip.Image != default)
            {
                contentClip.Clip.Bitmap = contentClip.Clip.Image
                    .ToBitmapSource();

                if (contentClip.Clip.Template?.Samples?.Any() == true)
                {
                    var compare = contentClip.Clip.Template.Samples
                        .Select(s => (Sample: s, Difference: s.Image.DiffTo(contentClip.Clip.Image)))
                        .Where(x => thresholdCompare == 0 || x.Difference <= thresholdCompare)
                        .OrderByDescending(x => x.Difference).FirstOrDefault();

                    contentClip.Clip.Value = compare != default
                        ? compare.Sample?.Value
                        : default;
                }
            }
            else
            {
                contentClip.Clip.Value = default;
            }
        }

        private async Task UpdateVideoAsync(bool isEnded)
        {
            videoUpdatedEvent.Publish();

            if (isEnded)
            {
                eventAggregator.GetEvent<VideoEndedEvent>().Publish();
            }

            await Task.Delay(Delay + DelayMin);
        }

        #endregion Private Methods
    }
}