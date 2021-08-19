using System;
using System.Collections.Generic;
using System.Linq;
using Pete.Models.Logs;
using Prism.Commands;
using Prism.Mvvm;

namespace Pete.ViewModels.Logs
{
    public class BaseLogViewModel : BindableBase
    {
        #region Private
        private DateTime _Date;
        private string _Text;
        #endregion

        #region Properties
        public DateTime Date { get => _Date; protected set => SetProperty(ref _Date, value); }
        public string Text { get => _Text; protected set => SetProperty(ref _Text, value); }
        #endregion
        public BaseLogViewModel(DateTime date, string text)
        {
            Date = date;
            Text = text;
        }
    }
}
