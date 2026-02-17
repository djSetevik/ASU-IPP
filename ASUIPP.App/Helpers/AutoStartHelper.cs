using System;
using Microsoft.Win32;

namespace ASUIPP.App.Helpers
{
    public static class AutoStartHelper
    {
        private const string AppName = "ASUIPP";
        private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        public static bool IsEnabled()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RunKey, false))
            {
                return key?.GetValue(AppName) != null;
            }
        }

        public static void Enable()
        {
            var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            using (var key = Registry.CurrentUser.OpenSubKey(RunKey, true))
            {
                key?.SetValue(AppName, $"\"{exePath}\" --tray");
            }
        }

        public static void Disable()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RunKey, true))
            {
                key?.DeleteValue(AppName, false);
            }
        }

        public static void SetEnabled(bool enabled)
        {
            if (enabled) Enable();
            else Disable();
        }
    }
}