using System.Collections.Generic;
using System.Text.Json.Serialization;
using Score2Stream.Commons.Interfaces;

namespace Score2Stream.Commons.Models.Contents
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