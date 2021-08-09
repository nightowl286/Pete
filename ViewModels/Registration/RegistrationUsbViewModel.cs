using System;
using System.Collections.Generic;
using System.Linq;
using Prism.Commands;
using Prism.Mvvm;
using TNO.Pete.E2fa;
using TNO.Pete.E2fa.UsbHasher;
using System.IO;
using Pete.Services.Interfaces;
using System.Threading.Tasks;
using System.Windows.Threading;
using Prism.Regions;
using Pete.Views;
using Pete.Views.Registration;
using System.Diagnostics;

namespace Pete.ViewModels.Registration
{
    public class RegistrationUsbViewModel : BindableBase, INavigationAware
    {
        #region Consts
        private const string TEXT_DEFAULT = "please replug the usb you wish to use as the 2fa device";
        private const string TEXT_CANCELLED = "the operation to store the 2fa key was cancelled, please try again";
        private const string TEXT_MISSING_FILES = "this usb does not have the necessary 2fa files";
        private const string TEXT_FAILED = "failed to verify the 2fa device, please try again";
        #endregion

        #region Private
        private bool _UsbDetected;
        private bool _UsbHadError;
        private string _StatusText = TEXT_DEFAULT;
        private readonly IEncryptionModule _EncryptionModule;
        private readonly IRegionManager _RegionManager;
        private readonly IActivityLog _ActivityLog;
        private readonly Dispatcher _Dispatcher;
        private string _Target = nameof(Dashboard);
        private Action<Action<string>> _TargetMethod;
        private DriveInfo _DetectedUsb;
        private byte[] _UsbHash;
        private byte[] _UsbId;
        #endregion

        #region Properties
        public bool UsbDetected { get => _UsbDetected; private set => _Dispatcher.Invoke(() => SetProperty(ref _UsbDetected, value)); }
        public bool UsbHadError { get => _UsbHadError; private set => _Dispatcher.Invoke(() => SetProperty(ref _UsbHadError, value)); }
        public string StatusText { get => _StatusText; private set => _Dispatcher.Invoke(() => SetProperty(ref _StatusText, value)); }
        #endregion
        public RegistrationUsbViewModel(IEncryptionModule encryptionModule, IRegionManager regionManager, IActivityLog activityLog)
        {
            _TargetMethod = GenerateNecessaryData; 
            _EncryptionModule = encryptionModule;
            _RegionManager = regionManager;
            _ActivityLog = activityLog;

            _Dispatcher = Dispatcher.CurrentDispatcher;

            Task.Run(ListenForUsb);
        }

        #region Methods
        private void ListenForUsb()
        {
            List<DriveInfo> last = PeteUsbHasher.GetValidDrives();
            while (true)
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
                if (PeteE2fa.HashUsb(info, out byte[] hash, out byte[] usbID))
                {
                    _UsbId = usbID;
                    _UsbHash = hash;
                    _DetectedUsb = info;
                    _Dispatcher.Invoke(() => _RegionManager.NavigateTaskScreen(RegionNames.MainRegion, _TargetMethod, _Target));
                }
                else
                {
                    UsbDetected = false;
                    UsbHadError = true;
                    StatusText = TEXT_MISSING_FILES;
                    Task.Run(ListenForUsb);
                }
            }
            catch
            {
                StatusText = TEXT_FAILED;
                UsbDetected = false;
                UsbHadError = true;
                Task.Run(ListenForUsb);

            }
        }
        private void GenerateNecessaryData(Action<string> updateStatus)
        {
            if (!_EncryptionModule.HasMasterKey)
            {
                updateStatus("deriving master key");
                _EncryptionModule.GenerateMasterKey();
            }
            if (_EncryptionModule.Device2FA == null)
            {
                updateStatus("storing 2fa device data");
                _EncryptionModule.SaveUsbHash(_UsbHash, _UsbId);
                _EncryptionModule.Device2FA = _DetectedUsb;
            }

            App.InitialiseE2FA();

            if (!_EncryptionModule.HasFAKey)
            {
                updateStatus("generating 2fa key");
                _EncryptionModule.Generate2FAKey();
            }

            if (!_EncryptionModule.HasAesKey)
            {
                updateStatus("deriving encryption key");
                _EncryptionModule.GenerateAesKey();
            }

            updateStatus("storing 2fa key");
            bool storedKey = _EncryptionModule.StoreFAKey();
            if (storedKey)
            {
                Debug.WriteLine($"[Registration] key successfully stored on 2fa device.");

                _ActivityLog.LoadEncrypted();
                _ActivityLog.Log(Models.Logs.LogType.Register);

                _Dispatcher.Invoke(() => _RegionManager.RequestNavigate(RegionNames.MainRegion, nameof(Dashboard), App.DebugNavigationCallback));
            }
            else
            {
                _Dispatcher.Invoke(() => _RegionManager.RequestNavigate(RegionNames.MainRegion, nameof(RegistrationUsb), App.DebugNavigationCallback, new NavigationParameters()
                {
                    {"status",TEXT_CANCELLED }
                }));
            };
        }
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            if (navigationContext.Parameters.TryGetValue("status", out string error))
            {
                UsbHadError = true;
                StatusText = error;
            }
            if (navigationContext.Parameters.TryGetValue("target", out string target))
                _Target = target;
            if (navigationContext.Parameters.TryGetValue("target-method", out Action<Action<string>> targetMethod))
                _TargetMethod = targetMethod;
        }
        public bool IsNavigationTarget(NavigationContext navigationContext) => false;
        public void OnNavigatedFrom(NavigationContext navigationContext) { }
        #endregion
    }
}
