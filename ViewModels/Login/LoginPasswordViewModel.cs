using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using Pete.Services.Interfaces;
using Pete.Views;
using Pete.Views.Login;
using Pete.Views.Registration;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using TNO.Pete.E2fa;

namespace Pete.ViewModels.Login
{
    [RegionMemberLifetime(KeepAlive = false)]
    public class LoginPasswordViewModel : BindableBase
    {
        #region Private
        private DelegateCommand _LoginCommand;
        private string _Password;
        private string _PasswordError;
        private readonly IRegionManager _RegionManager;
        private readonly IEncryptionModule _Encryption;
        private readonly IActivityLog _ActivityLog;
        private bool _CanContinue;
        private bool _CanEditPassword = true;
        private readonly Dispatcher _Dispatcher;
        #endregion

        #region Properties
        public DelegateCommand LoginCommand { get => _LoginCommand; private set => SetProperty(ref _LoginCommand, value); }
        public string Password { get => _Password; set => SetProperty(ref _Password, value, PasswordChanged); }
        public string PasswordError { get => _PasswordError; private set => SetProperty(ref _PasswordError, value); }
        public bool CanEditPassword { get => _CanEditPassword; private set => SetProperty(ref _CanEditPassword, value); }
        public bool CanContinue { get => _CanContinue; private set => SetProperty(ref _CanContinue, value); }
        #endregion
        public LoginPasswordViewModel(IRegionManager regionManager, IEncryptionModule encryption, IActivityLog activityLog)
        {
            _RegionManager = regionManager;
            _Encryption = encryption;
            _ActivityLog = activityLog;

            LoginCommand = new DelegateCommand(Login).ObservesCanExecute(() => CanContinue);
            _Dispatcher = Dispatcher.CurrentDispatcher;
        }

        #region Methods
        private void PasswordChanged()
        {
            PasswordError = null;
            CanContinue = Password?.Length > 0;
        }
        private void Login()
        {
            CanContinue = false;
            CanEditPassword = false;
            Task.Run(LoginTask);
        }
        private void LoginTask()
        {
            bool correct = _Encryption.CheckMaster(Password);
            if (correct)
            {
                bool deviceSaved = _Encryption.HasSavedDevice();
                string target = deviceSaved ? nameof(LoginUsb) : nameof(RegistrationUsb);

                if (deviceSaved)
                {
                    _Encryption.LoadUsbHash();
                    App.InitialiseE2FA();
                }

                _Dispatcher.Invoke(() => _RegionManager.RequestNavigate(RegionNames.MainRegion, target, App.DebugNavigationCallback, new NavigationParameters()
                    {
                        {"login-method", new Action<Action<string>>(GetLoginData) }
                    }));
            }
            else
            {
                _Dispatcher.Invoke(() => {
                    CanEditPassword = true;
                    PasswordError = "the entered password is incorrect";
                });
                _ActivityLog.Log(Models.Logs.LogType.FailedLogin);
            }
        }
        private void GetLoginData(Action<string> updateStatus)
        {
            updateStatus("Get login data");

            _ActivityLog.LoadEncrypted();
            _ActivityLog.Log(Models.Logs.LogType.Login);
            

            _Dispatcher.Invoke(() => _RegionManager.RequestNavigate(RegionNames.MainRegion, nameof(Dashboard), App.DebugNavigationCallback));
        }
        #endregion
    }
}
