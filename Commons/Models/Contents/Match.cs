using Score2Stream.Commons.Enums;

namespace Score2Stream.Commons.Models.Contents
{
    public class Match
    {
        #region Public Properties

        public double Similarity { get; set; }

        public MatchType Type { get; set; }

        public string Value { get; set; }

        #endregion Public Properties
    }
}