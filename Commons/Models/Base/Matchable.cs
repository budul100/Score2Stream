using System.Text.Json.Serialization;
using Avalonia.Media.Imaging;
using OpenCvSharp;

namespace Score2Stream.Commons.Models.Base
{
    public abstract class Matchable
    {
        #region Public Properties

        [JsonIgnore]
        public Bitmap Bitmap { get; set; }

        [JsonIgnore]
        public float[] Features { get; set; }

        [JsonIgnore]
        public int Hash { get; set; }

        [JsonIgnore]
        public Mat Image { get; set; }

        [JsonIgnore]
        public bool IsEmpty => Normalized == default;

        [JsonIgnore]
        public float[] Normalized { get; set; }

        #endregion Public Properties
    }
}