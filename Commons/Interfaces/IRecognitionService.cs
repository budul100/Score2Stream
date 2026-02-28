using System.Collections.Generic;
using OpenCvSharp;
using Score2Stream.Commons.Models.Contents;

namespace Score2Stream.Commons.Interfaces
{
    public interface IRecognitionService
    {
        #region Public Methods

        void Add(Sample sample);

        IEnumerable<Match> GetMatches(Mat image);

        Match GetValue(Mat image);

        bool HasSimilars(Mat image);

        void Remove(Sample sample);

        #endregion Public Methods
    }
}