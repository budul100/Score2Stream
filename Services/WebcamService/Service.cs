using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using ScoreboardOCR.Core.Interfaces;
using ScoreboardOCR.Core.Models;
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

        private readonly List<(Clip, Rect?)> clips = new();
        private readonly IDictionary<string, Mat> templates = new Dictionary<string, Mat>();
        private readonly IDispatcherService wpfContext;

        private CancellationTokenSource cancellationTokenSource;
        private Mat frame;
        private bool isDisposed;
        private Task webcamTask;

        #endregion Private Fields

        #region Public Constructors

        public Service(IDispatcherService dispatcherService)
        {
            this.wpfContext = dispatcherService;
        }

        #endregion Public Constructors

        #region Public Events

        public event EventHandler OnContentChangedEvent;

        #endregion Public Events

        #region Public Properties

        public BitmapSource Content { get; private set; }

        public double ThresholdCompare { get; set; }

        public double ThresholdMonochrome { get; set; }

        #endregion Public Properties

        #region Public Methods

        public void AddClip(Clip clip)
        {
            if (frame != default)
            {
                RemoveClip(clip);

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
                    var value = (clip, rectangle);
                    clips.Add(value);
                }
            }
        }

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

        public void RemoveClip(Clip clip)
        {
            var relevant = clips
                .SingleOrDefault(c => c.Item1 == clip);

            if (relevant != default)
            {
                clips.Remove(relevant);
            }
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

                using var currentFrame = new Mat();

                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    video.Read(currentFrame);

                    if (!currentFrame.Empty())
                    {
                        frame = currentFrame;

                        Content = currentFrame.ToBitmapSource();

                        foreach (var clip in clips)
                        {
                            if (clip.Item2.HasValue)
                            {
                                var cropImage = frame
                                    .Clone(clip.Item2.Value)
                                    .ToMonochrome(clip.Item1.ThresholdMonochrome);

                                var contourRectangle = cropImage.GetContour();

                                if (contourRectangle.HasValue)
                                {
                                    clip.Item1.Content = cropImage
                                        .Clone(contourRectangle.Value)
                                        .ToBitmapSource();
                                }
                            }
                        }

                        OnContentChangedEvent?.Invoke(
                            sender: this,
                            e: default);
                    }

                    await Task.Delay(Delay);
                }
            }
            catch { }

            frame = default;
            Content = default;
        }

        #endregion Private Methods
    }
}