using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace Pete.Services.Interfaces
{
    public interface ISettings : INotifyPropertyChanged
    {
        #region Consts
        public const int DEF_ITER = 3_000_000;
        public const int DEF_SALT = 10240;
        #endregion

        #region Properties
        int Iterations { get; set; }
        int SaltSize { get; set; }
        IEncryptionModule Encryption { get; set; }
        bool ShowEntryListAtStart { get; set; }
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
