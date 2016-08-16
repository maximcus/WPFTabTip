using System;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Point = System.Windows.Point;

namespace WPFTabTip
{
    public static class AnimationHelper
    {
        private static readonly Taskbar Taskbar = new Taskbar();

        private static Point GetCurrentUIElementPoint(Visual control) => control.PointToScreen(new Point(0, 0));

        private static Point GetCurrentUIElementPointRelativeToRoot(UIElement control)
        {
            return control.TransformToAncestor(GetRootVisualForAnimation(control)).Transform(new Point(0, 0));
        }

        private static Rectangle GetUIElementRect(UIElement element)
        {
            Rect rect = element.RenderTransform.TransformBounds(new Rect(GetCurrentUIElementPoint(element), element.RenderSize));

            return Rectangle.FromLTRB(
                left: (int) rect.Left, 
                top: (int) rect.Top, 
                right: (int) rect.Right, 
                bottom: (int) rect.Bottom);
        }

        private static Rectangle GetCurrentScreenBounds(DependencyObject control) => new Screen(Window.GetWindow(control)).Bounds;

        private static Rectangle GetWorkAreaWithTabTipOpened(DependencyObject control)
        {
            Rectangle workAreaWithTabTipClosed = GetWorkAreaWithTabTipClosed(control);

            return Rectangle.FromLTRB(
                left: workAreaWithTabTipClosed.Left, 
                top: workAreaWithTabTipClosed.Top, 
                right: workAreaWithTabTipClosed.Right, 
                bottom: TabTip.GetWouldBeTabTipRectangle().Top);
        }

        private static Rectangle GetWorkAreaWithTabTipClosed(DependencyObject control)
        {
            Rectangle currentScreenBounds = GetCurrentScreenBounds(control);

            switch (Taskbar.Position)
            {
                case TaskbarPosition.Bottom:
                    return Rectangle.FromLTRB(
                        left: currentScreenBounds.Left,
                        top: currentScreenBounds.Top,
                        right: currentScreenBounds.Right,
                        bottom: Taskbar.Bounds.Top);
                case TaskbarPosition.Top:
                    return Rectangle.FromLTRB(
                        left: currentScreenBounds.Left, 
                        top: Taskbar.Bounds.Bottom, 
                        right: currentScreenBounds.Right, 
                        bottom: currentScreenBounds.Bottom);
                default:
                    return currentScreenBounds;
            }
        }

        private static bool IsUIElementInWorkAreaWithTabTipOpened(UIElement element)
        {
            return GetWorkAreaWithTabTipOpened(element).Contains(GetUIElementRect(element));
        }

        private static FrameworkElement GetRootVisualForAnimation(DependencyObject control)
        {
            Window rootWindow = Window.GetWindow(control);

            if (rootWindow?.WindowState != WindowState.Maximized)
                return rootWindow;
            else
                return rootWindow.Content as FrameworkElement;
        }

        public static double GetYOffsetToMoveUIElementInToWorkArea(UIElement element)
        {
            const double noOffset = 0;
            const int padding = 10;

            if (IsUIElementInWorkAreaWithTabTipOpened(element))                                // UIElement is in work area
                return noOffset;

            Rectangle uiElementRect = GetUIElementRect(element);
            Rectangle workAreaWithTabTipOpened = GetWorkAreaWithTabTipOpened(element);

            if (uiElementRect.Top < workAreaWithTabTipOpened.Top)                              // Top of UIElement higher than work area
                return workAreaWithTabTipOpened.Top - uiElementRect.Top + padding;             // positive value to move down
            else                                                                               // Botom of UIElement lower than work area
            {
                int offset = workAreaWithTabTipOpened.Bottom - uiElementRect.Bottom - padding; // negative value to move up
                if (uiElementRect.Top > (workAreaWithTabTipOpened.Top - offset))               // will Top of UIElement be in work area if offset applied?
                    return offset;                                                             // negative value to move up
                else
                    return workAreaWithTabTipOpened.Top - uiElementRect.Top + padding;         // negative value to move up, but only to the point, where top 
                                                                                               // of UIElement is just below top bound of work area
            }
        }

        private static Storyboard GetMoveRootVisualStoryboard(FrameworkElement VisualRoot)
        {
            const string StoryboardName = "MoveRootVisualStoryboard";
            if (VisualRoot.Resources.Contains(StoryboardName))
                return VisualRoot.Resources[StoryboardName] as Storyboard;
            else
            {
                Storyboard MoveRootVisualStoryboard = new Storyboard
                {
                    Duration = new Duration(TimeSpan.FromSeconds(0.35))
                };

                DoubleAnimation TranslateTransformAnimation = new DoubleAnimation
                {
                    EasingFunction = new CircleEase {EasingMode = EasingMode.EaseOut},
                    Duration = new Duration(TimeSpan.FromSeconds(0.35)),
                    FillBehavior = (VisualRoot is Window) ? FillBehavior.Stop : FillBehavior.HoldEnd
                };

                MoveRootVisualStoryboard.Children.Add(TranslateTransformAnimation);

                VisualRoot.RenderTransform = new TranslateTransform();
                Storyboard.SetTarget(TranslateTransformAnimation, VisualRoot);
                Storyboard.SetTargetProperty(
                    element: TranslateTransformAnimation, 
                    path: (VisualRoot is Window) ? new PropertyPath("Top") : new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                VisualRoot.Resources.Add(StoryboardName, MoveRootVisualStoryboard);

                return MoveRootVisualStoryboard;
            }
        }

        public static void MoveRootVisualBy(UIElement element, double MoveBy)
        {
            if (MoveBy == 0)
                return;

            Storyboard MoveRootVisualStoryboard = GetMoveRootVisualStoryboard(GetRootVisualForAnimation(element));

            DoubleAnimation doubleAnimation = MoveRootVisualStoryboard.Children.First() as DoubleAnimation;

            FrameworkElement rootVisualForAnimation = GetRootVisualForAnimation(element);
            
            if (doubleAnimation != null)
                if (rootVisualForAnimation is Window)
                    doubleAnimation.To = (rootVisualForAnimation as Window).Top + MoveBy;
                else
                    doubleAnimation.To = (doubleAnimation.To ?? 0) + MoveBy;

            MoveRootVisualStoryboard.Begin();
        }
    }
}
