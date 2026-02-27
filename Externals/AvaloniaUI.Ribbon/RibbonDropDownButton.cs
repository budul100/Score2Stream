using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using AvaloniaUI.Ribbon.Enums;
using AvaloniaUI.Ribbon.Interfaces;

namespace AvaloniaUI.Ribbon
{
    public class RibbonDropDownButton
        : ItemsControl, IRibbonControl, ICanAddToQuickAccess
    {
        #region Public Fields

        public static readonly StyledProperty<bool> CanAddToQuickAccessProperty = RibbonButton.CanAddToQuickAccessProperty.AddOwner<RibbonDropDownButton>();

        public static readonly StyledProperty<object> CommandParameterProperty = Button.CommandParameterProperty.AddOwner<RibbonDropDownButton>();

        public static readonly StyledProperty<ICommand> CommandProperty = Button.CommandProperty.AddOwner<RibbonDropDownButton>();

        public static readonly StyledProperty<object> ContentProperty = ContentControl.ContentProperty.AddOwner<RibbonDropDownButton>();

        public static readonly StyledProperty<IControlTemplate> IconProperty = RibbonButton.IconProperty.AddOwner<RibbonDropDownButton>();

        public static readonly StyledProperty<bool> IsDropDownOpenProperty = ComboBox.IsDropDownOpenProperty.AddOwner<RibbonDropDownButton>();

        public static readonly StyledProperty<IControlTemplate> LargeIconProperty = RibbonButton.LargeIconProperty.AddOwner<RibbonDropDownButton>();

        public static readonly AvaloniaProperty<RibbonControlSize> MaxSizeProperty;

        public static readonly AvaloniaProperty<RibbonControlSize> MinSizeProperty;

        public static readonly StyledProperty<IControlTemplate> QuickAccessIconProperty = RibbonButton.QuickAccessIconProperty.AddOwner<RibbonToggleButton>();

        public static readonly StyledProperty<IControlTemplate> QuickAccessTemplateProperty = RibbonButton.QuickAccessTemplateProperty.AddOwner<RibbonDropDownButton>();

        public static readonly AvaloniaProperty<RibbonControlSize> SizeProperty;

        #endregion Public Fields

        #region Public Constructors

        static RibbonDropDownButton()
        {
            RibbonControlHelper<RibbonDropDownButton>.SetProperties(out SizeProperty, out MinSizeProperty, out MaxSizeProperty);
        }

        #endregion Public Constructors

        #region Public Properties

        public bool CanAddToQuickAccess
        {
            get => GetValue(CanAddToQuickAccessProperty);
            set => SetValue(CanAddToQuickAccessProperty, value);
        }

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

        public object Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        public IControlTemplate Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public bool IsDropDownOpen
        {
            get => GetValue(IsDropDownOpenProperty);
            set => SetValue(IsDropDownOpenProperty, value);
        }

        public IControlTemplate LargeIcon
        {
            get => GetValue(LargeIconProperty);
            set => SetValue(LargeIconProperty, value);
        }

        public RibbonControlSize MaxSize
        {
            get => (RibbonControlSize)GetValue(MaxSizeProperty);
            set => SetValue(MaxSizeProperty, value);
        }

        public RibbonControlSize MinSize
        {
            get => (RibbonControlSize)GetValue(MinSizeProperty);
            set => SetValue(MinSizeProperty, value);
        }

        public IControlTemplate QuickAccessIcon
        {
            get => GetValue(QuickAccessIconProperty);
            set => SetValue(QuickAccessIconProperty, value);
        }

        public IControlTemplate QuickAccessTemplate
        {
            get => GetValue(QuickAccessTemplateProperty);
            set => SetValue(QuickAccessTemplateProperty, value);
        }

        public RibbonControlSize Size
        {
            get => (RibbonControlSize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        #endregion Public Properties

        #region Protected Methods

        protected override Control CreateContainerForItemOverride(object item, int index, object recycleKey)
        {
            return new RibbonDropDownItemPresenter();
        }

        protected override bool NeedsContainerOverride(object item, int index, out object recycleKey)
        {
            return NeedsContainer<RibbonDropDownItemPresenter>(item, out recycleKey);
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            var toggle = e.NameScope.Find<ToggleButton>("toggle");
            if (toggle != null)
            {
                toggle.Click += (s, args) =>
                {
                    if (Command?.CanExecute(CommandParameter) == true)
                        Command.Execute(CommandParameter);
                };
            }
        }

        #endregion Protected Methods
    }
}