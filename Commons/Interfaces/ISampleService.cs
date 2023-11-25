using Score2Stream.Commons.Models.Contents;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Score2Stream.Commons.Interfaces
{
    public interface ISampleService
    {
        #region Public Properties

        Sample Active { get; }

        bool IsDetection { get; set; }

        bool NoRecognition { get; set; }

        List<Sample> Samples { get; }

        int ThresholdDetecting { get; set; }

        #endregion Public Properties

        #region Public Methods

        void Add(Sample sample);

        void Clear();

        Task ClearAsync();

        void Create(Clip clip);

        void Initialize(Template template);

        void Next(bool backward);

        void Order();

        Task RemoveAsync();

        void Select(Sample sample);

        void Update(Clip clip);

        #endregion Public Methods
    }
}