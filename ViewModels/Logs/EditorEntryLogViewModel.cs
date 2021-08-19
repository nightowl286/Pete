using System;
using System.Collections.Generic;
using System.Linq;
using Pete.Models.Logs;
using Prism.Commands;
using Prism.Mvvm;

namespace Pete.ViewModels.Logs
{
    public class EditorEntryLogViewModel : BindableBase
    {
        #region Private
        private EntryLogType _Type;
        private DateTime _Date;
        private string _Icon;
        #endregion

        #region Properties
        public EntryLogType Type { get => _Type; private set => SetProperty(ref _Type, value); }
        public DateTime Date { get => _Date; private set => SetProperty(ref _Date, value); }
        public string Icon { get => _Icon; private set => SetProperty(ref _Icon, value); }
        #endregion
        public EditorEntryLogViewModel(EntryLog log)
        {
            Type = log.EntryType;
            Date = log.Date;
            string iconKey = Type == EntryLogType.View ? "Text.Eye" : Type == EntryLogType.Edit ? "Text.Wrench" : "Text.Pen";
            Icon = App.Current.FindResource(iconKey) as string;
        }
    }
}
