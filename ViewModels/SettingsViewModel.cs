using System;
using System.Collections.Generic;
using System.Linq;
using Pete.Services.Interfaces;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using TNO.Pete.E2fa.UsbHasher;
using System.Globalization;
using Prism.Services.Dialogs;
using Pete.Models;

namespace Pete.ViewModels
{
    [RegionMemberLifetime(KeepAlive = false)]
    public class SettingsViewModel : BindableBase, INavigationAware
    {
        #region Private
        private int _OriginalIterations;
        private int _OriginalSaltSize;
        private readonly ISettings _Settings;
        private readonly IEncryptionModule _EncryptionModule;
        private readonly IDialogService _DialogService;
        private string _DeviceID;
        private string _DeviceHash;
        private DelegateCommand _SaveCommand;
        private DelegateCommand _CancelCommand;
        private DelegateCommand _RestoreDefaultsCommand;
        private DelegateCommand<string> _CopyToClipboardCommand;
        private string _Iterations;
        private string _IterationsError;
        private string _SaltSize;
        private string _SaltSizeError;
        private bool _ShowEntryList;
        private Action _GoBackMethod;
        private bool _HasChangedSettings;
        #endregion

        #region Properties
        public string DeviceID { get => _DeviceID; private set => SetProperty(ref _DeviceID, value); }
        public string DeviceHash { get => _DeviceHash; private set => SetProperty(ref _DeviceHash, value); }
        public DelegateCommand SaveCommand { get => _SaveCommand; private set => SetProperty(ref _SaveCommand, value); }
        public DelegateCommand CancelCommand { get => _CancelCommand; private set => SetProperty(ref _CancelCommand, value); }
        public DelegateCommand RestoreDefaultsCommand { get => _RestoreDefaultsCommand; private set => SetProperty(ref _RestoreDefaultsCommand, value); }
        public DelegateCommand<string> CopyToClipboardCommand { get => _CopyToClipboardCommand; private set => SetProperty(ref _CopyToClipboardCommand, value); }
        public string Iterations { get => _Iterations; set => SetProperty(ref _Iterations, value, IterationsChanged); }
        public string IterationsError { get => _IterationsError; set => SetProperty(ref _IterationsError, value); }
        public string SaltSize { get => _SaltSize; set => SetProperty(ref _SaltSize, value, SaltSizeChanged); }
        public string SaltSizeError { get => _SaltSizeError; set => SetProperty(ref _SaltSizeError, value); }
        public bool ShowEntryList { get => _ShowEntryList; set => SetProperty(ref _ShowEntryList, value); }
        public bool HasChangedSettings { get => _HasChangedSettings; private set => SetProperty(ref _HasChangedSettings, value); }
        #endregion
        public SettingsViewModel(ISettings settings, IEncryptionModule encryptionModule, IDialogService dialogService)
        {
            _Settings = settings;
            _EncryptionModule = encryptionModule;
            _DialogService = dialogService;

            _OriginalIterations = settings.Iterations;
            _OriginalSaltSize = settings.SaltSize;

            Iterations = settings.Iterations.ToString("n0");
            SaltSize = settings.SaltSize.ToString("n0");
            ShowEntryList = settings.ShowEntryListAtStart;

            DeviceID = PeteUsbHasher.ConvertHashToString(_EncryptionModule.DeviceID);
            DeviceHash = PeteUsbHasher.ConvertHashToString(_EncryptionModule.DeviceHash);

            SaveCommand = new DelegateCommand(SaveMethod, () => SettingsChanged() && SaltSizeError == null && IterationsError == null).ObservesProperty(() => SaltSizeError).ObservesProperty(() => IterationsError).ObservesProperty(() => SaltSize).ObservesProperty(() => Iterations).ObservesProperty(() => ShowEntryList);

            CancelCommand = new DelegateCommand(CancelMethod);
            RestoreDefaultsCommand = new DelegateCommand(RestoreDefaultMethod);
        }

        #region Events
        private void IterationsChanged() => VerifyNumber(Iterations, ref _Iterations, ref _IterationsError, ISettings.DEF_ITER, nameof(Iterations), "iteration count");
        private void SaltSizeChanged() => VerifyNumber(SaltSize, ref _SaltSize, ref _SaltSizeError, ISettings.DEF_SALT, nameof(SaltSize), "salt size");
        #endregion

