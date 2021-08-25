using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace Pete.Services.Interfaces
{
    public interface ISettings : INotifyPropertyChanged
    {
        #region Properties
        int Iterations { get; }
        int SaltSize { get; }
        IEncryptionModule Encryption { get; set; }
        bool ShowEntryListAtStart { get; }
        ObservableCollection<bool> LogFilters { get; }
        ObservableCollection<bool> LogEntryFilters { get; }
        #endregion

        #region Methods
        void Load();
        void Save();
        void RestoreDefault();
        #endregion
    }
}
