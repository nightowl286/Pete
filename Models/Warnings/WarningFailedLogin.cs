using System;
using System.Collections.Generic;
using System.Text;
using Pete.Services.Interfaces;

namespace Pete.Models.Warnings
{
    public class WarningFailedLogin : WarningBase
    {
        #region Fields
        public DateTime At;
        #endregion
        public WarningFailedLogin(DateTime at) => At = at;
    }
}
