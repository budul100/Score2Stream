using System;
using System.Collections.Generic;
using OpenCvSharp;
using Score2Stream.Commons.Models.Base;
using Score2Stream.Commons.Models.Contents;

namespace Score2Stream.Commons.Interfaces
{
    public interface IRecognitionService
        : IDisposable
    {
        #region Public Methods

        Match GetFromBase(Imageable imageable);

        IEnumerable<Match> GetFromSamples(Segment segment);

        bool HasSimilars(Segment segment);

        void Update(Imageable imageable);

        #endregion Public Methods
    }
}