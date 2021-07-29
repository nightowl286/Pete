using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Pete.Cryptography;
using Pete.Services.Interfaces;
using Pete.Views;
using Pete.Views.Login;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using TNO.Pete.E2fa;
using TNO.Pete.E2fa.UsbHasher;

namespace Pete.ViewModels.Login
{
    public class LoginLostUsbViewModel : BindableBase, INavigationAware
    {
        #region Consts
        private const string TEXT_USB_DEFAULT = "please replug the usb you wish to use as the 2fa device";
        private const string TEXT_USB_CANCELLED = "the operation to store the 2fa key was cancelled, please try again";
        private const string TEXT_USB_MISSING_FILES = "this usb does not have the necessary 2fa files";
        private const string TEXT_USB_VALID = "valid usb detected";
        private const string TEXT_USB_FAILED = "failed to hash the 2fa device, please try again";

        private const string TEXT_DEFAULT = "please fill out the following info about the previous 2fa device";
        private const string TEXT_INVALID = "the given information was invaild, please ensure you have entered it correctly";
        private const int STRING_HASH_LENGTH = 53;
        #endregion

        #region Private
        private bool _UsbDetected;
        private bool _UsbHadError;
        private bool _UsbValid;
        private string _UsbStatusText = TEXT_USB_DEFAULT;
        private string _StatusText = TEXT_DEFAULT;
        private bool _HadError;
        private Dispatcher _Dispatcher;
        private readonly IEncryptionModule _Encryption;
        private readonly IRegionManager _RegionManager;
        private DelegateCommand _GoBackCommand;
        private DelegateCommand _ContinueCommand;
        private string _DeviceID;
        private string _DeviceHash;
        private string _IDError;
        private string _HashError;
        private DriveInfo _DetectedUsb;
        private bool _AllowLoops = true;
        #endregion

        #region Properties
        public bool UsbDetected { get => _UsbDetected; private set => _Dispatcher.Invoke(() => SetProperty(ref _UsbDetected, value)); }
        public bool UsbHadError { get => _UsbHadError; private set => _Dispatcher.Invoke(() => SetProperty(ref _UsbHadError, value)); }
        public bool HadError { get => _HadError; private set => _Dispatcher.Invoke(() => SetProperty(ref _HadError, value)); }
        public bool UsbValid { get => _UsbValid; private set => _Dispatcher.Invoke(() => SetProperty(ref _UsbValid, value)); }
        public string UsbStatusText { get => _UsbStatusText; private set => _Dispatcher.Invoke(() => SetProperty(ref _UsbStatusText, value)); }
        public string StatusText { get => _StatusText; private set => _Dispatcher.Invoke(() => SetProperty(ref _StatusText, value)); }
        public DelegateCommand GoBackCommand { get => _GoBackCommand; private set => SetProperty(ref _GoBackCommand, value); }
        public DelegateCommand ContinueCommand { get => _ContinueCommand; private set => SetProperty(ref _ContinueCommand, value); }
        public string DeviceID { get => _DeviceID; set => SetProperty(ref _DeviceID, value, DeviceIDChanged); }
        public string DeviceHash { get => _DeviceHash; set => SetProperty(ref _DeviceHash, value, DeviceHashChanged); }
        public string IDError { get => _IDError; private set => SetProperty(ref _IDError, value); }
        public string HashError { get => _HashError; private set => SetProperty(ref _HashError, value); }
        #endregion
        public LoginLostUsbViewModel(IRegionManager manager, IEncryptionModule encryption)
        {
            _Dispatcher = Dispatcher.CurrentDispatcher;

            _RegionManager = manager;
            _Encryption = encryption;

            ContinueCommand = new DelegateCommand(ContinueCallback, CanContinue).ObservesProperty(() => DeviceID).ObservesProperty(() => IDError).ObservesProperty(() => DeviceHash).ObservesProperty(() => HashError).ObservesProperty(() => UsbValid);

        }

        #region Events
        private void DeviceIDChanged() => VerifyDeviceInfo(err => IDError = err, DeviceID, "id");
        private void DeviceHashChanged() => VerifyDeviceInfo(err => HashError = err, DeviceHash, "hash");
        #endregion

