using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Pete.ViewModels.Warnings
{
    class WarningFailedLoginGroupViewModel : WarningBaseViewModel
    {
        #region Private
        private ObservableCollection<WarningFailedLoginViewModel> _FailedLoginWarnings = new ObservableCollection<WarningFailedLoginViewModel>();
        #endregion

        #region Properties
        public ReadOnlyObservableCollection<WarningFailedLoginViewModel> FailedLoginWarnings => new ReadOnlyObservableCollection<WarningFailedLoginViewModel>(_FailedLoginWarnings);
        #endregion
        public WarningFailedLoginGroupViewModel(string text, IEnumerable<WarningFailedLoginViewModel> failedLoginWarnings) : base(text)
        {
            _FailedLoginWarnings.AddRange(failedLoginWarnings);
        }
    }
}
