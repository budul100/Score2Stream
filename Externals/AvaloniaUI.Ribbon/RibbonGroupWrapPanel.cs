using Avalonia;
using Avalonia.Controls;
using System.Linq;

namespace AvaloniaUI.Ribbon
{
    public class RibbonGroupWrapPanel : WrapPanel
    {
        #region Public Fields

        public static readonly StyledProperty<GroupDisplayMode> DisplayModeProperty = RibbonGroupBox.DisplayModeProperty.AddOwner<RibbonGroupWrapPanel>();

        #endregion Public Fields

        #region Public Constructors

        //AvaloniaProperty.Register<RibbonGroupWrapPanel, GroupDisplayMode>(nameof(DisplayMode), defaultValue: GroupDisplayMode.Large);
        static RibbonGroupWrapPanel()
        {
            AffectsArrange<RibbonGroupWrapPanel>(DisplayModeProperty);
            AffectsMeasure<RibbonGroupWrapPanel>(DisplayModeProperty);
            AffectsRender<RibbonGroupWrapPanel>(DisplayModeProperty);

            DisplayModeProperty.Changed.AddClassHandler<RibbonGroupWrapPanel>((sneder, args) =>
            {
                var children2 = sneder.Children.Where(x => x is IRibbonControl);
                if (((GroupDisplayMode)args.NewValue) == GroupDisplayMode.Large)
                {
                    foreach (IRibbonControl ctrl in children2.Cast<IRibbonControl>())
                    {
                        ctrl.Size = ctrl.MaxSize;
                    }
                }
                else if (((GroupDisplayMode)args.NewValue) == GroupDisplayMode.Small)
                {
                    foreach (IRibbonControl ctrl in children2.Cast<IRibbonControl>())
                    {
                        ctrl.Size = ctrl.MinSize;
                    }
                }
            });
        }

        public RibbonGroupWrapPanel()
        {
            if (TemplatedParent is RibbonGroupBox parentBox)
            {
                parentBox.Rearranged += (_, _) => ArrangeOverride(Bounds.Size);
                parentBox.Remeasured += (_, _) => MeasureOverride(Bounds.Size);
            }
        }

        #endregion Public Constructors

        #region Public Properties

        public GroupDisplayMode DisplayMode
        {
            get => GetValue(DisplayModeProperty);
            set => SetValue(DisplayModeProperty, value);
        }

        #endregion Public Properties
    }
}