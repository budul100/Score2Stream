using System.Collections.Generic;
using OpenCvSharp;
using Score2Stream.Commons.Models.Contents;

namespace Score2Stream.Commons.Interfaces
{
    public interface IRecognitionService
    {
        #region Public Properties

        public bool IsTrained { get; }

        #endregion Public Properties

        #region Public Methods

        (string Value, float Confidence) Recognize(Mat image);

        void Reset();

        void Train(IEnumerable<Sample> samples, int epochs = 50, float learningRate = 0.01f);

        #endregion Public Methods
    }
}