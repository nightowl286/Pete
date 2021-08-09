using System;
using System.Collections.Generic;
using System.Text;
using TNO.BitUtilities;

namespace Pete.Models.Logs
{
    public class EntryLog : LogBase
    {
        #region Fields
        public EntryLogType EntryType;
        public uint EntryId;
        #endregion
        public EntryLog(EntryLogType entryType, DateTime date, uint entryId) : base(LogType.Entry, date)
        {
            EntryId = entryId;
            EntryType = entryType;
        }

        #region Methods
        public override void Save(IAdvancedBitWriter w)
        {
            base.Save(w);
            w.WriteEntryLogType(EntryType);
            w.Write(EntryId);
        }
        #endregion
    }
}
