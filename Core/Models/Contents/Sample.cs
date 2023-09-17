using Avalonia.Media.Imaging;
using OpenCvSharp;
using System.Text.Json.Serialization;

namespace Score2Stream.Core.Models.Contents
{
    public class Sample
    {
        #region Public Properties

        [JsonIgnore]
        public Bitmap Bitmap { get; set; }

        [JsonIgnore]
        public Mat Centred { get; set; }

        [JsonIgnore]
        public Mat Full { get; set; }

        public int Index { get; set; }

        [JsonIgnore]
        public bool IsMatching { get; set; }

        [JsonIgnore]
        public bool IsRelevant { get; set; }

        [JsonIgnore]
        public double Similarity { get; set; }

        [JsonIgnore]
        public Template Template { get; set; }

        public string Value { get; set; }

        #endregion Public Properties
    }
}