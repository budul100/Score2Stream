using System.Collections.Generic;

namespace ScoreboardOCR.Core.Models
{
    public class Template
    {
        #region Public Properties

        public Clip Clip { get; set; }

        public string Name => Clip?.Name;

        public List<Sample> Samples { get; } = new List<Sample>();

        #endregion Public Properties
    }
}