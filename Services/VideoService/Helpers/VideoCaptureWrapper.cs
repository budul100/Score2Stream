using System;
using OpenCvSharp;
using Score2Stream.Commons.Interfaces;

namespace Score2Stream.VideoService.Helpers
{
    public class VideoCaptureWrapper
        : IVideoCapture
    {
        #region Private Fields

        private readonly VideoCapture videoCapture;

        #endregion Private Fields

        #region Public Constructors

        public VideoCaptureWrapper()
        {
            videoCapture = new VideoCapture();
        }

        #endregion Public Constructors

        #region Public Methods

        public void Dispose()
        {
            videoCapture?.Dispose();
            GC.SuppressFinalize(this);
        }

        public double Get(VideoCaptureProperties propertyId) => videoCapture.Get(propertyId);

        public bool Open(int index) => videoCapture.Open(index);

        public bool Open(string fileName) => videoCapture.Open(fileName);

        public bool Read(Mat image) => videoCapture.Read(image);

        public bool Set(VideoCaptureProperties propertyId, double value) => videoCapture.Set(
            propertyId: propertyId,
            value: value);

        #endregion Public Methods
    }
}