using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows;

namespace WPFTabTip
{
    public static class TabTipAutomation
    {
        static TabTipAutomation()
        {
            AutomateTabTipOpen(FocusSubject.AsObservable());
            AutomateTabTipClose(FocusSubject.AsObservable());
        }

        private static readonly Subject<bool> FocusSubject = new Subject<bool>(); 

        private static readonly List<Type> BindedUIElements = new List<Type>();

        /// <summary>
        /// TabTip automation happens only when no keyboard is connected to device.
        /// Set IgnoreHardwareKeyboard to true if you want to automate
        /// TabTip even if keyboard is connected.
        /// </summary>
        public static bool IgnoreHardwareKeyboard { get; set; }

        /// <summary>
        /// Description of keyboards to ignore if there is only one instance of given keyboard.
        /// If you want to ignore some ghost keyboard, add it's description to this list
        /// </summary>
        public static List<string> ListOfHardwareKeyboardsToIgnoreIfSingleInstance => HardwareKeyboard.IgnoreIfSingleInstance;

        private static void AutomateTabTipClose(IObservable<bool> FocusObservable)
        {
            FocusObservable
                .ObserveOn(Scheduler.Default)
                .Where(_ => IgnoreHardwareKeyboard || !HardwareKeyboard.IsConnectedAsync().Result)
                .Throttle(TimeSpan.FromMilliseconds(100)) // Close only if no other UIElement got focus in 100 ms
                .Where(gotFocus => gotFocus == false)
                .Subscribe(_ => TabTip.Close());
        }

        private static void AutomateTabTipOpen(IObservable<bool> FocusObservable)
        {
            FocusObservable
                .ObserveOn(Scheduler.Default)
                .Where(_ => IgnoreHardwareKeyboard || !HardwareKeyboard.IsConnectedAsync().Result)
                .Where(gotFocus => gotFocus == true)
                .Subscribe(_ => TabTip.OpenAndStartPoolingForClosedEvent());
        }

        /// <summary>
        /// Automate TabTip for given UIElement.
        /// Keyboard opens and closes on GotFocusEvent and LostFocusEvent respectively.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void BindTo<T>() where T : UIElement
        {
            if (BindedUIElements.Contains(typeof(T)))
                return;

            EventManager.RegisterClassHandler(classType: typeof(T), routedEvent: UIElement.GotFocusEvent, handler: new RoutedEventHandler((s, e) => FocusSubject.OnNext(true)), handledEventsToo: true);
            EventManager.RegisterClassHandler(classType: typeof(T), routedEvent: UIElement.LostFocusEvent, handler: new RoutedEventHandler((s, e) => FocusSubject.OnNext(false)), handledEventsToo: true);

            BindedUIElements.Add(typeof(T));
        }
    }
}
