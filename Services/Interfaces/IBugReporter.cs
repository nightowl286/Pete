using System;
using System.Collections.Generic;
using System.Text;

namespace Pete.Services.Interfaces
{
    public interface IBugReporter
    {
        #region Methods
        string Report(Exception ex, Dictionary<string, string> otherData);
        #endregion
    }
}
