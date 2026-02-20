using OpenCvSharp;

namespace Score2Stream.Commons.Interfaces
{
    public interface IRecognitionService
    {
        #region Public Methods

        (string Value, float Confidence) Recognize(Mat image);

        #endregion Public Methods
    }
}