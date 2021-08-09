using System;
using System.Collections.Generic;
using System.Text;
using TNO.BitUtilities;
using Pete;

namespace Pete.Models.Logs
{

    public class LogBase
    {
        #region Fields
        public LogType Type;
        public DateTime Date;
        #endregion
        public LogBase(LogType type, DateTime date)
        {
            Type = type;
            Date = date;
        }

        #region Methods
        public virtual void Save(IAdvancedBitWriter w)
        {
            w.WriteLogType(Type);
            w.Write(Date);
        }
        #endregion
    }
}
