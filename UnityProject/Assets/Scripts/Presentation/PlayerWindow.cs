using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Ember.Core;

namespace Ember.Presentation
{
    // Change only the game's visible caption; product ID and existing save path stay stable.
    public static class PlayerWindow
    {
        #if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern bool SetWindowText(IntPtr handle,string text);
        #endif
        public static void LocalizeTitle()
        {
            #if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            using(var process=Process.GetCurrentProcess())
            {
                var handle=process.MainWindowHandle;if(handle!=IntPtr.Zero)SetWindowText(handle,Loc.T("ui.window_title"));
            }
            #endif
        }
    }
}
