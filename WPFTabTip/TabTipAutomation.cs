using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;

namespace WPFTabTip
{
    public static class TabTipAutomation
    {
        static TabTipAutomation()
        {
            if (EnvironmentEx.GetOSVersion() == OSVersion.Win7)
                return;

            TabTip.Closed += () => TabTipClosedSubject.OnNext(true);

            AutomateTabTipOpen(FocusSubject.AsObservable());
            AutomateTabTipClose(FocusSubject.AsObservable(), TabTipClosedSubject);
        }

        private static readonly Subject<Tuple<UIElement, bool>> FocusSubject = new Subject<Tuple<UIElement, bool>>();
        private static readonly Subject<bool> TabTipClosedSubject = new Subject<bool>();

        private static readonly List<Type> BindedUIElements = new List<Type>();

        public enum DockModeOptions
        {
            /// <summary>
            /// Dock the keyboard to the taskbar.
            /// </summary>
            Docked,

            /// <summary>
            /// Undock the keyboard (floating window).
            /// </summary>
            Undocked,

            /// <summary>
            /// Do not change the docking behaviour.
            /// </summary>
            NoChange
        }

        /// <summary>
        /// By default TabTip automation happens only when no keyboard is connected to device.
        /// Change IgnoreHardwareKeyboard if you want to automate
        /// TabTip even if keyboard is connected.
        /// </summary>
        public static HardwareKeyboardIgnoreOptions IgnoreHardwareKeyboard
        {
            get { return HardwareKeyboard.IgnoreOptions; }
            set { HardwareKeyboard.IgnoreOptions = value; } 
        }

        public static DockModeOptions DockingMode { get; set; } = DockModeOptions.Undocked;

        /// <summary>
        /// Description of keyboards to ignore if there is only one instance of given keyboard.
        /// If you want to ignore some ghost keyboard, add it's description to this list
        /// </summary>
        public static List<string> ListOfHardwareKeyboardsToIgnoreIfSingleInstance => HardwareKeyboard.IgnoreIfSingleInstance;

        private static void AutomateTabTipClose(IObservable<Tuple<UIElement, bool>> FocusObservable, Subject<bool> tabTipClosedSubject)
        {
            FocusObservable
                .ObserveOn(Scheduler.Default)
                .Where(_ => IgnoreHardwareKeyboard == HardwareKeyboardIgnoreOptions.IgnoreAll || !HardwareKeyboard.IsConnectedAsync().Result)
                .Throttle(TimeSpan.FromMilliseconds(100)) // Close only if no other UIElement got focus in 100 ms
                .Where(tuple => tuple.Item2 == false)
                .Do(_ => TabTip.Close())
                .Subscribe(_ => tabTipClosedSubject.OnNext(true));

            tabTipClosedSubject
                .ObserveOnDispatcher()
                .Subscribe(_ => AnimationHelper.GetEverythingInToWorkAreaWithTabTipClosed());
        }

        private static void AutomateTabTipOpen(IObservable<Tuple<UIElement, bool>> FocusObservable)
        {
            FocusObservable
                .ObserveOn(Scheduler.Default)
                .Where(_ => IgnoreHardwareKeyboard == HardwareKeyboardIgnoreOptions.IgnoreAll || !HardwareKeyboard.IsConnectedAsync().Result)
                .Where(tuple => tuple.Item2 == true)
                .Do(_ => TabTip.OpenAndStartPoolingForClosedEvent(DockingMode))
                .ObserveOnDispatcher()
                .Subscribe(tuple => AnimationHelper.GetUIElementInToWorkAreaWithTabTipOpened(tuple.Item1));
        }

        /// <summary>
        /// Automate TabTip for given UIElement.
        /// Keyboard opens and closes on GotFocusEvent and LostFocusEvent respectively.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void BindTo<T>() where T : UIElement
        {
            if (EnvironmentEx.GetOSVersion() == OSVersion.Win7)
                return;

            if (BindedUIElements.Contains(typeof(T)))
                return;

            EventManager.RegisterClassHandler(
                classType: typeof(T), 
                routedEvent: UIElement.GotFocusEvent, 
                handler: new RoutedEventHandler((s, e) => FocusSubject.OnNext(new Tuple<UIElement, bool>((UIElement) s, true))), 
                handledEventsToo: true);
            EventManager.RegisterClassHandler(
                classType: typeof(T), 
                routedEvent: UIElement.LostFocusEvent, 
                handler: new RoutedEventHandler((s, e) => FocusSubject.OnNext(new Tuple<UIElement, bool>((UIElement) s, false))), 
                handledEventsToo: true);

            BindedUIElements.Add(typeof(T));
        }
    }
}
