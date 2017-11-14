﻿using Certify.Locales;
using Microsoft.ApplicationInsights;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

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

            Mouse.OverrideCursor = Cursors.AppStarting;

            await MainViewModel.InitServiceConnections();

            if (MainViewModel.IsServiceAvailable)
            {
                await MainViewModel.LoadSettingsAsync();
            }

            Mouse.OverrideCursor = Cursors.Arrow;

            // quit if service/service client cannot connect
            if (!MainViewModel.IsServiceAvailable)
            {
                MessageBox.Show("Certify SSL Manager service is not started. Please restart the service.");
                App.Current.Shutdown();
                return;
            }

            //init telemetry if enabled
            InitTelemetry();

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

            if (!MainViewModel.IsServiceAvailable) return;

            //check for any startup actions required such as vault import

            /* if (!this.MainViewModel.ManagedSites.Any())
             {
                 //if we have a vault, preview import.
                 this.MainViewModel.PreviewImport(sanMergeMode: true);
             }*/

            // check if IIS is available, if so also populates IISVersion
            await MainViewModel.CheckServerAvailability(Models.StandardServerTypes.IIS);

            if (MainViewModel.IsIISAvailable)
            {
                if (MainViewModel.ImportedManagedSites.Any())
                {
                    //show import ui
                    var d = new Windows.ImportManagedSites();
                    d.ShowDialog();
                }
            }
            else
            {
                //warn if IIS not detected
                MessageBox.Show(SR.MainWindow_IISNotAvailable);
            }

            //FIXME:  checks cause async blocks
            if (!MainViewModel.HasRegisteredContacts)
            {
                //start by registering
                MessageBox.Show(SR.MainWindow_GetStartGuideWithNewCert);
                var d = new Windows.EditContactDialog { };
                d.ShowDialog();
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