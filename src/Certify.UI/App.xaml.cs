﻿using MahApps.Metro;
using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Certify.UI
{
    /// <summary>
    /// Interaction logic for App.xaml 
    /// </summary>
    public partial class App : Application
    {
        private TelemetryClient tc = null;

        protected Certify.UI.ViewModel.AppModel MainViewModel
        {
            get
            {
                return UI.ViewModel.AppModel.AppViewModel;
            }
        }

        protected async override void OnStartup(StartupEventArgs e)
        {
            /*
            // get the current app style (theme and accent) from the application you can then use the
            // current theme and custom accent instead set a new theme
            Tuple<AppTheme, Accent> appStyle = ThemeManager.DetectAppStyle(Application.Current);

            // now set the Green accent and dark theme
            ThemeManager.ChangeAppStyle(Application.Current,
                                        ThemeManager.GetAccent("Green"),
                                        ThemeManager.GetAppTheme("BaseLight")); // or appStyle.Item1
            */

            // Test translations
            //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("zh-HANS");

            // upgrade assembly version of saved settings (if required)
            //Certify.Properties.Settings.Default.UpgradeSettingsVersion(); // deprecated
            //Certify.Management.SettingsManager.LoadAppSettings();

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += CurrentDomain_UnhandledException;

            base.OnStartup(e);

            // MainViewModel.LoadSettings();

            await Dispatcher.InvokeAsync(async () =>
            {
                // FIXME: async blocking
                MainViewModel.Preferences = await MainViewModel.CertifyClient.GetPreferences();

                var list = await MainViewModel.CertifyClient.GetManagedSites(new Models.ManagedSiteFilter());
                MainViewModel.ManagedSites = new System.Collections.ObjectModel.ObservableCollection<Models.ManagedSite>(list);


                //init telemetry if enabled
                InitTelemetry();
            });

            //check version capabilities
            MainViewModel.PluginManager = new Management.PluginManager();

            MainViewModel.PluginManager.LoadPlugins();

            var licensingManager = MainViewModel.PluginManager.LicensingManager;
            if (licensingManager != null)
            {
                if (licensingManager.IsInstallRegistered(ViewModel.AppModel.ProductTypeId, Certify.Management.Util.GetAppDataFolder()))
                {
                    MainViewModel.IsRegisteredVersion = true;
                }
            }

        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var feedbackMsg = "";
            if (e.ExceptionObject != null)
            {
                feedbackMsg = "An error occurred: " + ((Exception)e.ExceptionObject).ToString();
            }

            var d = new Windows.Feedback(feedbackMsg, isException: true);
            d.ShowDialog();
        }

        private void InitTelemetry()
        {
            if (MainViewModel.Preferences.EnableAppTelematics)
            {
                tc = new Certify.Management.Util().InitTelemetry();
                tc.TrackEvent("Start");
            }
            else
            {
                tc = null;
            }
        }
    }
}