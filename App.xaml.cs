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
#if DEBUG
        public const bool REQUIRE_ADMIN = false;
#endif
        #endregion

        #region Prism
        protected override Window CreateShell()
        {

            ReloadImages();
            MakeGlobalCommands();
            Container.Resolve<IActivityLog>();

            Container.Resolve<ISettings>().Load();

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

            containerRegistry.RegisterDialogWindow<GeneralDialogWindow>(nameof(GeneralDialogWindow));
            containerRegistry.RegisterDialog<ConfirmRemoveDialog>();
            containerRegistry.RegisterDialog<MessageDialog>();
            containerRegistry.RegisterDialog<TextInputDialog>();

            containerRegistry.RegisterSingleton<IEncryptionModule, EncryptionModule>();
            containerRegistry.RegisterSingleton<ICategoryStore, CategoryStore>();
            containerRegistry.RegisterSingleton<ISettings, Settings>();
            containerRegistry.RegisterSingleton<IEntryStore, EntryStore>();
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
        #endregion

        #region Functions
        private static bool IsAdmin()
        {
#if DEBUG
            if (!REQUIRE_ADMIN) return true;
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
            if (SHOW_NAVIGATION_DEBUG && result.Result == false)
                Debug.WriteLine($"Navigating to '{result.Context.Uri}' failed, reason: {result.Error?.Message}");
        }
        public static void DisplayThreadId([CallerMemberName] string name = "Unknown") => Debug.WriteLine($"[{name}] Thread ID: {System.Windows.Threading.Dispatcher.CurrentDispatcher.Thread.ManagedThreadId}");
        #endregion
    }
}
