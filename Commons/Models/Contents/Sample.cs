using System.Text.Json.Serialization;
using Score2Stream.Commons.Models.Base;

namespace Score2Stream.Commons.Models.Contents
{
    public class Sample
        : Imageable
    {
        #region Public Properties

        public byte[] Bytes { get; set; }

        public double Height { get; set; }

        [JsonIgnore]
        public int Index { get; set; }

        [JsonIgnore]
        public bool IsFiltered { get; set; }

        public bool IsVerified { get; set; }

        [JsonIgnore]
        public Match Match { get; set; }

        [JsonIgnore]
        public Template Template { get; set; }

        public string Value { get; set; }

        public double Width { get; set; }

        #endregion Public Properties
    }
}