﻿using Core.Events.Clip;
using Core.Events.Sample;
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

namespace VideoService
{
    public class Service
        : IVideoService
    {
        #region Private Fields

        private const int DefaultDelay = 100;
        private const int DelayMin = 10;
        private const double DividerThreshold = 100;
        private const int ThresholdDetectingDefault = 80;
        private const int ThresholdMatchingDefault = 70;
        private const int WaitingDurationDefault = 50;

        private readonly IDispatcherService dispatcherService;
        private readonly IEventAggregator eventAggregator;
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

        public int ThresholdDetecting { get; set; } = ThresholdDetectingDefault;

        public int ThresholdMatching { get; set; } = ThresholdMatchingDefault;

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

        private IEnumerable<Clip> GetRelevantClips()
        {
            var result = ClipService.Clips
                .Where(c => c.Rect.HasValue);

            return result;
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

                        var relevants = GetRelevantClips().ToArray();

                        foreach (var relevant in relevants)
                        {
                            UpdateClip(relevant);
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

        private void UpdateClip(Clip clip)
        {
            var thresholdMonochrome = clip.ThresholdMonochrome / DividerThreshold;

            var thresholdDetecting = ThresholdDetecting / DividerThreshold;
            var thresholdMatching = ThresholdMatching / DividerThreshold;

            var waitingSpan = TimeSpan.FromMilliseconds(WaitingDuration);

            var monochromImage = frame
                .Clone(clip.Rect.Value)
                .ToMonochrome(thresholdMonochrome);

            var contourRectangle = !NoCentering
                ? monochromImage.GetContour()
                : default;

            if (contourRectangle.HasValue)
            {
                clip.Image = monochromImage
                    .ToCropped(contourRectangle.Value);
            }
            else
            {
                clip.Image = monochromImage;
            }

            if (clip.Image != default)
            {
                clip.Bitmap = clip.Image
                    .ToCentered(
                        fullWidth: maxWidth,
                        fullHeight: maxHeight)
                    .ToBitmapSource();

                clip.SetSimilarities();

                var matchingSample = clip.Template?.Samples?
                    .OrderByDescending(c => c.Similarity).FirstOrDefault();

                if ((matchingSample != default)
                    && (matchingSample.Similarity >= thresholdMatching))
                {
                    clip.SetValue(
                        value: matchingSample?.Value,
                        waitingSpan: waitingSpan);
                }
                else
                {
                    clip.SetValue(
                        value: clip.Template?.ValueEmpty,
                        waitingSpan: waitingSpan);
                }

                if ((clip.Template?.Samples.Any() != true)
                    || ((matchingSample != default) && (matchingSample.Similarity < thresholdDetecting)))
                {
                    eventAggregator
                        .GetEvent<SampleDetectedEvent>()
                        .Publish(clip);
                }
            }
            else
            {
                clip.SetValue(
                    value: clip.Template?.ValueEmpty,
                        waitingSpan: waitingSpan);
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
                    maxHeight = GetRelevantClips().Max(r => r.Rect.Value.Height);
                    maxWidth = GetRelevantClips().Max(r => r.Rect.Value.Width);
                }
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