using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pete.ViewModels.Warnings
{
	public class WarningBaseViewModel : BindableBase
	{
        #region Private
        private string _Text;
        #endregion

        #region Properties
        public string Text { get => _Text; private set => SetProperty(ref _Text, value); }
        #endregion
        public WarningBaseViewModel(string text) => Text = text;
	}
}
