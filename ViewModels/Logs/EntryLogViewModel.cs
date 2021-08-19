using System;
using System.Collections.Generic;
using System.Text;
using Pete.Models.Logs;
using Pete.Services.Interfaces;

namespace Pete.ViewModels.Logs
{
    public class EntryLogViewModel : BaseLogViewModel
    {
        #region Private
        private bool _EntryMissing;
        private string _EntryName;
        private string _EntryCategory;
        #endregion

        #region Properties
        public bool EntryMissing { get => _EntryMissing; private set => SetProperty(ref _EntryMissing, value); }
        public string EntryName { get => _EntryName; private set => SetProperty(ref _EntryName, value); }
        public string EntryCategory { get => _EntryCategory; private set => SetProperty(ref _EntryCategory, value); }
        #endregion
        public EntryLogViewModel(uint? entryID, IEntryStore entryStore, ICategoryStore categoryStore, DateTime date, string text) : base(date, text)
        {
            EntryMissing = !entryID.HasValue;

            if (entryID.HasValue)
            {
                entryStore.GetInfo(entryID.Value, out uint? category, out string name);
                EntryName = name;
                if (category.HasValue)
                    EntryCategory = categoryStore.GetCategory(category.Value).Name;
            }
            else
                EntryName = "<missing>";
        }
    }
}
