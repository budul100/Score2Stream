using System.Collections.Generic;
using OpenCvSharp;
using Score2Stream.Commons.Models.Contents;

namespace Score2Stream.Commons.Interfaces
{
    public interface IRecognitionService
    {
        #region Public Methods

        void Add(Sample sample);

        Match GetModelMatch(Mat image);

        IEnumerable<Match> GetSampleMatches(Mat image);

        void Remove(Sample sample);

        #endregion Public Methods
    }
}