using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ApplicationSettingsNS;

namespace MobileConfiguration
{

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    /// 
    public partial class App : Application
    {
        public static ApplicationSettings_Serializer.ApplicationSettings Settings;

        public App() => Settings = ApplicationSettings_Serializer.Load("appsettings.xml");

    }
}