        #region Methods
        private void RestoreDefaultMethod()
        {
            _DialogService.Message(RestoreDefaultCallback, "restore default settings?", "are you sure you want to restore the default settings?", ButtonResult.No,
                    new ButtonInfo(ButtonType.Primary, "keep current", ButtonResult.No),
                    new ButtonInfo(ButtonType.Normal, "restore default", ButtonResult.Yes));
        }
        private void RestoreDefaultCallback(ButtonResult result)
        {
            if (result == ButtonResult.Yes)
            {
                _Settings.RestoreDefault();

                _OriginalIterations = _Settings.Iterations;
                _OriginalSaltSize = _Settings.SaltSize;

                Iterations = _Settings.Iterations.ToString("n0");
                SaltSize = _Settings.SaltSize.ToString("n0");
                ShowEntryList = _Settings.ShowEntryListAtStart;

            }
        }
        private bool SettingsChanged()
        {
            if (_SaltSizeError != null || _IterationsError != null) HasChangedSettings = true;
            else if (TryParseNumber(SaltSize, out int saltSize) && saltSize != _OriginalSaltSize) HasChangedSettings = true;
            else if (TryParseNumber(Iterations, out int iterations) && iterations != _OriginalIterations) HasChangedSettings = true;
            else if (_Settings.ShowEntryListAtStart != ShowEntryList) HasChangedSettings = true;
            else HasChangedSettings = false;

            return HasChangedSettings;
        }
        private void CancelMethod()
        {
            if (SettingsChanged())
            {
                _DialogService.Message(CancelCallback, "cancel setting changes?", "you have changed some of the settings, are you sure you want to cancel them?", ButtonResult.No,
                    new ButtonInfo(ButtonType.Primary, "keep editing", ButtonResult.No),
                    new ButtonInfo(ButtonType.Normal, "cancel changes", ButtonResult.Yes));
            }
            else _GoBackMethod?.Invoke();
        }
        private void CancelCallback(ButtonResult result)
        {
            if (result == ButtonResult.Yes)
                _GoBackMethod?.Invoke();
        }
        private void SaveMethod()
        {
            bool modifiedEncrpytion = false;
            if (TryParseNumber(SaltSize, out int saltSize) && saltSize != _OriginalSaltSize) modifiedEncrpytion = true;
            else if (TryParseNumber(Iterations, out int iterations) && iterations != _OriginalIterations) modifiedEncrpytion = true;

            string text = "are you sure you want to save the changes you have made to the settings?";
            if (modifiedEncrpytion)
                text += "\n\nyou have modified the encrpytion parameters which requires the program data to be re-encrypted, this may take a few seconds.";
            _DialogService.Message(SaveCallback, "save setting changes?", text, ButtonResult.No,
                 new ButtonInfo(ButtonType.Primary, "keep editing", ButtonResult.No),
                    new ButtonInfo(ButtonType.Normal, "save changes", ButtonResult.Yes));
        }
        private void SaveCallback(ButtonResult result)
        {
            if (result == ButtonResult.Yes)
            {
                /*_ = TryParseNumber(Iterations, out int newIterations);
                _ = TryParseNumber(SaltSize, out int newSaltSize);

                _Settings.Iterations = newIterations;
                _Settings.SaltSize = newSaltSize;*/
                _Settings.ShowEntryListAtStart = ShowEntryList;

                _Settings.Save();
                _GoBackMethod?.Invoke();
            }
        }
        private void VerifyNumber(string text, ref string field, ref string error, int minimum, string propertyName, string name)
        {
            if (TryParseNumber(text, out int number))
            {
                if (number < minimum)
                    error = $"{name} cannot be lower than {minimum:n0}";
                else
                {
                    field = $"{number:n0}";
                    error = null;
                }
            }
            else
                error = $"{name} must be a full number";

            RaisePropertyChanged(propertyName);
            RaisePropertyChanged($"{propertyName}Error");
        }
        public bool IsNavigationTarget(NavigationContext navigationContext) => false;
        public void OnNavigatedFrom(NavigationContext navigationContext) { }
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            _GoBackMethod = navigationContext.NavigationService.Journal.GoBack;
        }
        #endregion

        #region Functions
        private static bool TryParseNumber(string text, out int number) => int.TryParse(text, NumberStyles.AllowThousands, null, out number);
        #endregion
    }
}
