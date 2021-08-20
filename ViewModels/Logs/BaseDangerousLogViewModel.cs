using System;
using System.Collections.Generic;
using System.Text;

namespace Pete.ViewModels.Logs
{
    public class BaseDangerousLogViewModel : BaseLogViewModel
    {
        public BaseDangerousLogViewModel(DateTime date, string text) : base(date, text) { }
    }
}
