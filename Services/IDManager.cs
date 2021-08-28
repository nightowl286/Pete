using System;
using System.Collections.Generic;
using System.Text;
using Pete.Services.Interfaces;
using System.Linq;

namespace Pete.Services
{
    public class IDManager : IIDManager
    {
        #region Fields
        private object ReservationRef = new object();
        private uint NextID = 0;
        private HashSet<uint> FreedIDs = new HashSet<uint>();
        private Dictionary<uint, WeakReference<ReservationToken<uint>>> Reserved = new Dictionary<uint, WeakReference<ReservationToken<uint>>>();
        #endregion
        public IDManager() { }
        public IDManager(IEnumerable<uint> takenIDs) => Claim(takenIDs);

        #region Methods
        public void Clear()
        {
            ReservationRef = new object();
            NextID = 0;
            FreedIDs.Clear();
            Reserved.Clear();
        }
        public void Reset(IEnumerable<uint> takenIDs)
        {
            Clear();
            Claim(takenIDs);
        }
        public ReservationToken<uint> ReserveNew()
        {
            uint id = GetNew();
            ReservationToken<uint> token = new ReservationToken<uint>(ReservationRef, id);
            Reserved.Add(id, new WeakReference<ReservationToken<uint>>(token));
            return token;
        }
        public void Take(ReservationToken<uint> token)
        {
            CheckToken(token);
            Reserved.Remove(token.Item);
            CleanReservations();
        }
        public void Free(ReservationToken<uint> token)
        {
            Take(token);
            Free(token.Item);
        }
        public void CleanReservations()
        {
            foreach (var pair in Reserved)
            {
                if (!pair.Value.TryGetTarget(out _))
                {
                    Reserved.Remove(pair.Key);
                    Free(pair.Key);
                }
            }
        }
        public void CleanFree()
        {
            while (FreedIDs.Contains(NextID - 1))
                FreedIDs.Remove(--NextID);
        }
        private void CheckToken(ReservationToken<uint> token)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));
            if (!Reserved.ContainsKey(token.Item) || !token.IsFromSource(ReservationRef))
                throw new ArgumentException("This token was not given out by this instance of the id manager.", nameof(token));
        }
        private uint GetNew()
        {
            if (FreedIDs.Count > 0)
            {
                uint id = FreedIDs.First();
                FreedIDs.Remove(id);
                return id;
            }
            return NextID++;
        }
        public void Free(uint id)
        {
            if (id >= NextID || FreedIDs.Contains(id)) throw new ArgumentException($"The ID '{id}' has not been taken by this manager.", nameof(id));

            if (id == NextID - 1)
                NextID--;
            else
                FreedIDs.Add(id);

            CleanFree();
        }
        public bool IsReserved(uint id) => Reserved.ContainsKey(id);
        public void Claim(IEnumerable<uint> ids)
        {
            ids = ids.OrderBy(n => n);
            uint counter = 0;
            foreach (uint id in ids)
            {
                while (counter < id)
                {
                    FreedIDs.Add(counter++);
                    NextID++;
                }

                if (id == NextID)
                    NextID++;
                else
                {

                    FreedIDs.Add(id);
                    NextID = id + 1;

                }
                counter++;
            }
        }
        #endregion
    }
}
