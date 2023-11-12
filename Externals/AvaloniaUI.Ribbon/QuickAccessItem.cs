using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using AvaloniaUI.Ribbon.Interfaces;
using System;

namespace AvaloniaUI.Ribbon
{
    public class QuickAccessItem : ContentControl
    {
        #region Public Fields

        public static readonly StyledProperty<ICanAddToQuickAccess> ItemProperty = AvaloniaProperty.Register<QuickAccessItem, ICanAddToQuickAccess>(nameof(Item), null);

        #endregion Public Fields

        #region Public Properties

        public ICanAddToQuickAccess Item
        {
            get => GetValue(ItemProperty);
            set => SetValue(ItemProperty, value);
        }

        #endregion Public Properties

        #region Protected Properties

        protected override Type StyleKeyOverride => typeof(QuickAccessItem);

        #endregion Protected Properties

        #region Protected Methods

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            e.NameScope.Find<MenuItem>("PART_RemoveFromQuickAccessToolbar")!.Click += (_, _) => Avalonia.VisualTree.VisualExtensions.FindAncestorOfType<QuickAccessToolbar>(this)?.RemoveItem(Item);
        }

        #endregion Protected Methods
    }
}