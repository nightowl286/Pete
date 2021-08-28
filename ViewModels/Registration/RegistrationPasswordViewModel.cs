using System;
using System.Collections.Generic;
using System.Linq;
using Pete.Services.Interfaces;
using Pete.Views.Registration;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;

namespace Pete.ViewModels.Registration
{
    [RegionMemberLifetime(KeepAlive = false)]
    public class RegistrationPasswordViewModel : BindableBase
    {
        #region Private
        private string _Password;
        private string _PasswordRepeat;
        private DelegateCommand _ContinueCommand;
        private readonly IRegionManager _RegionManager;
        private readonly IEncryptionModule _EncryptionModule;
        private string _PasswordRepeatError;
        #endregion

        #region Properties
        public string Password { get => _Password; set => SetProperty(ref _Password, value, PasswordRepeatChanged); }
        public string PasswordRepeat { get => _PasswordRepeat; set => SetProperty(ref _PasswordRepeat, value, PasswordRepeatChanged); }
        public string PasswordRepeatError { get => _PasswordRepeatError; private set => SetProperty(ref _PasswordRepeatError, value, PasswordRepeatChanged); }
        public DelegateCommand ContinueCommand { get => _ContinueCommand; private set => SetProperty(ref _ContinueCommand, value); }
        #endregion
        public RegistrationPasswordViewModel(IRegionManager regionManager, IEncryptionModule encryptionModule)
        {
            _RegionManager = regionManager;
            _EncryptionModule = encryptionModule;
            ContinueCommand = new DelegateCommand(Continue, CanContinue).ObservesProperty(() => Password).ObservesProperty(() => PasswordRepeat);
        }

        #region Methods
        private void PasswordRepeatChanged()
        {
            if (Password?.Length > 0 && PasswordRepeat?.Length > 0 && Password != PasswordRepeat)
                PasswordRepeatError = "passwords must match";
            else
                PasswordRepeatError = null;
        }
        private bool CanContinue() => Password?.Length > 0 && Password == PasswordRepeat;
        private void Continue()
        {
            _EncryptionModule.SetMaster(Password);
            _RegionManager.RequestNavigate(RegionNames.MainRegion, nameof(RegistrationUsb));
        }
        #endregion
    }
}