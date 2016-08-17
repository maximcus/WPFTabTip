using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace WPFTabTip
{
    internal static class AnimationHelper
    {
        private static Point GetCurrentUIElementPoint(Visual element) => element.PointToScreen(new Point(0, 0)).ToPointInLogicalUnits(element);

        private static Point RealPixelsPointToLogicalUnits(this Point point, Visual element)
        {
            Matrix t = PresentationSource.FromVisual(element).CompositionTarget.TransformFromDevice;
            return t.Transform(point);
        }

        private static Rectangle ToRectangleInLogicalUnits(this Rectangle rectangleToConvert, DependencyObject element)
        {
            const float logicalUnitDpi = 96.0f;
            // ReSharper disable once AssignNullToNotNullAttribute
            IntPtr windowHandle = new WindowInteropHelper(Window.GetWindow(element)).EnsureHandle();

            using (Graphics graphics = Graphics.FromHwnd(windowHandle))
                return Rectangle.FromLTRB(
                    left: (int) (rectangleToConvert.Left * logicalUnitDpi / graphics.DpiX), 
                    top: (int) (rectangleToConvert.Top * logicalUnitDpi / graphics.DpiY), 
                    right: (int) (rectangleToConvert.Right * logicalUnitDpi / graphics.DpiX), 
                    bottom: (int) (rectangleToConvert.Bottom * logicalUnitDpi / graphics.DpiY));
        }

        private static Point ToPointInLogicalUnits(this Point point, DependencyObject element)
        {
            const float logicalUnitDpi = 96.0f;

            // ReSharper disable once AssignNullToNotNullAttribute
            IntPtr windowHandle = new WindowInteropHelper(Window.GetWindow(element)).EnsureHandle();

            using (Graphics graphics = Graphics.FromHwnd(windowHandle))
                return new Point(x: point.X * logicalUnitDpi / graphics.DpiX, y: point.Y * logicalUnitDpi / graphics.DpiY);
        }

        private static Point GetCurrentUIElementPointRelativeToRoot(UIElement element)
        {
            return element.TransformToAncestor(GetRootVisualForAnimation(element)).Transform(new Point(0, 0));
        }

        private static Rectangle GetUIElementRect(UIElement element)
        {
            Rect rect = element.RenderTransform.TransformBounds(new Rect(GetCurrentUIElementPoint(element), element.RenderSize));

            return Rectangle.FromLTRB(
                left: (int)rect.Left,
                top: (int)rect.Top,
                right: (int)rect.Right,
                bottom: (int)rect.Bottom);
        }

        private static Rectangle GetCurrentScreenBounds(DependencyObject element) => 
            new Screen(Window.GetWindow(element)).Bounds.ToRectangleInLogicalUnits(element);

        private static Rectangle GetWorkAreaWithTabTipOpened(DependencyObject element)
        {
            Rectangle workAreaWithTabTipClosed = GetWorkAreaWithTabTipClosed(element);

            return Rectangle.FromLTRB(
                left: workAreaWithTabTipClosed.Left, 
                top: workAreaWithTabTipClosed.Top, 
                right: workAreaWithTabTipClosed.Right, 
                bottom: TabTip.GetWouldBeTabTipRectangle().ToRectangleInLogicalUnits(element).Top);
        }

        private static Rectangle GetWorkAreaWithTabTipClosed(DependencyObject element)
        {
            Rectangle currentScreenBounds = GetCurrentScreenBounds(element);
            Taskbar taskbar = new Taskbar();
            Rectangle taskbarBounds = taskbar.Bounds.ToRectangleInLogicalUnits(element);

            switch (taskbar.Position)
            {
                case TaskbarPosition.Bottom:
                    return Rectangle.FromLTRB(
                        left: currentScreenBounds.Left,
                        top: currentScreenBounds.Top,
                        right: currentScreenBounds.Right,
                        bottom: taskbarBounds.Top);
                case TaskbarPosition.Top:
                    return Rectangle.FromLTRB(
                        left: currentScreenBounds.Left, 
                        top: taskbarBounds.Bottom, 
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

        private static bool IsUIElementInWorkArea(UIElement element, Rectangle workAreaRectangle)
        {
            return workAreaRectangle.Contains(GetUIElementRect(element));
        }

        private static FrameworkElement GetRootVisualForAnimation(DependencyObject element)
        {
            Window rootWindow = Window.GetWindow(element);

            if (rootWindow?.WindowState != WindowState.Maximized)
                return rootWindow;
            else
                return rootWindow.Content as FrameworkElement;
        }

        private static double GetYOffsetToMoveUIElementInToWorkArea(Rectangle uiElementRectangle, Rectangle workAreaRectangle)
        {
            const double noOffset = 0;
            const int paddingTop = 30;
            const int paddingBottom = 10;

            if (workAreaRectangle.Contains(uiElementRectangle))                                    // UIElement is in work area
                return noOffset;

            if (uiElementRectangle.Top < workAreaRectangle.Top)                                    // Top of UIElement higher than work area
                return workAreaRectangle.Top - uiElementRectangle.Top + paddingTop;                // positive value to move down
            else                                                                                   // Botom of UIElement lower than work area
            {
                int offset = workAreaRectangle.Bottom - uiElementRectangle.Bottom - paddingBottom; // negative value to move up
                if (uiElementRectangle.Top > (workAreaRectangle.Top - offset))                     // will Top of UIElement be in work area if offset applied?
                    return offset;                                                                 // negative value to move up
                else
                    return workAreaRectangle.Top - uiElementRectangle.Top + paddingTop;            // negative value to move up, but only to the point, where top 
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

                DoubleAnimation MoveAnimation = new DoubleAnimation
                {
                    EasingFunction = new CircleEase {EasingMode = EasingMode.EaseOut},
                    Duration = new Duration(TimeSpan.FromSeconds(0.35)),
                    FillBehavior = (VisualRoot is Window) ? FillBehavior.Stop : FillBehavior.HoldEnd
                };

                MoveRootVisualStoryboard.Children.Add(MoveAnimation);

                if (! (VisualRoot is Window))
                    VisualRoot.RenderTransform = new TranslateTransform();

                Storyboard.SetTarget(MoveAnimation, VisualRoot);
                Storyboard.SetTargetProperty(
                    element: MoveAnimation, 
                    path: (VisualRoot is Window) ? new PropertyPath("Top") : new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                VisualRoot.Resources.Add(StoryboardName, MoveRootVisualStoryboard);

                return MoveRootVisualStoryboard;
            }
        }

        private static void MoveRootVisualBy(FrameworkElement rootVisual, double moveBy)
        {
            if (moveBy == 0)
                return;

            Storyboard MoveRootVisualStoryboard = GetMoveRootVisualStoryboard(rootVisual);

            DoubleAnimation doubleAnimation = MoveRootVisualStoryboard.Children.First() as DoubleAnimation;

            if (doubleAnimation != null)
                if (rootVisual is Window)
                {
                    doubleAnimation.From = ((Window) rootVisual).Top;
                    doubleAnimation.To = ((Window) rootVisual).Top + moveBy;
                }
                else
                {
                    doubleAnimation.To = doubleAnimation.To ?? 0;
                    doubleAnimation.To = (doubleAnimation.To ?? 0) + moveBy;
                }

            MoveRootVisualStoryboard.Begin();
        }

        internal static void GetUIElementInToWorkAreaWithTabTipOpened(UIElement element)
        {
            try
            {
                FrameworkElement rootVisualForAnimation = GetRootVisualForAnimation(element);
                Rectangle workAreaWithTabTipOpened = GetWorkAreaWithTabTipOpened(element);

                Rectangle uiElementRectangle;
                Window window = rootVisualForAnimation as Window;
                if (window != null && workAreaWithTabTipOpened.Height >= window.Height)
                    uiElementRectangle = Rectangle.FromLTRB((int)window.Left, (int)window.Top, (int)(window.Left + window.Width), (int)(window.Top + window.Height));
                else
                    uiElementRectangle = GetUIElementRect(element);

                MoveRootVisualBy(
                    rootVisual: rootVisualForAnimation,
                    moveBy: GetYOffsetToMoveUIElementInToWorkArea(
                        uiElementRectangle: uiElementRectangle,
                        workAreaRectangle: workAreaWithTabTipOpened));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}
