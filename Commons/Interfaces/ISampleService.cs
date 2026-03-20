using System.Collections.Generic;
using System.Threading.Tasks;
using Score2Stream.Commons.Models.Contents;

namespace Score2Stream.Commons.Interfaces
{
    public interface ISampleService
    {
        #region Public Properties

        Sample Active { get; }

        bool IsDetection { get; set; }

        IReadOnlyList<Sample> Samples { get; }

        #endregion Public Properties

        #region Public Methods

        void Add(Sample sample);

        void Clear();

        Task ClearAsync();

        void Create(Segment clip);

        void Initialize(Template template);

        void Next(bool backward);

        void Order(bool reverseOrder = false);

        Task RemoveAsync();

        void Select(Sample sample);

        #endregion Public Methods
    }
}