        #region Methods
        private bool CanContinue() => (DeviceID?.Length > 0 && IDError == null) && (DeviceHash?.Length > 0 && HashError == null) && UsbValid;
        private void ContinueCallback()
        {
            byte[] hashArr = PeteUsbHasher.ConvertStringToHash(DeviceHash);
            byte[] idArr = PeteUsbHasher.ConvertStringToHash(DeviceID);

            bool hashValid = PBKDF2.SameHash(hashArr, _Encryption.DeviceHash);
            bool idValid = PBKDF2.SameHash(idArr, _Encryption.DeviceID);

            if (!hashValid || !idValid)
            {
                HadError = true;
                StatusText = TEXT_INVALID;
                if (!hashValid) HashError = "this hash does not match the stored hash";
                if (!idValid) IDError = "this id does not match the stored id";
            }
            else
                _RegionManager.NavigateTaskScreen(RegionNames.MainRegion, ContinueTask);
        }
        private void ContinueTask(Action<string> updateStatus)
        {
            updateStatus("storing new device data");

            if (!PeteE2fa.HashUsb(_DetectedUsb, out byte[] hash, out byte[] id))
            {
                NavigateBack(DeviceID, DeviceHash, null, null, TEXT_USB_FAILED);
                return;
            }

            _Encryption.SaveUsbHash(hash, id);
            _Encryption.Device2FA = _DetectedUsb;

            App.InitialiseE2FA();
            updateStatus("requesting 2fa key");

            if (!_Encryption.LoadFAKey())
            {
                NavigateBack(DeviceID, DeviceHash, null, null, TEXT_USB_CANCELLED);
                return;
            }

            updateStatus("deriving encryption key");
            _Encryption.LoadAesKey();

            _Dispatcher.Invoke(() => _RegionManager.RequestNavigate(RegionNames.MainRegion, nameof(Dashboard), App.DebugNavigationCallback));
        }
        private void NavigateBack(string id, string hash, string error, DriveInfo device, string deviceError)
        {
            NavigationParameters p = new NavigationParameters();
            if (id != null) p.Add("id", id);
            if (hash != null) p.Add("hash", hash);
            if (error != null) p.Add("error", error);
            if (device != null) p.Add("device", device);
            if (deviceError != null) p.Add("device-error", deviceError);

            _Dispatcher.Invoke(() => _RegionManager.RequestNavigate(RegionNames.MainRegion, nameof(LoginLostUsb), App.DebugNavigationCallback, p));
        }
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
                    Task.Delay(100).Wait();
                }
            }
        }
        private void UsbDriveDetected(DriveInfo info)
        {
            UsbDetected = true;
            UsbHadError = false;
            UsbStatusText = "checking usb...";
            if (PeteE2fa.HashUsb(info, out _, out _))
            {
                UsbValid = true;
                UsbStatusText = TEXT_USB_VALID;
                _DetectedUsb = info;
                Task.Run(DetectUsbRemoval);
            }
            else
            {
                UsbHadError = true;
                UsbDetected = false;
                UsbStatusText = TEXT_USB_MISSING_FILES;

                Task.Run(ListenForUsb);
            }
        }
        private void DetectUsbRemoval()
        {
            while (_AllowLoops)
            {
                List<DriveInfo> pluggedIn = PeteUsbHasher.GetValidDrives();
                if (pluggedIn.Any(u => u.Name.Equals(_DetectedUsb?.Name)))
                    Task.Delay(100).Wait();
                else
                {
                    _DetectedUsb = null;
                    UsbValid = false;
                    UsbDetected = false;
                    UsbStatusText = TEXT_USB_DEFAULT;
                    Task.Run(ListenForUsb);
                    return;
                }
            }
        }
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            _AllowLoops = true;

            Action toStart = ListenForUsb;

            GoBackCommand = new DelegateCommand(navigationContext.NavigationService.Journal.GoBack, () => !(UsbDetected && !UsbValid))
                .ObservesProperty(() => UsbDetected).ObservesProperty(() => UsbValid);

            if (navigationContext.Parameters.TryGetValue("id", out string id)) DeviceID = id;
            if (navigationContext.Parameters.TryGetValue("hash", out string hash)) DeviceHash = hash;
            if (navigationContext.Parameters.TryGetValue("error", out string error))
            {
                StatusText = error;
                HadError = true;
            }
            if (navigationContext.Parameters.TryGetValue("device", out DriveInfo device))
            {
                _DetectedUsb = device;
                UsbValid = true;
                UsbDetected = true;
                toStart = DetectUsbRemoval;
            }
            if (navigationContext.Parameters.TryGetValue("device-error", out string deviceErr))
            {
                _DetectedUsb = null;
                UsbStatusText = deviceErr;
                UsbDetected = false;
                UsbDetected = false;
                UsbHadError = true;
            }

            Task.Run(toStart);
        }
        public bool IsNavigationTarget(NavigationContext navigationContext) => false;
        public void OnNavigatedFrom(NavigationContext navigationContext) => _AllowLoops = false;
        #endregion

        #region Functions
        private static bool IsValidInfo(string info) => info?.Length == STRING_HASH_LENGTH && PeteUsbHasher.IsStringHashValid(info);
        private static void VerifyDeviceInfo(Action<string> setError, string info, string label) => setError((info?.Length == 0 || IsValidInfo(info)) ? null : $"the entered {label} does not match the required format");
        #endregion
    }
}
