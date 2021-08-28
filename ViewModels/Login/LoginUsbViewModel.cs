using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using Pete.Services.Interfaces;
using Pete.Views.Login;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using TNO.Pete.E2fa;
using TNO.Pete.E2fa.UsbHasher;

namespace Pete.ViewModels.Login
{
    [RegionMemberLifetime(KeepAlive = false)]
    public class LoginUsbViewModel : BindableBase, INavigationAware
    {
        #region Consts
        private const string TEXT_DEFAULT = "please replug the usb you used as the 2fa device";
        private const string TEXT_CANCELLED = "the operation to load the 2fa key was cancelled, please try again";
        private const string TEXT_TAMPERED = "the necessary 2fa files have been tampered with";
        private const string TEXT_FAILED = "failed to verify the 2fa device, please try again";
        #endregion

        #region Private
        private bool _UsbDetected;
        private bool _UsbHadError;
        private string _StatusText = TEXT_DEFAULT;
        private readonly IEncryptionModule _EncryptionModule;
        private readonly IRegionManager _RegionManager;
        private readonly Dispatcher _Dispatcher;
        private Action<Action<string>> _GetLoginDataMethod;
        private DelegateCommand _LostUsbCommand;
        private bool _AllowLoops = true;
        #endregion

        #region Properties
        public bool UsbDetected { get => _UsbDetected; private set => _Dispatcher.Invoke(() => SetProperty(ref _UsbDetected, value)); }
        public bool UsbHadError { get => _UsbHadError; private set => _Dispatcher.Invoke(() => SetProperty(ref _UsbHadError, value)); }
        public string StatusText { get => _StatusText; private set => _Dispatcher.Invoke(() => SetProperty(ref _StatusText, value)); }
        public DelegateCommand LostUsbCommand { get => _LostUsbCommand; private set => SetProperty(ref _LostUsbCommand, value); }
        #endregion
        public LoginUsbViewModel(IEncryptionModule encryptionModule, IRegionManager regionManager)
        {
            _EncryptionModule = encryptionModule;
            _RegionManager = regionManager;

            _Dispatcher = Dispatcher.CurrentDispatcher;

            LostUsbCommand = new DelegateCommand(LostUsbCommandCallback, () => !UsbDetected).ObservesProperty(() => UsbDetected);
            Task.Run(ListenForUsb);
        }

        #region Methods
        private void LostUsbCommandCallback() => _RegionManager.RequestNavigate(RegionNames.MainRegion, nameof(LoginLostUsb), App.DebugNavigationCallback);
        private void ListenForUsb()
        {
            List<DriveInfo> last = PeteUsbHasher.GetValidDrives();
            while (_AllowLoops)
            {
                List<DriveInfo> current = PeteUsbHasher.GetValidDrives();
                List<DriveInfo> n = PeteUsbHasher.GetNewDrives(last, current);
                if (n.Count > 0)
                {
                    UsbDriveDetected(n[0]);
                    return;
                }
                else
                {
                    last = current;
                    Task.Delay(250).Wait();
                }
            }
        }
        private void UsbDriveDetected(DriveInfo info)
        {
            UsbDetected = true;
            UsbHadError = false;
            StatusText = "checking usb...";
            try
            {
                if (PeteE2fa.VerifyUSB(info))
                {
                    _EncryptionModule.Device2FA = info;
                    _Dispatcher.Invoke(() => _RegionManager.NavigateTaskScreen(RegionNames.MainRegion, Get2FAKey));
                }
                else
                {
                    StatusText = TEXT_TAMPERED;
                    UsbHadError = true;
                    UsbDetected = false;
                }
            }
            catch
            {
                StatusText = TEXT_FAILED;
                UsbHadError = true;
                UsbDetected = false;
            }

        }
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            _AllowLoops = true;
            if (navigationContext.Parameters.TryGetValue("login-method", out Action<Action<string>> loginMethod))
                _GetLoginDataMethod = loginMethod;
            if (navigationContext.Parameters.TryGetValue("status", out string status))
            {
                UsbHadError = true;
                StatusText = status;
            }
        }
        public bool IsNavigationTarget(NavigationContext navigationContext) => false;
        public void OnNavigatedFrom(NavigationContext navigationContext) => _AllowLoops = false;
        private void Get2FAKey(Action<string> updateStatus)
        {
            updateStatus("requesting 2fa key");

            if (!_EncryptionModule.LoadFAKey())
                _Dispatcher.Invoke(() => _RegionManager.RequestNavigate(RegionNames.MainRegion, nameof(LoginUsb), App.DebugNavigationCallback, new NavigationParameters()
                {
                    { "status", TEXT_CANCELLED },
                    { "login-method", _GetLoginDataMethod },
                }));
            else
            {
                updateStatus("deriving encryption key");
                _EncryptionModule.LoadAesKey();

                _Dispatcher.Invoke(() => _RegionManager.NavigateTaskScreen(RegionNames.MainRegion, _GetLoginDataMethod));
            }
        }
        #endregion
    }
}
