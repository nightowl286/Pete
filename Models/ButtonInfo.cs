using System;
using System.Collections.Generic;
using System.Text;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace Pete.Models
{

    public enum ButtonType
    {
        Normal,
        Primary,
        Cancel,
    }
    public class ButtonInfo : BindableBase
    {
        #region Static
        public static readonly ButtonInfo Cancel = new ButtonInfo(ButtonType.Cancel, "cancel", ButtonResult.Cancel);
        public static readonly ButtonInfo No = new ButtonInfo(ButtonType.Cancel, "no", ButtonResult.No);
        public static readonly ButtonInfo Yes = new ButtonInfo(ButtonType.Primary, "yes", ButtonResult.Yes);
        public static readonly ButtonInfo Ok = new ButtonInfo(ButtonType.Primary, "ok", ButtonResult.OK);
        #endregion

        #region Private
        private ButtonType _Type = ButtonType.Normal;
        private object _Content;
        private object _Parameter;
        private bool _IsParameterButtonResult;
        private bool _IsCancel;
        private bool _IsPrimary;
        #endregion

        #region Properties
        public ButtonType Type { get => _Type; private set => SetProperty(ref _Type, value); }
        public bool IsCancel { get => _IsCancel; private set => SetProperty(ref _IsCancel, value); }
        public bool IsPrimary { get => _IsPrimary; private set => SetProperty(ref _IsPrimary, value); }
        public object Content { get => _Content; private set => SetProperty(ref _Content, value); }
        public object Parameter { get => _Parameter; private set => SetProperty(ref _Parameter, value); }
        public bool IsParameterButtonResult { get => _IsParameterButtonResult; private set => SetProperty(ref _IsParameterButtonResult, value); }
        #endregion
        public ButtonInfo(ButtonType type, object content, object parameter)
        {
            Type = type;
            Content = content;
            Parameter = parameter;

            IsCancel = type == ButtonType.Cancel;
            IsPrimary = type == ButtonType.Primary;

            IsParameterButtonResult = (parameter is ButtonResult);
        }
        public ButtonInfo(object content, object parameter) : this(ButtonType.Normal, content, parameter) { }
        public ButtonInfo(object content) : this(content, content) { }

    }
}
