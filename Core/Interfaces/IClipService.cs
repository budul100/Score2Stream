﻿using Core.Models;
using System.Collections.Generic;

namespace Core.Interfaces
{
    public interface IClipService
    {
        #region Public Properties

        Clip Active { get; }

        List<Clip> Clips { get; }

        #endregion Public Properties

        #region Public Methods

        void Add();

        bool IsUniqueName(string name);

        void Remove();

        void RemoveAll();

        #endregion Public Methods
    }
}