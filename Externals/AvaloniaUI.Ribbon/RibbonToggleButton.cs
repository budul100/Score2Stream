using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using System;

namespace AvaloniaUI.Ribbon
{
    public class RibbonToggleButton : ToggleButton, IRibbonControl, ICanAddToQuickAccess
    {
        #region Public Fields

        public static readonly StyledProperty<bool> CanAddToQuickAccessProperty = RibbonButton.CanAddToQuickAccessProperty.AddOwner<RibbonToggleButton>();
        public static readonly StyledProperty<IControlTemplate> IconProperty = RibbonButton.IconProperty.AddOwner<RibbonToggleButton>();
        public static readonly StyledProperty<IControlTemplate> LargeIconProperty = RibbonButton.LargeIconProperty.AddOwner<RibbonToggleButton>();
        public static readonly AvaloniaProperty<RibbonControlSize> MaxSizeProperty;
        public static readonly AvaloniaProperty<RibbonControlSize> MinSizeProperty;
        public static readonly StyledProperty<IControlTemplate> QuickAccessIconProperty = RibbonButton.QuickAccessIconProperty.AddOwner<RibbonToggleButton>();
        public static readonly StyledProperty<IControlTemplate> QuickAccessTemplateProperty = AvaloniaProperty.Register<RibbonButton, IControlTemplate>(nameof(Template));
        public static readonly AvaloniaProperty<RibbonControlSize> SizeProperty;

        #endregion Public Fields

        #region Public Constructors

        static RibbonToggleButton()
        {
            RibbonControlHelper<RibbonToggleButton>.SetProperties(out SizeProperty, out MinSizeProperty, out MaxSizeProperty);
            ToggleButton.FocusableProperty.OverrideDefaultValue<RibbonToggleButton>(false);
        }

        #endregion Public Constructors

        #region Public Properties

        public bool CanAddToQuickAccess
        {
            get => GetValue(CanAddToQuickAccessProperty);
            set => SetValue(CanAddToQuickAccessProperty, value);
        }

        public IControlTemplate Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
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

        #region Protected Properties

        protected override Type StyleKeyOverride => typeof(RibbonToggleButton);

        #endregion Protected Properties
    }
}