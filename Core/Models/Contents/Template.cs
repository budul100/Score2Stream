using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Score2Stream.Core.Models.Contents
{
    public class Template
    {
        #region Public Properties

        [JsonIgnore]
        public Clip Clip { get; set; }

        public string ClipDescription { get; set; }

        [JsonIgnore]
        public string Description => Clip?.Description;

        public List<Sample> Samples { get; set; }

        public string ValueEmpty { get; set; }

        #endregion Public Properties
    }
}