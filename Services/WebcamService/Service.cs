using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using Prism.Events;
using ScoreboardOCR.Core.Events;
using ScoreboardOCR.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using WebcamService.Extensions;

namespace WebcamService
{
    public class Service
        : IWebcamService, IDisposable
    {
        #region Private Fields

        private const int Delay = 100;

        private readonly IEventAggregator eventAggregator;
        private readonly IDictionary<string, Mat> templates = new Dictionary<string, Mat>();
        private readonly IDispatcherService wpfContext;

        private CancellationTokenSource cancellationTokenSource;
        private bool isDisposed;
        private Task webcamTask;

        #endregion Private Fields

        #region Public Constructors

        public Service(IEventAggregator eventAggregator, IDispatcherService dispatcherService)
        {
            this.eventAggregator = eventAggregator;
            this.wpfContext = dispatcherService;
        }

        #endregion Public Constructors

        #region Public Properties

        public BitmapSource Current { get; private set; }

        public bool IsRunning { get; set; }

        public double ThresholdCompare { get; set; }

        public double ThresholdMonochrome { get; set; }

        #endregion Public Properties

        #region Public Methods

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public string Get(Mat image, int firstX, int firstY, int secondX, int secondY)
        {
            var result = default(string);

            if (templates.Count > 0)
            {
                var converted = GetImage(
                    image: image,
                    firstX: firstX,
                    firstY: firstY,
                    secondX: secondX,
                    secondY: secondY);

                if (converted != default)
                {
                    //Current = converted;

                    var template = templates
                        .OrderByDescending(t => t.Value.DiffTo(converted))
                        .First();

                    if (ThresholdCompare == 0 || template.Value.DiffTo(converted) >= ThresholdCompare)
                    {
                        result = template.Key;
                    }
                }
            }

            return result;
        }

        public void Set(Mat image, int firstX, int firstY, int secondX, int secondY, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                var converted = GetImage(
                    image: image,
                    firstX: firstX,
                    firstY: firstY,
                    secondX: secondX,
                    secondY: secondY);

                if (converted != default)
                {
                    //Current = converted;

                    if (templates.ContainsKey(value))
                    {
                        templates.Add(
                            key: value,
                            value: default);
                    }

                    templates[value] = converted;
                }
            }
        }

        public async Task StartAsync()
        {
            if (webcamTask?.IsCompleted == false)
            {
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();

            webcamTask = Task.Run(
                function: async () => wpfContext.Invoke(() => RunWebcamAsync()),
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

        private Mat GetImage(Mat image, int firstX, int firstY, int secondX, int secondY)
        {
            var result = default(Mat);

            var cropRectangle = image.Size().GetRectangle(
                firstX: firstX,
                firstY: firstY,
                secondX: secondX,
                secondY: secondY);

            if (cropRectangle.HasValue)
            {
                var cropImage = image
                    .Clone(cropRectangle.Value)
                    .ToMonochrome(ThresholdMonochrome);

                var contourRectangle = cropImage.GetContour();

                if (contourRectangle.HasValue)
                {
                    result = cropImage
                        .Clone(contourRectangle.Value);
                }
            }

            return result;
        }

        private async void RunWebcamAsync()
        {
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

                using var frame = new Mat();

                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    video.Read(frame);

                    if (!frame.Empty())
                    {
                        Current = frame.ToBitmapSource();

                        eventAggregator
                            .GetEvent<WebcamChangedEvent>()
                            .Publish();

                        IsRunning = true;
                    }

                    await Task.Delay(Delay);
                }
            }
            catch { }

            IsRunning = false;
        }

        #endregion Private Methods
    }
}