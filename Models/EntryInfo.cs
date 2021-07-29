using System;
using System.Collections.Generic;
using System.Text;

namespace Pete.Models
{
    public class EntryInfo
    {
        #region Fields
        public uint ID;
        public string Title;
        public uint? Category;
        public byte[] EncryptedData;
        #endregion
        public EntryInfo(uint id, string title, uint? category, byte[] encryptedData)
        {
            ID = id;
            Title = title;
            Category = category;
            EncryptedData = encryptedData;
        }
    }
}
