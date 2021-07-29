using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using Pete.Models;

namespace Pete.Services.Interfaces
{
    public enum EntryLogType : byte
    {
        View,
        Edit,
        Create,
        Delete,
    }

    public interface IActivityLog : INotifyPropertyChanged
    {
        #region Methods
        IEnumerable<LogEntryInfo<EntryLogType>> GetAll(uint entryId);
        DateTime? GetLast(uint entryId, EntryLogType type);
        void GetLastAll(uint entryId, out DateTime? view, out DateTime? edit, out DateTime? create);
        void GetLastAll(uint entryId, out DateTime? view, out DateTime? edit);
        DateTime Log(uint entryId, EntryLogType type);
        #endregion
    }
}
