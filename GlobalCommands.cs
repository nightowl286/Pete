using System;
using System.Collections.Generic;
using System.Text;
using Prism.Commands;

namespace Pete
{
    public static class GlobalCommands
    {
        #region Commands
        public static DelegateCommand ExitCommand { get; set; }
        public static DelegateCommand<string> OpenUrlCommand { get; set; }
        public static DelegateCommand<string> CopyTextToClipboardCommand { get; set; }
        #endregion
    }
}
