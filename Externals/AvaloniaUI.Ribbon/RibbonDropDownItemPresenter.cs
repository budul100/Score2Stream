using Avalonia.Controls;
using System;

namespace AvaloniaUI.Ribbon
{
    public class RibbonDropDownItemPresenter
        : Button
    {
        /*public static readonly StyledProperty<IControlTemplate> IconProperty = RibbonControlItem.IconProperty.AddOwner<RibbonControlItemPresenter>();
        public IControlTemplate Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }*/

        #region Protected Properties

        protected override Type StyleKeyOverride => typeof(RibbonDropDownItemPresenter);

        #endregion Protected Properties

        #region Protected Methods

        protected override void OnClick()
        {
            if (this.Parent is RibbonDropDownButton parent)
            {
                parent.IsDropDownOpen = false;
            }

            base.OnClick();
        }

        #endregion Protected Methods
    }
}