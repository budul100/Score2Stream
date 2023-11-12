using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaUI.Ribbon.Extensions;
using AvaloniaUI.Ribbon.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace AvaloniaUI.Ribbon
{
    public class RibbonTab : TabItem, IKeyTipHandler
    {
        #region Public Fields

        public static readonly DirectProperty<RibbonTab, ObservableCollection<RibbonGroupBox>> GroupsProperty = AvaloniaProperty.RegisterDirect<RibbonTab, ObservableCollection<RibbonGroupBox>>(nameof(Groups), o => o.Groups, (o, v) => o.Groups = v);

        public static readonly StyledProperty<bool> IsContextualProperty = AvaloniaProperty.Register<RibbonTab, bool>(nameof(IsContextual), false);

        #endregion Public Fields

        #region Private Fields

        private ObservableCollection<RibbonGroupBox> _groups = new();

        //private IKeyTipHandler _prev;
        private Ribbon _ribbon;

        #endregion Private Fields

        #region Public Constructors

        static RibbonTab()
        {
            KeyTip.ShowChildKeyTipKeysProperty.Changed.AddClassHandler<RibbonTab>(new Action<RibbonTab, AvaloniaPropertyChangedEventArgs>((sender, args) =>
            {
                if ((bool)args.NewValue)
                {
                    foreach (RibbonGroupBox g in sender.Groups)
                    {
                        if ((g.Command != null) && KeyTip.HasKeyTipKeys(g))
                            KeyTip.GetKeyTip(g).IsOpen = true;

                        foreach (Control c in g.Items.Cast<Control>())
                        {
                            if (KeyTip.HasKeyTipKeys(c))
                                KeyTip.GetKeyTip(c).IsOpen = true;
                        }
                    }
                }
                else
                {
                    foreach (RibbonGroupBox g in sender.Groups)
                    {
                        KeyTip.GetKeyTip(g).IsOpen = false;

                        foreach (Control c in g.Items.Cast<Control>())
                            KeyTip.GetKeyTip(c).IsOpen = false;
                    }
                }
            }));
        }

        public RibbonTab()
        {
            LostFocus += (sneder, args) => KeyTip.SetShowChildKeyTipKeys(this, false);
        }

        #endregion Public Constructors

        #region Public Properties

        public ObservableCollection<RibbonGroupBox> Groups
        {
            get { return _groups; }
            set { SetAndRaise(GroupsProperty, ref _groups, value); }
        }

        public bool IsContextual
        {
            get => GetValue(IsContextualProperty);
            set => SetValue(IsContextualProperty, value);
        }

        #endregion Public Properties

        #region Protected Properties

        protected override Type StyleKeyOverride => typeof(RibbonTab);

        #endregion Protected Properties

        /*[Content]*/

        #region Public Methods

        public void ActivateKeyTips(Ribbon ribbon, IKeyTipHandler prev)
        {
            _ribbon = ribbon;
            // _prev = prev;
            foreach (RibbonGroupBox g in Groups)
                Debug.WriteLine("GROUP KEYS: " + KeyTip.GetKeyTipKeys(g));

            Focus();
            KeyTip.SetShowChildKeyTipKeys(this, true);
            KeyDown += RibbonTab_KeyDown;
        }

        public bool HandleKeyTipKeyPress(Key key)
        {
            bool retVal = false;
            foreach (RibbonGroupBox g in Groups)
            {
                if (KeyTip.HasKeyTipKey(g, key))
                {
                    g.Command?.Execute(g.CommandParameter);
                    (Parent as Ribbon).Close();
                    retVal = true;
                    break;
                }
                else
                {
                    foreach (Control c in g.Items.Cast<Control>())
                    {
                        if (KeyTip.HasKeyTipKey(c, key))
                        {
                            if (c is IKeyTipHandler hdlr)
                            {
                                hdlr.ActivateKeyTips(_ribbon, this);
                                Debug.WriteLine("Group handled " + key.ToString() + " for IKeyTipHandler");
                            }
                            else
                            {
                                if ((c is Button btn) && (btn.Command != null))
                                    btn.Command.Execute(btn.CommandParameter);
                                else
                                    c.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                                _ribbon.Close();
                                retVal = true;
                            }
                            break;
                        }
                    }
                    if (retVal)
                        break;
                }
            }
            return retVal;
        }

        #endregion Public Methods

        #region Protected Methods

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if ((e.Root is IInputRoot inputRoot) && (inputRoot is WindowBase wnd))
                wnd.Deactivated += InputRoot_Deactivated;
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            if ((e.Root is IInputRoot inputRoot) && (inputRoot is WindowBase wnd))
                wnd.Deactivated -= InputRoot_Deactivated;
        }

        #endregion Protected Methods

        #region Private Methods

        private void InputRoot_Deactivated(object sender, EventArgs e)
        {
            KeyTip.SetShowChildKeyTipKeys(this, false);
            RibbonControlExtensions.GetParentRibbon(this)?.Close();
        }

        private void RibbonTab_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = HandleKeyTipKeyPress(e.Key);
            if (e.Handled)
                _ribbon.IsCollapsedPopupOpen = false;

            KeyTip.SetShowChildKeyTipKeys(this, false);
            KeyDown -= RibbonTab_KeyDown;
        }

        #endregion Private Methods
    }
}