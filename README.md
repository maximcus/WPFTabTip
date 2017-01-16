# WPFTabTip
Simple TabTip / Virtual Keyboard integration for WPF apps on Win 8.1 and Win 10

## Simple to use

The easiest way to install the WPFTabTip is using the [Package Manager Console](https://docs.NuGet.org/consume/package-manager-console) in Visual Studio:

~~~powershell
PM> Install-Package WPFTabTip
~~~

One line of code in your startup logic, and you good to go!

```c#
TabTipAutomation.BindTo<TextBox>();
```

You can bind TabTip automation logic to any `UIElement`. Virtual Keyboard will open when any such element will get focus, and it will close when element will lose focus. Not only that, but `TabTipAutomation` will move `UIElement` (or `Window`) into  view, so that TabTip will not block focused element.

## Hardware keyboard detection

By default TabTip automation will occur only if no hardware keyboard is detected.

You can change that behavior by setting `TabTipAutomation.IgnoreHardwareKeyboard` to any of the following values:

```c#
public enum HardwareKeyboardIgnoreOptions
    {
        /// <summary>
        /// Do not ignore any keyboard.
        /// </summary>
        DoNotIgnore,

        /// <summary>
        /// Ignore keyboard, if there is only one, and it's description 
        /// can be found in IgnoredDevices.
        /// </summary>
        IgnoreIfSingleInstanceOnList,

        /// <summary>
        /// Ignore keyboard, if there is only one.
        /// </summary>
        IgnoreIfSingleInstance,
        
        /// <summary>
        /// Ignore all keyboards for which the description 
        /// can be found in IgnoredDevices
        /// </summary>
        IgnoreIfInstanceOnList,

        /// <summary>
        /// Ignore all keyboards
        /// </summary>
        IgnoreAll
    }
```

 If you want to ignore specific keyboard you should set `TabTipAutomation.IgnoreHardwareKeyboard` to `IgnoreIfSingleInstanceOnList`, and add keyboard description to `TabTipAutomation.IgnoredDevices`.

To get description of keyboards connected to machine you can use following code:

```c#
new ManagementObjectSearcher(new SelectQuery("Win32_Keyboard")).Get()
                .Cast<ManagementBaseObject>()
                .SelectMany(keyboard =>
                    keyboard.Properties
                        .Cast<PropertyData>()
                        .Where(k => k.Name == "Description")
                        .Select(k => k.Value as string))
                .ToList();
```

## Change keyboard layout

To specify keyboard layout to be used with certain element you can set `InputScope` property in xaml to one of the following:
- Default
- Url
- EmailSmtpAddress
- Number
