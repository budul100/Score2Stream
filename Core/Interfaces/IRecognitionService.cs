namespace Score2Stream.Core.Interfaces
{
    public interface IRecognitionService
    {
        #region Public Methods

        string Recognize(byte[] bytes);

        #endregion Public Methods
    }
}