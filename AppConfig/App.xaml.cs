using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Windows;

namespace Yafes
{
    /// <summary>
    /// App.xaml için etkileşim mantığı
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Doğrudan StartupScreen'i göster
            var startupScreen = new StartupScreen();
            startupScreen.Show();
        }
    }
}