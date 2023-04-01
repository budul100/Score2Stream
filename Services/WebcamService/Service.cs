using Core.Events;
using Core.Models.Receiver;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using Prism.Events;
using ScoreboardOCR.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using WebcamService.Extensions;
using WebcamService.Models;

namespace WebcamService
{
    public class Service
        : IWebcamService, IDisposable
    {
        #region Private Fields

        private const int Delay = 100;
        private const double thresholdDivider = 100;

        private readonly List<RecClip> contentClips = new();
        private readonly IDispatcherService dispatcherService;
        private readonly IEventAggregator eventAggregator;

        private CancellationTokenSource cancellationTokenSource;
        private Mat frame;
        private bool isDisposed;
        private Task webcamTask;

        #endregion Private Fields

        #region Public Constructors

        public Service(IClipService clipService, IDispatcherService dispatcherService,
            IEventAggregator eventAggregator)
        {
            this.dispatcherService = dispatcherService;
            this.eventAggregator = eventAggregator;

            eventAggregator.GetEvent<ClipUpdatedEvent>().Subscribe(
                action: c => CreateRecClip(c),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<ClipsChangedEvent>().Subscribe(
                action: () => contentClips.RemoveAll(c => !clipService.Clips.Contains(c.Clip)),
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Events

        public event EventHandler OnContentUpdatedEvent;

        #endregion Public Events

        #region Public Properties

        public BitmapSource Content { get; private set; }

        public bool CropContents { get; set; }

        public bool IsActive => Content != default;

        public double ThresholdCompare { get; set; }

        #endregion Public Properties

        #region Public Methods

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task StartAsync()
        {
            if (webcamTask?.IsCompleted == false)
            {
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();

            webcamTask = Task.Run(
                function: async () => dispatcherService.Invoke(() => RunWebcamAsync()),
                cancellationToken: cancellationTokenSource.Token);

            if (webcamTask.IsFaulted)
            {
                // To let the exceptions exit
                await webcamTask;
            }
        }

        public async Task StopAsync()
        {
            if (cancellationTokenSource?.IsCancellationRequested == true)
            {
                return;
            }

            cancellationTokenSource?.Cancel();

            if (webcamTask != default)
            {
                await webcamTask;
            }
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

        private void CreateClipContent(RecClip contentClip)
        {
            var thresholdMonochrome = contentClip.Clip.ThresholdMonochrome / thresholdDivider;

            var cropImage = frame
                .Clone(contentClip.Rect)
                .ToMonochrome(thresholdMonochrome);

            if (CropContents)
            {
                var contourRectangle = cropImage.GetContour();

                contentClip.Clip.Image = cropImage
                    .Clone(contourRectangle.Value);
            }
            else
            {
                contentClip.Clip.Image = cropImage;
            }

            if (contentClip.Clip.Image != default)
            {
                contentClip.Clip.Content = contentClip.Clip.Image
                    .ToBitmapSource();

                if (contentClip.Clip.Template?.Samples?.Any() == true)
                {
                    var compare = contentClip.Clip.Template.Samples
                        .Select(s => (Sample: s, Difference: s.Image.DiffTo(contentClip.Clip.Image)))
                        .Where(x => ThresholdCompare == 0 || x.Difference >= ThresholdCompare)
                        .OrderByDescending(x => x.Difference).FirstOrDefault();

                    contentClip.Clip.Value = compare.Sample.Value;
                }
            }
        }

        private void CreateRecClip(Clip clip)
        {
            if (frame != default
                && clip.HasDimensions)
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

                    contentClips.RemoveAll(c => c.Clip == clip);
                    contentClips.Add(value);
                }
            }
        }

        private async void RunWebcamAsync()
        {
            var contentUpdatedEvent = eventAggregator
                .GetEvent<WebcamUpdatedEvent>();

            try
            {
                //// Creation and disposal of this object should be done in the same thread
                //// because if not it throws disconnectedContext exception
                //var video = new VideoCapture();

                using var video = VideoCapture.FromFile(@"..\..\..\..\Additionals\test_images\test_video.mp4");

                //if (!video.Open(CameraDeviceId))
                //{
                //    throw new ApplicationException("Cannot connect to camera");
                //}

                using var currentFrame = new Mat();

                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    video.Read(currentFrame);

                    if (!currentFrame.Empty())
                    {
                        frame = currentFrame;

                        Content = currentFrame.ToBitmapSource();

                        foreach (var contentClip in contentClips)
                        {
                            CreateClipContent(contentClip);
                        }

                        contentUpdatedEvent.Publish();
                    }

                    await Task.Delay(Delay);
                }
            }
            catch (Exception ex)
            {
                Debugger.Break();
            }

            frame = default;
            Content = default;

            contentUpdatedEvent.Publish();
        }

        #endregion Private Methods
    }
}