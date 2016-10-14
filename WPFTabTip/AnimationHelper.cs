using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Point = System.Windows.Point;

namespace WPFTabTip
{
    internal static class AnimationHelper
    {
        private static readonly Dictionary<FrameworkElement, Storyboard> MoveRootVisualStoryboards = new Dictionary<FrameworkElement, Storyboard>();

        private static Point GetCurrentUIElementPoint(Visual element) => element.PointToScreen(new Point(0, 0)).ToPointInLogicalUnits(element);

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

        // ReSharper disable once UnusedMember.Local
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

            int TabTipRectangleTop = TabTip.GetWouldBeTabTipRectangle().ToRectangleInLogicalUnits(element).Top;

            int bottom = (TabTipRectangleTop == 0) ? workAreaWithTabTipClosed.Bottom / 2 : TabTipRectangleTop; // in case TabTip is not yet opened

            return Rectangle.FromLTRB(
                left: workAreaWithTabTipClosed.Left,
                top: workAreaWithTabTipClosed.Top,
                right: workAreaWithTabTipClosed.Right,
                bottom: bottom);
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

        // ReSharper disable once UnusedMember.Local
        private static bool IsUIElementInWorkAreaWithTabTipOpened(UIElement element)
        {
            return GetWorkAreaWithTabTipOpened(element).Contains(GetUIElementRect(element));
        }

        // ReSharper disable once UnusedMember.Local
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

        private static Storyboard GetOrCreateMoveRootVisualStoryboard(FrameworkElement VisualRoot)
        {
            if (MoveRootVisualStoryboards.ContainsKey(VisualRoot))
                return MoveRootVisualStoryboards[VisualRoot];
            else
                return CreateMoveRootVisualStoryboard(VisualRoot);
        }

        private static Storyboard CreateMoveRootVisualStoryboard(FrameworkElement VisualRoot)
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

            if (!(VisualRoot is Window))
                VisualRoot.RenderTransform = new TranslateTransform();

            Storyboard.SetTarget(MoveAnimation, VisualRoot);
            Storyboard.SetTargetProperty(
                element: MoveAnimation,
                path: (VisualRoot is Window) ? new PropertyPath("Top") : new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));

            MoveRootVisualStoryboards.Add(VisualRoot, MoveRootVisualStoryboard);
            SubscribeToWindowStateChangedToMoveRootVisual(VisualRoot);

            return MoveRootVisualStoryboard;
        }

        private static void SubscribeToWindowStateChangedToMoveRootVisual(FrameworkElement VisualRoot)
        {
            if (VisualRoot is Window)
            {
                Window window = (Window) VisualRoot;

                window.StateChanged += (sender, args) =>
                {
                    if (window.WindowState == WindowState.Normal)
                        MoveRootVisualBy(
                            rootVisual: window,
                            moveBy: GetYOffsetToMoveUIElementInToWorkArea(
                                uiElementRectangle: GetWindowRectangle(window),
                                workAreaRectangle: GetWorkAreaWithTabTipClosed(window)));
                };
            }
            else
            {
                Window window = Window.GetWindow(VisualRoot);
                if (window != null)
                    window.StateChanged += (sender, args) =>
                    {
                        if (window.WindowState == WindowState.Normal)
                            MoveRootVisualTo(VisualRoot, 0);
                    };
            }
        }

        private static void MoveRootVisualBy(FrameworkElement rootVisual, double moveBy)
        {
            if (moveBy == 0)
                return;

            Storyboard MoveRootVisualStoryboard = GetOrCreateMoveRootVisualStoryboard(rootVisual);

            DoubleAnimation doubleAnimation = MoveRootVisualStoryboard.Children.First() as DoubleAnimation;

            if (doubleAnimation != null)
                if (rootVisual is Window)
                {
                    doubleAnimation.From = ((Window) rootVisual).Top;
                    doubleAnimation.To = ((Window) rootVisual).Top + moveBy;
                }
                else
                {
                    doubleAnimation.From = doubleAnimation.To ?? 0;
                    doubleAnimation.To = (doubleAnimation.To ?? 0) + moveBy;
                }

            MoveRootVisualStoryboard.Begin();
        }

        private static void MoveRootVisualTo(FrameworkElement rootVisual, double moveTo)
        {
            Storyboard MoveRootVisualStoryboard = GetOrCreateMoveRootVisualStoryboard(rootVisual);

            DoubleAnimation doubleAnimation = MoveRootVisualStoryboard.Children.First() as DoubleAnimation;

            if (doubleAnimation != null)
                if (rootVisual is Window)
                {
                    doubleAnimation.From = ((Window)rootVisual).Top;
                    doubleAnimation.To = moveTo;
                }
                else
                {
                    doubleAnimation.From = doubleAnimation.To ?? 0;
                    doubleAnimation.To = moveTo;
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
                    uiElementRectangle = GetWindowRectangle(window);
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

        private static Rectangle GetWindowRectangle(Window window)
        {
            return Rectangle.FromLTRB(
                left: (int)window.Left, 
                top: (int)window.Top, 
                right: (int)(window.Left + window.Width), 
                bottom: (int)(window.Top + window.Height));
        }

        internal static void GetEverythingInToWorkAreaWithTabTipClosed()
        {
            foreach (KeyValuePair<FrameworkElement, Storyboard> moveRootVisualStoryboard in MoveRootVisualStoryboards)
            {
                Window window = moveRootVisualStoryboard.Key as Window;
                // if window exist also check if it has not been closed
                if (window != null && new WindowInteropHelper(window).Handle != IntPtr.Zero)
                {
                    MoveRootVisualBy(window, GetYOffsetToMoveUIElementInToWorkArea(GetWindowRectangle(window), GetWorkAreaWithTabTipClosed(window)));
                }
                else
                    MoveRootVisualTo(moveRootVisualStoryboard.Key, 0);
            }
        } 

    }
}
