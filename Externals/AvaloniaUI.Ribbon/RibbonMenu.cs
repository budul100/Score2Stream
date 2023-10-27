using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Styling;
using Avalonia.Threading;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Timers;

namespace AvaloniaUI.Ribbon
{
    [TemplatePart("MenuPopup", typeof(Popup))]
    public sealed class RibbonMenu : ItemsControl, IRibbonMenu
    {
        #region Public Fields

        public static readonly StyledProperty<object> ContentProperty = ContentControl.ContentProperty.AddOwner<RibbonMenu>();
        public static readonly StyledProperty<bool> HasSelectedItemProperty = AvaloniaProperty.Register<RibbonMenu, bool>(nameof(HasSelectedItem), false);
        public static readonly StyledProperty<bool> IsMenuOpenProperty = AvaloniaProperty.Register<RibbonMenu, bool>(nameof(IsMenuOpen), false);
        public static readonly StyledProperty<string> RightColumnHeaderProperty = AvaloniaProperty.Register<RibbonMenu, string>(nameof(RightColumnHeader));
        public static readonly StyledProperty<ITemplate<Panel>> RightColumnItemsPanelProperty = AvaloniaProperty.Register<RibbonMenu, ITemplate<Panel>>(nameof(RightColumnItemsPanel), DefaultPanel);
        public static readonly DirectProperty<RibbonMenu, IEnumerable> RightColumnItemsProperty = AvaloniaProperty.RegisterDirect<RibbonMenu, IEnumerable>(nameof(RightColumnItems), o => o.RightColumnItems, (o, v) => o.RightColumnItems = v);
        public static readonly StyledProperty<IDataTemplate> RightColumnItemTemplateProperty = AvaloniaProperty.Register<RibbonMenu, IDataTemplate>(nameof(RightColumnItemTemplate));
        public static readonly StyledProperty<object> SelectedSubItemsProperty = AvaloniaProperty.Register<RibbonMenu, object>(nameof(SelectedSubItems));

        #endregion Public Fields

        #region Private Fields

        private static readonly FuncTemplate<Panel> DefaultPanel = new(() => new StackPanel());
        private RibbonMenuItem _previousSelectedItem = null;
        private IEnumerable _rightColumnItems = new AvaloniaList<object>();

        #endregion Private Fields

        #region Public Constructors

        static RibbonMenu()
        {
            IsMenuOpenProperty.Changed.AddClassHandler<RibbonMenu>(new Action<RibbonMenu, AvaloniaPropertyChangedEventArgs>((sender, e) =>
            {
                if (e.NewValue is bool boolean)
                {
                    if (boolean)
                    {
                        //sender.Focus();
                    }
                    else
                    {
                        sender.SelectedSubItems = null;
                        sender.HasSelectedItem = false;

                        if (sender._previousSelectedItem != null)
                            sender._previousSelectedItem.IsSelected = false;
                    }
                }
            }));

            ItemsSourceProperty.Changed.AddClassHandler<RibbonMenu>((x, e) => x.ItemsChanged(e));
        }

        public RibbonMenu()
        {
            /*LostFocus += (_, _) =>
            {
                IsMenuOpen = false;
            };*/
            /*this.FindAncestorOfType<VisualLayerManager>()*/
        }

        #endregion Public Constructors

        #region Private Destructors

        ~RibbonMenu()
        {
            if (ItemsSource is INotifyCollectionChanged collectionChanged)
                collectionChanged.CollectionChanged -= ItemsCollectionChanged;
        }

        #endregion Private Destructors

        #region Public Properties

        public object Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        public bool HasSelectedItem
        {
            get => GetValue(HasSelectedItemProperty);
            set => SetValue(HasSelectedItemProperty, value);
        }

        public bool IsMenuOpen
        {
            get => GetValue(IsMenuOpenProperty);
            set => SetValue(IsMenuOpenProperty, value);
        }

        public string RightColumnHeader
        {
            get => GetValue(RightColumnHeaderProperty);
            set => SetValue(RightColumnHeaderProperty, value);
        }

        public IEnumerable RightColumnItems
        {
            get => _rightColumnItems;
            set => SetAndRaise(RightColumnItemsProperty, ref _rightColumnItems, value);
        }

        public ITemplate<Panel> RightColumnItemsPanel
        {
            get => GetValue(RightColumnItemsPanelProperty);
            set => SetValue(RightColumnItemsPanelProperty, value);
        }

        public IDataTemplate RightColumnItemTemplate
        {
            get => GetValue(RightColumnItemTemplateProperty);
            set => SetValue(RightColumnItemTemplateProperty, value);
        }

        public object SelectedSubItems
        {
            get => GetValue(SelectedSubItemsProperty);
            set => SetValue(SelectedSubItemsProperty, value);
        }

        #endregion Public Properties

        #region Protected Methods

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            var popup = e.NameScope.Find<Popup>("MenuPopup");
            popup.Closed += PopupOnClosed;
        }

        #endregion Protected Methods

        #region Private Methods

        private void Item_PointerEnter(object sender, Avalonia.Input.PointerEventArgs e)
        {
            if (sender is RibbonMenuItem item)
            {
                int counter = 0;
                Timer timer = new Timer(1);
                timer.Elapsed += (sneder, args) =>
                {
                    if (counter < 25)
                    {
                        counter++;
                    }
                    else
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            if (item.IsPointerOver)
                            {
                                if (item.HasItems)
                                {
                                    SelectedSubItems = item.Items;
                                    HasSelectedItem = true;

                                    item.IsSelected = true;

                                    if (_previousSelectedItem != null)
                                        _previousSelectedItem.IsSelected = false;

                                    _previousSelectedItem = item;
                                }
                                else
                                {
                                    SelectedSubItems = null;
                                    HasSelectedItem = false;

                                    if (_previousSelectedItem != null)
                                        _previousSelectedItem.IsSelected = false;
                                }
                            }
                        });

                        timer.Stop();
                    }
                };
                timer.Start();
            }
        }

        private void ItemsChanged(AvaloniaPropertyChangedEventArgs args)
        {
            ResetItemHoverEvents();

            if (args.OldValue is INotifyCollectionChanged oldSource)
                oldSource.CollectionChanged -= ItemsCollectionChanged;
            if (args.NewValue is INotifyCollectionChanged newSource)
            {
                newSource.CollectionChanged += ItemsCollectionChanged;
            }
        }

        private void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ResetItemHoverEvents();
        }

        private void PopupOnClosed(object sender, EventArgs e)
        {
        }

        private void ResetItemHoverEvents()
        {
            foreach (RibbonMenuItem item in Items.OfType<RibbonMenuItem>())
            {
                item.PointerEntered -= Item_PointerEnter;
                item.PointerEntered += Item_PointerEnter;
            }
        }

        #endregion Private Methods
    }
}