using Avalonia;
using Avalonia.Controls.Primitives;
using AvaloniaUI.Ribbon.Interfaces;

namespace AvaloniaUI.Ribbon
{
    public class QuickAccessRecommendation : AvaloniaObject//INotifyPropertyChanged
    {
        #region Public Fields

        public static readonly StyledProperty<bool?> IsCheckedProperty = ToggleButton.IsCheckedProperty.AddOwner<QuickAccessRecommendation>();
        public static readonly StyledProperty<ICanAddToQuickAccess> ItemProperty = QuickAccessItem.ItemProperty.AddOwner<QuickAccessRecommendation>();

        #endregion Public Fields

        #region Public Properties

        public bool? IsChecked
        {
            get => GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }

        public ICanAddToQuickAccess Item
        {
            get => GetValue(ItemProperty);
            set => SetValue(ItemProperty, value);
        }

        #endregion Public Properties

        /*void NotifyPropertyChanged([CallerMemberName]string propertyName = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        public event PropertyChangedEventHandler PropertyChanged;*/
    }
}