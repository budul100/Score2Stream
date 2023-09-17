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

        public int Height { get; set; }

        public byte[] Image { get; set; }

        public int Index { get; set; }

        [JsonIgnore]
        public bool IsMatching { get; set; }

        [JsonIgnore]
        public bool IsRelevant { get; set; }

        [JsonIgnore]
        public Mat Mat { get; set; }

        [JsonIgnore]
        public double Similarity { get; set; }

        [JsonIgnore]
        public Template Template { get; set; }

        public string Value { get; set; }

        public int Width { get; set; }

        #endregion Public Properties
    }
}