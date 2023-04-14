using System.Collections.Generic;

namespace Score2Stream.Core.Models
{
    public class Template
    {
        #region Public Properties

        public Clip Clip { get; set; }

        public string Description => Clip?.Description;

        public List<Sample> Samples { get; } = new List<Sample>();

        public string ValueEmpty { get; set; }

        #endregion Public Properties
    }
}