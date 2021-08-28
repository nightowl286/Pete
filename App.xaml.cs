using System.Windows;
using Pete.Views;
using Pete.Views.Registration;
using Pete.ViewModels.Registration;
using Prism.Ioc;
using Prism.Regions;
using Pete.Services.Interfaces;
using Pete.Services;
using Pete.Cryptography;
using System.Diagnostics;
using TNO.Pete.E2fa;
using Prism.DryIoc;
using TNO.Pete.E2fa.UsbHasher;
using System.IO;
using System.Windows.Media.Imaging;
using System;
using System.Linq;
using Pete.Views.Login;
using Prism.Commands;
using System.Windows.Media;
using Pete.Views.Dialogs;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Security.Principal;
using Microsoft.Win32;
using System.Windows.Threading;
using Prism.Services.Dialogs;

namespace Pete
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>

    public partial class App
    {
        #region Consts
        private const bool SHOW_NAVIGATION_DEBUG = true;
        public const int SLIDE_ANIMATION_INTERVAL = 15;
        public const int SLIDE_ANIMATION_INTERVAL_MINIMUM = 1; // requires at least 1 otherwise the text won't update and it will look like it froze
        public const double SLIDE_ANIMATION_INTERVAL_DECREMENT = 0.2;
        public const bool REPORT_BUGS = true;
        public const string GITHUB_LINK = @"https://github.com/nightowl286";
        public const string ENERGY_DRINKS = "36";
#if DEBUG
        public const bool REQUIRE_ADMIN = true;
#endif
        #endregion

        #region Private
        private IBugReporter _BugReporter;
        private IDialogService _DialogService;
        #endregion

        #region Prism
        protected override Window CreateShell()
        {
            _BugReporter = Container.Resolve<IBugReporter>();
            _DialogService = Container.Resolve<IDialogService>();

            ReloadImages();
            MakeGlobalCommands();
            _ = Container.Resolve<IActivityLog>();

            return Container.Resolve<MainWindow>();
        }
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<RegistrationPassword>();
            containerRegistry.RegisterForNavigation<RegistrationUsb>();
            containerRegistry.RegisterForNavigation<LoginPassword>();
            containerRegistry.RegisterForNavigation<LoginUsb>();
            containerRegistry.RegisterForNavigation<LoginLostUsb>();
            containerRegistry.RegisterForNavigation<TaskScreen>();
            containerRegistry.RegisterForNavigation<Dashboard>();
            containerRegistry.RegisterForNavigation<EntryEditor>();
            containerRegistry.RegisterForNavigation<EntryList>();
            containerRegistry.RegisterForNavigation<Views.ActivityLog>();
            containerRegistry.RegisterForNavigation<RequireAdmin>();
            containerRegistry.RegisterForNavigation<Views.Settings>();

            containerRegistry.RegisterDialogWindow<GeneralDialogWindow>(nameof(GeneralDialogWindow));
            containerRegistry.RegisterDialog<ConfirmRemoveDialog>();
            containerRegistry.RegisterDialog<MessageDialog>();
            containerRegistry.RegisterDialog<TextInputDialog>();
            containerRegistry.RegisterDialog<BugReportDialog>();

            containerRegistry.RegisterSingleton<IEncryptionModule, EncryptionModule>();
            containerRegistry.RegisterSingleton<ICategoryStore, CategoryStore>();
            containerRegistry.RegisterSingleton<ISettings, Services.Settings>();
            containerRegistry.RegisterSingleton<IEntryStore, EntryStore>();
            containerRegistry.RegisterSingleton<IBugReporter, BugReporter>();
            containerRegistry.RegisterSingleton<IActivityLog, Services.ActivityLog>();
            containerRegistry.Register<IIDManager, IDManager>();
        }
        #endregion

        #region Methods
        
        public void DecideStartScreen(IRegionManager manager)
        {
            if (!IsAdmin())
            {
                manager.RequestNavigate(RegionNames.MainRegion, nameof(RequireAdmin), DebugNavigationCallback);
                return;
            }

            IEncryptionModule encryption = Container.Resolve<IEncryptionModule>();

#if DEBUG
            if (SkipToDashboard(manager))
                return;
#endif


            manager.RequestNavigate(RegionNames.MainRegion, encryption.HasSavedMaster() ? nameof(LoginPassword) : nameof(RegistrationPassword), DebugNavigationCallback);
        }

