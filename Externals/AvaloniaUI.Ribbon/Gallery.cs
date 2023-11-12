﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using AvaloniaUI.Ribbon.Enums;
using AvaloniaUI.Ribbon.Interfaces;
using System;

namespace AvaloniaUI.Ribbon
{
    [TemplatePart("PART_ItemsPresenter", typeof(ItemsPresenter))]
    [TemplatePart("PART_ItemsPresenterHolder", typeof(ContentControl))]
    [TemplatePart("PART_UpButton", typeof(RepeatButton))]
    [TemplatePart("PART_DownButton", typeof(RepeatButton))]
    [TemplatePart("PART_ScrollContentPresenter", typeof(GalleryScrollContentPresenter))]
    [TemplatePart("PART_FlyoutItemsPresenterHolder", typeof(ContentControl))]
    [TemplatePart("PART_FlyoutRoot", typeof(Control))]
    public class Gallery : ListBox, IRibbonControl
    {
        #region Public Fields

        public static readonly StyledProperty<bool> IsDropDownOpenProperty;

        public static readonly StyledProperty<double> ItemHeightProperty = AvaloniaProperty.Register<Gallery, double>(nameof(ItemHeight));

        public static readonly AvaloniaProperty<RibbonControlSize> MaxSizeProperty;

        public static readonly AvaloniaProperty<RibbonControlSize> MinSizeProperty;

        public static readonly AvaloniaProperty<RibbonControlSize> SizeProperty;

        #endregion Public Fields

        #region Private Fields

        private ContentControl _flyoutPresenter;

        private ItemsPresenter _itemsPresenter;

        private ContentControl _mainPresenter;

        #endregion Private Fields

        #region Public Constructors

        static Gallery()
        {
            //IsDropDownOpenProperty = ComboBox.IsDropDownOpenProperty.AddOwner<Gallery>(element => element.IsDropDownOpen, (element, value) => element.IsDropDownOpen = value);
            IsDropDownOpenProperty = ComboBox.IsDropDownOpenProperty.AddOwner<Gallery>();
            IsDropDownOpenProperty.Changed.AddClassHandler<Gallery, bool>(new Action<Gallery, AvaloniaPropertyChangedEventArgs>((sneder, args) =>
            {
                if (args.NewValue is bool value)
                    sneder.UpdatePresenterLocation(value);
            }));
            RibbonControlHelper<Gallery>.SetProperties(out SizeProperty, out MinSizeProperty, out MaxSizeProperty);
        }

        #endregion Public Constructors

        #region Public Properties

        public bool IsDropDownOpen
        {
            get => GetValue(IsDropDownOpenProperty);
            set => SetValue(IsDropDownOpenProperty, value);
        }

        public double ItemHeight
        {
            get => GetValue(ItemHeightProperty);
            set => SetValue(ItemHeightProperty, value);
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

        public RibbonControlSize Size
        {
            get => (RibbonControlSize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        #endregion Public Properties

        #region Protected Properties

        protected override Type StyleKeyOverride => typeof(Gallery);

        #endregion Protected Properties

        #region Protected Methods

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            _itemsPresenter = e.NameScope.Find<ItemsPresenter>("PART_ItemsPresenter");
            _mainPresenter = e.NameScope.Find<ContentControl>("PART_ItemsPresenterHolder");

            GalleryScrollContentPresenter pres = e.NameScope.Find<GalleryScrollContentPresenter>("PART_ScrollContentPresenter");
            e.NameScope.Find<RepeatButton>("PART_UpButton").Click += (sneder, args) => pres.Offset = pres.Offset.WithY(Math.Max(0, pres.Offset.Y - ItemHeight));
            e.NameScope.Find<RepeatButton>("PART_DownButton").Click += (sneder, args) => pres.Offset = pres.Offset.WithY(Math.Min(pres.Offset.Y + ItemHeight, _mainPresenter.Bounds.Height - pres.Bounds.Height));

            _flyoutPresenter = e.NameScope.Find<ContentControl>("PART_FlyoutItemsPresenterHolder");
            /*_flyoutPresenter.PointerWheelChanged += (s, a) =>
            {
                a.Handled = true;
            };*/
            e.NameScope.Find<Control>("PART_FlyoutRoot").PointerExited += (sneder, a) => IsDropDownOpen = false;

            UpdatePresenterLocation(IsDropDownOpen);
        }

        #endregion Protected Methods

        #region Private Methods

        private void UpdatePresenterLocation(bool intoFlyout)
        {
            if (_itemsPresenter.Parent is ContentPresenter presenter)
                presenter.Content = null;
            else if (_itemsPresenter.Parent is ContentControl control)
                control.Content = null;
            else if (_itemsPresenter.Parent is Panel panel)
                panel.Children.Remove(_itemsPresenter);

            if (intoFlyout)
                _flyoutPresenter.Content = _itemsPresenter;
            else
                _mainPresenter.Content = _itemsPresenter;
        }

        #endregion Private Methods
    }
}