using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace Pete.Services.Interfaces
{
    public interface IEncryptionModule : INotifyPropertyChanged
    {
        #region Properties
        DriveInfo Device2FA { get; set; }
        byte[] DeviceID { get; }
        byte[] DeviceHash { get; }
        bool HasAesKey { get; }
        bool HasFAKey { get; }
        bool HasMasterKey { get; }
        /*byte[] MasterHash { get; }
        byte[] MasterKey { get; }
        byte[] FAKey { get; }
        byte[] FAKeyHash { get; }
        byte[] AesKey { get; }
        byte[] AesSalt { get; }
        string Id2FA { get; }*/
        event Action ReencryptionNeeded;
        #endregion

        #region Methods
        void RegenerateKey();
        bool HasSavedDevice();
        bool HasSavedMaster();
        bool CheckMaster(string master);
        void GenerateAesKey();
        void GenerateMasterKey();
        bool LoadFAKey();
        bool StoreFAKey();
        void Generate2FAKey();
        void SetMaster(string master);
        void LoadAesKey();
        void SaveUsbHash(byte[] hash, byte[] usbID);
        void LoadUsbHash();
        byte[] Encrypt(byte[] data);
        byte[] Decrypt(byte[] data);
        #endregion
    }
}
