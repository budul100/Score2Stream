using System;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

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
            base.OnClick();

            var parent = this.FindLogicalAncestorOfType<RibbonDropDownButton>();

            if (parent != null)
            {
                parent.IsDropDownOpen = false;
            }
        }

        #endregion Protected Methods
    }
}