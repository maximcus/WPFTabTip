using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Windows;
using System.Windows.Controls;
using WPFTabTip;

namespace UITest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            TabTipAutomation.IgnoreHardwareKeyboard = HardwareKeyboardIgnoreOptions.IgnoreAll;
            TabTipAutomation.BindTo<TextBox>();
            TabTipAutomation.BindTo<RichTextBox>();
        }

        private static void GetKeyboardDescriptions()
        {
            string tempFileName = "temp.txt";
            List<string> KeyboardDescriptions = QueryWmiKeyboards();

            File.WriteAllLines(tempFileName, KeyboardDescriptions);

            Process.Start(tempFileName);
        }

        private static List<string> QueryWmiKeyboards()
        {
            using (var searcher = new ManagementObjectSearcher(new SelectQuery("Win32_Keyboard")))
            using (var result = searcher.Get())
            {
                return result
                    .Cast<ManagementBaseObject>()
                    .SelectMany(keyboard =>
                        keyboard.Properties
                            .Cast<PropertyData>()
                            .Where(k => k.Name == "Description")
                            .Select(k => k.Value as string))
                    .ToList();
            }
        }

        private void btn_NewWindow_OnClick(object sender, RoutedEventArgs e) => new DialogWindow().Show();

        private void btn_KeyboardDescriptions_OnClick(object sender, RoutedEventArgs e) => GetKeyboardDescriptions();
    }
}
