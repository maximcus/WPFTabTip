using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Threading.Tasks;

namespace WPFTabTip
{
    public enum HardwareKeyboardIgnoreOptions
    {
        /// <summary>
        /// Do not ignore any keyboard.
        /// </summary>
        DoNotIgnore,

        /// <summary>
        /// Ignore keyboard, if there is only one, and it's description 
        /// can be found in ListOfKeyboardsToIgnore.
        /// </summary>
        IgnoreIfSingleInstanceOnList,

        /// <summary>
        /// Ignore keyboard, if there is only one.
        /// </summary>
        IgnoreIfSingleInstance,

        /// <summary>
        /// Ignore all keyboards for which the description 
        /// can be found in ListOfKeyboardsToIgnore
        /// </summary>
        IgnoreIfOnList,

        /// <summary>
        /// Ignore all keyboards
        /// </summary>
        IgnoreAll
    }

    internal static class HardwareKeyboard
    {
        private static bool? _isConnected;

        /// <summary>
        /// Checks if Hardware Keyboard is Connected
        /// </summary>
        /// <returns></returns>
        internal static async Task<bool> IsConnectedAsync()
        {
            Task<bool> KeyboardConnectedCheckTask = Task.Run(() =>
            {
                SelectQuery SelectKeyboardsQuery = new SelectQuery("Win32_Keyboard");
                using (ManagementObjectSearcher Searcher = new ManagementObjectSearcher(SelectKeyboardsQuery))
                using (ManagementObjectCollection Keyboards = Searcher.Get())
                {
                    if (Keyboards.Count == 0)
                        return false;

                    switch (IgnoreOptions)
                    {
                        case HardwareKeyboardIgnoreOptions.IgnoreAll:
                            return false;

                        case HardwareKeyboardIgnoreOptions.DoNotIgnore:
                            return Keyboards.Count > 0;

                        case HardwareKeyboardIgnoreOptions.IgnoreIfSingleInstance:
                            return Keyboards.Count > 1;

                        case HardwareKeyboardIgnoreOptions.IgnoreIfSingleInstanceOnList:
                            return (Keyboards.Count > 1) ||
                                   (Keyboards.Count == 1 &&
                                    !IsIgnoredKeyboard(Keyboards.Cast<ManagementBaseObject>().First()));

                        case HardwareKeyboardIgnoreOptions.IgnoreIfOnList:
                            return Keyboards.Cast<ManagementBaseObject>().Any(k => !IsIgnoredKeyboard(k));

                        default:
                            return true;
                    }
                }
            });

#pragma warning disable 4014
            KeyboardConnectedCheckTask.ContinueWith(t => _isConnected = t.Result);
#pragma warning restore 4014

            if (_isConnected != null)
                return _isConnected.Value;
            else
                return await KeyboardConnectedCheckTask;
        }

        private static bool IsIgnoredKeyboard(ManagementBaseObject keyboard)
        {
            string description = keyboard.Properties.Cast<PropertyData>()
                .Where(k => k.Name == "Description")
                .Select(k => k.Value)
                .First()
                .ToString();

            return ListOfKeyboardsToIgnore.Contains(description);
        }

        internal static HardwareKeyboardIgnoreOptions IgnoreOptions = HardwareKeyboardIgnoreOptions.DoNotIgnore;

        /// <summary>
        /// Description of keyboards to ignore if there is only one instance of given keyboard.
        /// If you want to ignore some ghost keyboard, add it's description to this list
        /// </summary>
        internal static List<string> ListOfKeyboardsToIgnore { get; } = new List<string>();
    }
}
