using System;
using System.Collections.Generic;
using System.Text;
using TNO.BitUtilities;

namespace Pete.Models.Logs
{
    public class TamperLog : LogBase
    {
        #region Fields
        public TamperType TamperType;
        #endregion
        public TamperLog(TamperType tamperType, DateTime date) : base(LogType.TamperAttempt, date)
        {
            TamperType = tamperType;
        }

        #region Methods
        public override void Save(IAdvancedBitWriter w)
        {
            base.Save(w);
            w.WriteTamperType(TamperType);
        }
        #endregion
    }
}
