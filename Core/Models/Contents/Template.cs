using Score2Stream.Core.Interfaces;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Score2Stream.Core.Models.Contents
{
    public class Template
        : Nameable
    {
        #region Public Properties

        public string Empty { get; set; }

        public List<Sample> Samples { get; set; } = new List<Sample>();

        [JsonIgnore]
        public ISampleService SampleService { get; set; }

        #endregion Public Properties
    }
}