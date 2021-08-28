using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Pete.Models;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace Pete.ViewModels.Dialogs
{
    [RegionMemberLifetime(KeepAlive = false)]
    public class TextInputDialogViewModel : BindableBase, IDialogAware
    {
        #region Private
        private string _Message;
        private ObservableCollection<ButtonInfo> _Buttons;
        private DelegateCommand<ButtonInfo> _GotResultCommand;
        private ButtonResult _DefaultResult = ButtonResult.None;
        private string _Title = "Pete | Input";
        private string _InputHint = string.Empty;
        private string _InputText = string.Empty;
        private string _InputError;
        private Func<string, string> _ValidationCallback;
        private bool _AllowCancel = true;
        #endregion

        #region Properties
        public event Action<IDialogResult> RequestClose;
        public bool AllowCancel { get => _AllowCancel; private set => SetProperty(ref _AllowCancel, value); }
        public Func<string, string> ValidationCallback { get => _ValidationCallback; private set => SetProperty(ref _ValidationCallback, value); }
        public string Message { get => _Message; private set => SetProperty(ref _Message, value); }
        public string Title { get => _Title; private set => SetProperty(ref _Title, value); }
        public string InputHint { get => _InputHint; private set => SetProperty(ref _InputHint, value); }
        public string InputText { get => _InputText; set => SetProperty(ref _InputText, value, InputTextChanged); }
        public string InputError { get => _InputError; private set => SetProperty(ref _InputError, value); }
        public ReadOnlyObservableCollection<ButtonInfo> Buttons => new ReadOnlyObservableCollection<ButtonInfo>(_Buttons);
        public DelegateCommand<ButtonInfo> GotResultCommand { get => _GotResultCommand; private set => SetProperty(ref _GotResultCommand, value); }
        public ButtonResult DefaultResult { get => _DefaultResult; private set => SetProperty(ref _DefaultResult, value); }
        #endregion
        public TextInputDialogViewModel()
        {
            GotResultCommand = new DelegateCommand<ButtonInfo>(GotResultCallback, CanGetResult).ObservesProperty(() => AllowCancel).ObservesProperty(() => InputError).ObservesProperty(() => ValidationCallback);
            _Buttons = new ObservableCollection<ButtonInfo>();
        }

        #region Events
        private void InputTextChanged() => ValidateInput();
        #endregion

        #region Methods
        private void ValidateInput()
        {
            if (ValidationCallback != null)
                InputError = ValidationCallback(InputText);
        }
        private bool CanGetResult(ButtonInfo info)
        {
            ValidateInput();
            if (InputError != null)
                return (info.IsCancel && AllowCancel);
            return true;
        }
        private void GotResultCallback(ButtonInfo parameter)
        {
            ButtonResult buttonResult = parameter.IsParameterButtonResult ? (ButtonResult)parameter.Parameter : DefaultResult;
            DialogParameters resultParam = new DialogParameters();
            if (!parameter.IsParameterButtonResult)
                resultParam.Add("result", parameter.Parameter ?? parameter.Content);

            if (InputError == null)
                resultParam.Add("input", InputText);

            RequestClose?.Invoke(new DialogResult(buttonResult, resultParam));
        }
        public bool CanCloseDialog() => true;
        public void OnDialogClosed() { }
        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (parameters.TryGetValue("hint", out string hint)) InputHint = hint;
            if (parameters.TryGetValue("input", out string input)) InputText = input;
            if (parameters.TryGetValue("allow-cancel", out bool allowCancel)) AllowCancel = allowCancel;
            if (parameters.TryGetValue("validation", out Func<string, string> validation)) ValidationCallback = validation;
            if (parameters.TryGetValue("message", out string msg)) Message = msg;
            if (parameters.TryGetValue("default", out ButtonResult defResult))
                DefaultResult = defResult;

            if (parameters.TryGetValue("title", out string title))
                Title = $"Pete | {title}";
            else if (msg != null)
                Title = $"Pete | {msg}";

            if (parameters.TryGetValue("buttons", out object buttonsObj) && buttonsObj is IEnumerable<ButtonInfo> buttons)
                _Buttons.AddRange(buttons);

        }
        #endregion
    }
}
