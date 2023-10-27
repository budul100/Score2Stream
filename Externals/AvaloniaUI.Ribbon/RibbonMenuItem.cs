using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;

namespace AvaloniaUI.Ribbon
{
    [TemplatePart("PART_ContentButton", typeof(Button))]
    public class RibbonMenuItem : HeaderedItemsControl
    {
        #region Public Fields

        public static readonly RoutedEvent<RoutedEventArgs> ClickEvent = RoutedEvent.Register<Button, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

        public static readonly StyledProperty<object> CommandParameterProperty = Button.CommandParameterProperty.AddOwner<RibbonMenuItem>();

        public static readonly StyledProperty<ICommand> CommandProperty = Button.CommandProperty.AddOwner<RibbonMenuItem>();

        public static readonly StyledProperty<bool> HasItemsProperty = AvaloniaProperty.Register<RibbonMenuItem, bool>(nameof(HasItems));

        public static readonly StyledProperty<object> IconProperty = AvaloniaProperty.Register<RibbonMenuItem, object>(nameof(Icon));

        public static readonly StyledProperty<bool> IsSelectedProperty = AvaloniaProperty.Register<RibbonMenuItem, bool>(nameof(IsSelected));

        public static readonly StyledProperty<bool> IsSubmenuOpenProperty = AvaloniaProperty.Register<RibbonMenuItem, bool>(nameof(IsSubmenuOpen));

        #endregion Public Fields

        #region Public Constructors

        static RibbonMenuItem()
        {
            ItemsSourceProperty.Changed.AddClassHandler<RibbonMenuItem>((x, e) => x.ItemsChanged(e));
        }

        #endregion Public Constructors

        #region Public Events

        public event EventHandler<RoutedEventArgs> Click
        {
            add => AddHandler(ClickEvent, value);
            remove => RemoveHandler(ClickEvent, value);
        }

        #endregion Public Events

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

        public bool HasItems
        {
            get => GetValue(HasItemsProperty);
            set => SetValue(HasItemsProperty, value);
        }

        public object Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public bool IsSelected
        {
            get => GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        public bool IsSubmenuOpen
        {
            get => GetValue(IsSubmenuOpenProperty);
            set => SetValue(IsSubmenuOpenProperty, value);
        }

        #endregion Public Properties

        #region Protected Methods

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            e.NameScope.Get<Button>("PART_ContentButton").Click += (_, _) =>
            {
                var f = new RoutedEventArgs(ClickEvent);
                RaiseEvent(f);
            };
        }

        #endregion Protected Methods

        #region Private Methods

        private void ItemsChanged(AvaloniaPropertyChangedEventArgs args)
        {
            HasItems = Items.Any();
            if (args.OldValue is INotifyCollectionChanged oldSource)
                oldSource.CollectionChanged -= ItemsCollectionChanged;
            if (args.NewValue is INotifyCollectionChanged newSource)
            {
                newSource.CollectionChanged += ItemsCollectionChanged;
            }
        }

        private void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            HasItems = Items.Any();
        }

        #endregion Private Methods
    }
}