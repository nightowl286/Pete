using System;
using System.Collections.Generic;
using System.Text;
using Pete.Services.Interfaces;

namespace Pete.Models.Warnings
{
    public class WarningFailedLoginGroup : WarningBase
    {
        #region Fields
        public WarningFailedLogin[] Attempts;
        #endregion
        public WarningFailedLoginGroup(WarningFailedLogin[] attempts)
        {
            Attempts = attempts;
        }
    }
}
