using System;
using System.Collections.Generic;
using System.Text;
using TNO.BitUtilities;

namespace Pete.Models.Logs
{
    public class EntryDeletedLog : LogBase
    {
        #region Fields
        public string EntryName;
        public string CategoryName;
        #endregion
        public EntryDeletedLog(DateTime date, string entryName, string categoryName) : base(LogType.Entry, date)
        {
            EntryName = entryName;
            CategoryName = categoryName;
        }

        #region Methods
        public override void Save(IAdvancedBitWriter w)
        {
            base.Save(w);
            w.WriteEntryLogType(EntryLogType.Delete);

            w.WriteString(EntryName, Encoding.UTF8);
            w.WriteBool(CategoryName != null);
            if (CategoryName != null)
                w.WriteString(CategoryName, Encoding.UTF8);
        }
        #endregion
    }
}
