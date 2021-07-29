using System;
using System.Collections.Generic;
using System.Text;
using Pete.ViewModels;

namespace Pete.Services.Interfaces
{
    public interface IEntryStore
    {
        #region Properties
        public int Count { get; }
        #endregion

        #region Methods
        int CountMissingCategories(out int entriesAffected);
        void PurgeMissingCategories();
        bool IsNew(uint id);
        ReservationToken<uint> GetNewID();
        void FreeToken(ReservationToken<uint> token);
        void AddEntry(ReservationToken<uint> id, uint? category, string title, byte[] data);
        void GetInfo(uint id, out uint? category, out string title);
        byte[] GetData(uint id);
        void UpdateData(uint id, uint? category, string title, byte[] data);
        void RemoveEntry(uint id);
        IEnumerable<EntryPreviewViewModel> GetAll(uint? categoryId, out int count);
        #endregion
    }
}
