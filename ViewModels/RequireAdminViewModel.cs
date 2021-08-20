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
            RestartAsAdminCommand = new DelegateCommand(RestartAsAdmin);
        }

        #region Methods
        private void RestartAsAdmin()
        {
            Debug.WriteLine("Attempting to restart as admin");
            ProcessStartInfo info = new ProcessStartInfo()
            {
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = Process.GetCurrentProcess().MainModule.FileName,
                Verb = "runas"
            };
            try
            {
                Process.Start(info);
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to restart as admin. Ex: {ex.Message}");
            }

        }
        #endregion
    }
}
