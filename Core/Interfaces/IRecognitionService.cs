using OpenCvSharp;

namespace Score2Stream.Core.Interfaces
{
    public interface IRecognitionService
    {
        #region Public Methods

        string Recognize(Mat image);

        #endregion Public Methods
    }
}