using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace AvaloniaUI.Ribbon
{
    public class GalleryItem : ListBoxItem
    {
        #region Public Fields

        public static readonly StyledProperty<IControlTemplate> IconProperty = RibbonButton.IconProperty.AddOwner<GalleryItem>();

        #endregion Public Fields

        #region Public Properties

        public IControlTemplate Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        #endregion Public Properties
    }
}