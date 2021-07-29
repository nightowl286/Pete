using System;
using System.Collections.Generic;
using System.Linq;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace Pete.ViewModels.Dialogs
{
    public class ConfirmRemoveDialogViewModel : BindableBase, IDialogAware
    {
        #region Private
        private string _ItemType;
        private string _ItemName;
        private string _ExtraText;
        private string _CustomText;
        private DelegateCommand<bool?> _GotResultCommand;
        #endregion

        #region Properties
        public string Title => "Pete | confirm removal";
        public event Action<IDialogResult> RequestClose;
        public string ItemType { get => _ItemType; private set => SetProperty(ref _ItemType, value); }
        public string ItemName { get => _ItemName; private set => SetProperty(ref _ItemName, value); }
        public string ExtraText { get => _ExtraText; private set => SetProperty(ref _ExtraText, value); }
        public string CustomText { get => _CustomText; private set => SetProperty(ref _CustomText, value); }
        public DelegateCommand<bool?> GotResultCommand { get => _GotResultCommand; private set => SetProperty(ref _GotResultCommand, value); }
        #endregion
        public ConfirmRemoveDialogViewModel()
        {
            GotResultCommand = new DelegateCommand<bool?>(GotResult);
        }

        #region Methods
        private void GotResult(bool? result)
        {
            DialogParameters param = new DialogParameters();

            if (result.HasValue)
                param.Add("delete", result.Value);

            DialogResult res = new DialogResult(result == true ? ButtonResult.Yes : ButtonResult.No, param);

            RequestClose?.Invoke(res);

        }
        public bool CanCloseDialog() => true;
        public void OnDialogClosed() { }
        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (parameters.TryGetValue("type", out string type)) ItemType = type;
            if (parameters.TryGetValue("name", out string name)) ItemName = name;
            if (parameters.TryGetValue("extra", out string extra)) ExtraText = extra;
            if (parameters.TryGetValue("custom", out string custom)) CustomText = custom;
        }
        #endregion
    }
}
