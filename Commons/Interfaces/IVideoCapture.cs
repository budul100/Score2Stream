using System;
using OpenCvSharp;

namespace Score2Stream.Commons.Interfaces
{
    public interface IVideoCapture
        : IDisposable
    {
        #region Public Methods

        double Get(VideoCaptureProperties propertyId);

        bool Open(int index);

        bool Open(string fileName);

        bool Read(Mat image);

        bool Set(VideoCaptureProperties propertyId, double value);

        #endregion Public Methods
    }
}