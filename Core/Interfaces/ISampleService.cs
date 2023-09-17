using Score2Stream.Core.Models.Contents;
using System.Collections.Generic;

namespace Score2Stream.Core.Interfaces
{
    public interface ISampleService
    {
        #region Public Properties

        bool IsDetection { get; set; }

        Sample Active { get; }

        List<Sample> Samples { get; }

        #endregion Public Properties

        #region Public Methods

        void Add(Clip clip);

        void Add(Sample sample);

        void Next(bool onward);

        void Order();

        void Remove(Template template);

        void Remove();

        void Select(Sample sample);

        #endregion Public Methods
    }
}