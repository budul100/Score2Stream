﻿using ScoreboardOCR.Core.Mvvm;

namespace WebcamModule.ViewModels
{
    public class ClipViewModel
        : ViewModelBase
    {
        #region Private Fields

        private double height;
        private bool isActive;
        private double? left;
        private double? top;
        private double width;

        #endregion Private Fields

        #region Public Properties

        public double? Bottom => Top.HasValue
            ? Top.Value + Height
            : default;

        public bool HasValue => Left.HasValue
            && Top.HasValue;

        public double Height
        {
            get { return height; }
            set { SetProperty(ref height, value); }
        }

        public bool IsActive
        {
            get { return isActive; }
            set { SetProperty(ref isActive, value); }
        }

        public double? Left
        {
            get { return left; }
            set { SetProperty(ref left, value); }
        }

        public double? Right => Left.HasValue
            ? Left.Value + Width
            : default;

        public double? Top
        {
            get { return top; }
            set { SetProperty(ref top, value); }
        }

        public double Width
        {
            get { return width; }
            set { SetProperty(ref width, value); }
        }

        #endregion Public Properties
    }
}