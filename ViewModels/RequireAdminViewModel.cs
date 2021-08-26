using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using Prism.Commands;
using Prism.Mvvm;

namespace Pete.ViewModels
{
    public class RequireAdminViewModel : BindableBase
    {
        #region Private
        private DelegateCommand _RestartAsAdminCommand;
        #endregion

        #region Properties
        public DelegateCommand RestartAsAdminCommand { get => _RestartAsAdminCommand; private set => SetProperty(ref _RestartAsAdminCommand, value); }
        #endregion
        public RequireAdminViewModel()
        {
            RestartAsAdminCommand = new DelegateCommand(App.RestartAsAdmin);
        }
    }
}
