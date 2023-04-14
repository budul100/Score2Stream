using Score2Stream.Core.Models;
using System.Collections.Generic;

namespace Score2Stream.Core.Interfaces
{
    public interface IClipService
    {
        #region Public Properties

        Clip Clip { get; }

        List<Clip> Clips { get; }

        ITemplateService TemplateService { get; }

        #endregion Public Properties

        #region Public Methods

        void Add();

        void Clear();

        void Remove();

        void Remove(Clip clip);

        void Select(Clip clip);

        #endregion Public Methods
    }
}