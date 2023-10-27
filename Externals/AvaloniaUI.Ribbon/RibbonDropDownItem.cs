using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;
using System.Windows.Input;

namespace AvaloniaUI.Ribbon
{
    public class RibbonDropDownItem : AvaloniaObject
    {
        #region Public Fields

        public static readonly StyledProperty<object> CommandParameterProperty = Button.CommandParameterProperty.AddOwner<RibbonDropDownItem>();

        public static readonly StyledProperty<ICommand> CommandProperty = Button.CommandProperty.AddOwner<RibbonDropDownItem>();

        public static readonly StyledProperty<IControlTemplate> IconProperty = RibbonButton.IconProperty.AddOwner<RibbonDropDownItem>();

        public static readonly StyledProperty<bool> IsCheckedProperty = AvaloniaProperty.Register<RibbonDropDownItem, bool>(nameof(IsChecked));

        public static readonly DirectProperty<RibbonDropDownItem, string> TextProperty = AvaloniaProperty.RegisterDirect<RibbonDropDownItem, string>(
            nameof(Text),
            o => o.Text,
            (o, v) => o.Text = v);

        #endregion Public Fields

        #region Private Fields

        private string _text = string.Empty;

        #endregion Private Fields

        #region Public Properties

        public ICommand Command
        {
            get => GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public object CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        //AvaloniaProperty.Register<RibbonControlItem, IControlTemplate>(nameof(Icon));
        public IControlTemplate Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public bool IsChecked
        {
            get => GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }

        [Content]
        public string Text
        {
            get => _text;
            set => SetAndRaise(TextProperty, ref _text, value);
        }

        #endregion Public Properties
    }
}