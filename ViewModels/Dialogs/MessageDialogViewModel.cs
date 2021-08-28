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
    public class MessageDialogViewModel : BindableBase, IDialogAware
    {
        #region Private
        private string _Message;
        private ObservableCollection<ButtonInfo> _Buttons;
        private DelegateCommand<ButtonInfo> _GotResultCommand;
        private ButtonResult _DefaultResult = ButtonResult.None;
        private string _Title = "Pete | Message";
        #endregion

        #region Properties
        public event Action<IDialogResult> RequestClose;
        public string Message { get => _Message; private set => SetProperty(ref _Message, value); }
        public string Title { get => _Title; private set => SetProperty(ref _Title, value); }
        public ReadOnlyObservableCollection<ButtonInfo> Buttons => new ReadOnlyObservableCollection<ButtonInfo>(_Buttons);
        public DelegateCommand<ButtonInfo> GotResultCommand { get => _GotResultCommand; private set => SetProperty(ref _GotResultCommand, value); }
        public ButtonResult DefaultResult { get => _DefaultResult; private set => SetProperty(ref _DefaultResult, value); }
        #endregion
        public MessageDialogViewModel()
        {
            GotResultCommand = new DelegateCommand<ButtonInfo>(GotResultCallback);
            _Buttons = new ObservableCollection<ButtonInfo>();
        }


        #region Methods
        private void GotResultCallback(ButtonInfo parameter)
        {
            ButtonResult buttonResult = parameter.IsParameterButtonResult ? (ButtonResult)parameter.Parameter : DefaultResult;
            DialogParameters resultParam = new DialogParameters();
            if (!parameter.IsParameterButtonResult)
                resultParam.Add("result", parameter.Parameter ?? parameter.Content);

            RequestClose?.Invoke(new DialogResult(buttonResult, resultParam));
        }
        public bool CanCloseDialog() => true;
        public void OnDialogClosed() {}
        public void OnDialogOpened(IDialogParameters parameters)
        {
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
