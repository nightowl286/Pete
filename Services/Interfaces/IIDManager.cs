using System;
using System.Collections.Generic;
using System.Text;

namespace Pete.Services.Interfaces
{
    public class ReservationToken<T>
    {
        #region Properties
        private readonly object SourceRef;
        public T Item { get; private set; }
        #endregion
        public ReservationToken(object sourceRef, T item)
        {
            SourceRef = sourceRef;
            Item = item;
        }

        #region Methods
        public bool IsFromSource(object source) => SourceRef == source;
        #endregion
    }

    public interface IIDManager
    {
        #region Methods
        void Clear();
        void Reset(IEnumerable<uint> ids);
        ReservationToken<uint> ReserveNew();
        void Take(ReservationToken<uint> token);
        void Free(ReservationToken<uint> token);
        void Free(uint id);
        void CleanReservations();
        void CleanFree();
        bool IsReserved(uint id);
        void Claim(IEnumerable<uint> ids);
        #endregion
    }
}
