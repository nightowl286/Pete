using System;
using System.Collections.Generic;
using System.Text;

namespace Pete.Models.Logs
{
    public class CleanupLog : LogBase
    {
        #region Fields
        public int LogsRemoved;
        public DateTime From;
        #endregion
        public CleanupLog(int logsRemoved, DateTime from, DateTime date) : base(LogType.Cleanup, date)
        {
            LogsRemoved = logsRemoved;
            From = from;
        }
    }
}
