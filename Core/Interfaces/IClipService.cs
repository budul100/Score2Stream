﻿using Core.Models;
using System.Collections.Generic;

namespace Core.Interfaces
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