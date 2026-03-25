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

        void Bind(Matchable imageable);

        Match Detect(Matchable imageable);

        IEnumerable<Match> GetMatches(Segment segment, IEnumerable<Sample> samples);

        bool HasSimilars(Segment segment, IEnumerable<Sample> samples);

        #endregion Public Methods
    }
}