using System;
using System.Collections.Generic;
using System.Text;
using Pete.Services.Interfaces;

namespace Pete.Models
{
    public class LogEntryInfo<TType> where TType : Enum
    {
        #region Fields
        public DateTime UtcDate;
        public TType Type;
        public object Extra;
        #endregion
        public LogEntryInfo(DateTime utcDate, TType type, object extra)
        {
            UtcDate = utcDate;
            Type = type;
            Extra = extra;
        }
        public LogEntryInfo(DateTime utcDate, TType type) : this(utcDate, type, null) { }
    }
}
