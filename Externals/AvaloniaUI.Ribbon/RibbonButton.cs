using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using AvaloniaUI.Ribbon.Enums;
using AvaloniaUI.Ribbon.Interfaces;
using System;

namespace AvaloniaUI.Ribbon
{
    public class RibbonButton : Button, IRibbonControl, ICanAddToQuickAccess
    {
        #region Public Fields

        public static readonly StyledProperty<bool> CanAddToQuickAccessProperty = AvaloniaProperty.Register<RibbonButton, bool>(nameof(CanAddToQuickAccess), true);

        public static readonly StyledProperty<IControlTemplate> IconProperty = AvaloniaProperty.Register<RibbonButton, IControlTemplate>(nameof(Icon));

        public static readonly StyledProperty<IControlTemplate> LargeIconProperty = AvaloniaProperty.Register<RibbonButton, IControlTemplate>(nameof(LargeIcon));

        public static readonly AvaloniaProperty<RibbonControlSize> MaxSizeProperty;

        public static readonly AvaloniaProperty<RibbonControlSize> MinSizeProperty;

        public static readonly StyledProperty<IControlTemplate> QuickAccessIconProperty = AvaloniaProperty.Register<RibbonButton, IControlTemplate>(nameof(QuickAccessIcon));

        public static readonly StyledProperty<IControlTemplate> QuickAccessTemplateProperty = AvaloniaProperty.Register<RibbonButton, IControlTemplate>(nameof(Template));

        public static readonly AvaloniaProperty<RibbonControlSize> SizeProperty;

        #endregion Public Fields

        #region Public Constructors

        static RibbonButton()
        {
            RibbonControlHelper<RibbonButton>.SetProperties(out SizeProperty, out MinSizeProperty, out MaxSizeProperty);
            Button.FocusableProperty.OverrideDefaultValue<RibbonButton>(false);
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

        protected override Type StyleKeyOverride => typeof(RibbonButton);

        #endregion Protected Properties
    }
}