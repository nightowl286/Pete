using System;
using System.Collections.Generic;
using System.Text;

namespace Pete.ViewModels.Warnings
{
    public class WarningFailedLoginViewModel : WarningBaseViewModel
    {
        #region Private
        private DateTime _Date;
        #endregion

        #region Properties
        public DateTime Date { get => _Date; private set => SetProperty(ref _Date, value); }
        #endregion
        public WarningFailedLoginViewModel(string text, DateTime date) : base(text) => Date = date;
    }
}