#if DEBUG
        private bool SkipToDashboard(IRegionManager manager)
        {
            IEncryptionModule encryption = Container.Resolve<IEncryptionModule>();

            if (!(encryption as EncryptionModule).LoadDebug()) return false;

            IActivityLog log = Container.Resolve<IActivityLog>();
            log.LoadEncrypted();
            log.Log(Models.Logs.LogType.Login);

            manager.RequestNavigate(RegionNames.MainRegion, nameof(Dashboard), DebugNavigationCallback);
            return true;
        }
#endif
        private void MakeGlobalCommands()
        {
            GlobalCommands.ExitCommand = new DelegateCommand(App.Current.Shutdown);
            GlobalCommands.OpenUrlCommand = new DelegateCommand<string>(OpenUrl);
            GlobalCommands.CopyTextToClipboardCommand = new DelegateCommand<string>(CopyToClipboard);
        }
        private void ReloadImages()
        {
            var dct = Resources.MergedDictionaries.First(r => r.Contains("logo"));
            string ImagesPath = Path.Combine(Environment.CurrentDirectory, "Resources", "Images");
            dct["logo"] = LoadBitmap(Path.Combine(ImagesPath, "logoManagerForeground.png"));
        }
        private BitmapImage LoadBitmap(string path)
        {
            BitmapImage bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnDemand;
            bmp.UriSource = new Uri(path);
            bmp.EndInit();
            return bmp;
        }
        private void ReportBug(Exception ex, object sender, Dictionary<string, string> otherData = null)
        {
            if (_BugReporter == null) return;

            if (otherData == null) otherData = new Dictionary<string, string>();
            if (sender != null)
            {
                otherData.Add("sender-type", sender.GetType().FullName);
                otherData.Add("sender", sender.ToString());
            }
            string path = _BugReporter.Report(ex, otherData);
            _DialogService.BugReport(path);
        }
        #endregion

        #region Functions
        public static void CopyToClipboard(string text)
        {
            Clipboard.SetText(text);
            Clipboard.Flush();
        }
        private static bool IsAdmin()
        {
#if DEBUG
#pragma warning disable CS0162 // Unreachable code detected
            if (!REQUIRE_ADMIN) return true;
#pragma warning restore CS0162 // Unreachable code detected
#endif

            try
            {
                WindowsIdentity user = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(user);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }
        private static IContainerProvider GetContainer() => (App.Current as PrismApplication).Container;
        public static void InitialiseE2FA()
        {
            IEncryptionModule crypto = GetContainer().Resolve<IEncryptionModule>();

            string dev = PeteUsbHasher.ConvertHashToString(crypto.DeviceID);
            string hash = PeteUsbHasher.ConvertHashToString(crypto.DeviceHash);

            PeteE2fa.Initialize(true, "Pete", dev, hash, "#008b8b");
        }
        public static void DebugNavigationCallback(NavigationResult result)
        {
            if (result.Result == false)
            {
                if (SHOW_NAVIGATION_DEBUG)
                    Debug.WriteLine($"Navigating to '{result.Context.Uri}' failed, reason: {result.Error?.Message}");
                if (REPORT_BUGS)
                    (App.Current as App).ReportBug(result.Error, null);
            }
        }
        public static void DisplayThreadId([CallerMemberName] string name = "Unknown") => Debug.WriteLine($"[{name}] Thread ID: {Dispatcher.CurrentDispatcher.Thread.ManagedThreadId}");
        public static int ThreadId() => Dispatcher.CurrentDispatcher.Thread.ManagedThreadId;
        public static void RestartAsAdmin()
        {
            Debug.WriteLine("Attempting to restart as admin");
            ProcessStartInfo info = new ProcessStartInfo()
            {
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = Process.GetCurrentProcess().MainModule.FileName,
                Verb = "runas"
            };
            try
            {
                Process.Start(info);
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to restart as admin. Ex: {ex.Message}");
                Environment.Exit(0);
            }

        }
        public static void OpenUrl(string url) => Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true, UseShellExecute = true, WindowStyle = ProcessWindowStyle.Hidden });
        #endregion

        #region Events
        private void PrismApplication_Startup(object sender, StartupEventArgs e)
        {
            if (REPORT_BUGS)
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }
        private void PrismApplication_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (REPORT_BUGS)
            {
                e.Handled = true;
                ReportBug(e.Exception, sender);
            }
        }
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) => ReportBug(e.ExceptionObject as Exception, sender);
        #endregion

    }
}
