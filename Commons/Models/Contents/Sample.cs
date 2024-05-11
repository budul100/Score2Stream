using System.Text.Json.Serialization;
using Avalonia.Media.Imaging;
using OpenCvSharp;
using Score2Stream.Commons.Enums;

namespace Score2Stream.Commons.Models.Contents
{
    public class Sample
    {
        #region Public Properties

        [JsonIgnore]
        public Bitmap Bitmap { get; set; }

        public double Height { get; set; }

        public byte[] Image { get; set; }

        [JsonIgnore]
        public int Index { get; set; }

        [JsonIgnore]
        public bool IsFiltered { get; set; }

        public bool IsVerified { get; set; }

        [JsonIgnore]
        public Mat Mat { get; set; }

        [JsonIgnore]
        public Template Template { get; set; }

        public string Value { get; set; }

        public double Width { get; set; }

        #endregion Public Properties
    }
}