using System.Collections.Generic;
using System.Threading.Tasks;
using Score2Stream.Commons.Models.Contents;

namespace Score2Stream.Commons.Interfaces
{
    public interface IClipService
    {
        #region Public Properties

        Clip Active { get; }

        List<Clip> Clips { get; }

        ITemplateService TemplateService { get; }

        bool UndoSizePossible { get; }

        #endregion Public Properties

        #region Public Methods

        void Add(Clip clip);

        void Clear();

        Task ClearAsync();

        void Create();

        void Empty();

        void Next(bool backward);

        void Order();

        Task RemoveAsync();

        void Select(Clip clip);

        void UndoSize();

        #endregion Public Methods
    }
}