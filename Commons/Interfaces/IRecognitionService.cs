using System;
using System.Collections.Generic;
using Score2Stream.Commons.Models.Base;
using Score2Stream.Commons.Models.Contents;

namespace Score2Stream.Commons.Interfaces
{
    public interface IRecognitionService
        : IDisposable
    {
        #region Public Methods

        void Bind(Imageable imageable);

        IEnumerable<(Match Match, Sample Sample)> Compare(Segment segment, IEnumerable<Sample> samples);

        Match Detect(Imageable imageable);

        bool HasSimilars(Segment segment, IEnumerable<Sample> samples);

        #endregion Public Methods
    }
}