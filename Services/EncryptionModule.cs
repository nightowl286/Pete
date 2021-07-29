using System;
using System.Collections.Generic;
using System.Text;
using Pete.Services.Interfaces;
using TNO.Pete.E2fa;
using Pete.Cryptography;
using Prism.Mvvm;
using System.IO;
using System.Security.Cryptography;
using TNO.BitUtilities;
using TNO.Pete.E2fa.UsbHasher;
using System.Diagnostics;

namespace Pete.Services
{
    public class EncryptionModule : BindableBase, IEncryptionModule
    {
        #region Private
        private const string PATH_DATA = "data";
        private const string PATH_HASH_USB = PATH_DATA + "\\usb.hash";
        private const string PATH_ID_2FA = PATH_DATA + "\\id.hash";
        private const string PATH_HASH_2FA = PATH_DATA + "\\2fa.hash";
        private const string PATH_HASH_MASTER = PATH_DATA + "\\master.hash";
        private const string PATH_KEY = PATH_DATA + "\\key.info";
        private const string PATH_DEBUG = PATH_DATA + "\\debug.info";
        private byte[] _MasterHash;
        private byte[] _MasterKey;
        private byte[] _MasterData;
        private byte[] _MasterSalt;
        private byte[] _FAKey;
        private byte[] _FAKeyHash;
        private byte[] _AesKey;
        private byte[] _AesSalt;
        private string _Id2FA;
        private DriveInfo _Device2FA;
        private byte[] _DeviceID;
        private byte[] _DeviceHash;
        private readonly ISettings _Settings;
        private int _LastIterSize;
        #endregion

        #region Properties
        /*public byte[] FAKey { get => _FAKey; private set => SetProperty(ref _FAKey, value); }
        public byte[] AesKey { get => _AesKey; private set => SetProperty(ref _AesKey, value); }
        public byte[] AesSalt { get => _AesSalt; private set => SetProperty(ref _AesSalt, value); }
        public byte[] FAKeyHash { get => _FAKeyHash; private set => SetProperty(ref _FAKeyHash, value); }
        public byte[] MasterHash { get => _MasterHash; private set => SetProperty(ref _MasterHash, value); }
        public byte[] MasterKey { get => _MasterKey; private set => SetProperty(ref _MasterKey, value); }
        public byte[] MasterSalt { get => _MasterSalt; private set => SetProperty(ref _MasterSalt, value); }
        public string Id2FA { get => _Id2FA; private set => SetProperty(ref _Id2FA, value); }*/
        public byte[] DeviceID { get => _DeviceID; private set => SetProperty(ref _DeviceID, value); }
        public byte[] DeviceHash { get => _DeviceHash; private set => SetProperty(ref _DeviceHash, value); }
        public DriveInfo Device2FA { get => _Device2FA; set => SetProperty(ref _Device2FA, value); }
        public bool HasAesKey => _AesKey != null;
        public bool HasFAKey => _FAKey != null;
        public bool HasMasterKey => _MasterKey != null;
        #endregion
        public EncryptionModule(ISettings settings)
        {
            _Settings = settings;
        }

        #region Methods
#if DEBUG
        public bool LoadDebug()
        {
            if (!File.Exists(PATH_DEBUG))
            {
                Debug.WriteLine($"[EncryptionModule] debug data not found");
                return false;
            }
            using (FileStream fs = new FileStream(PATH_DEBUG, FileMode.Open))
            {
                AdvancedBitReader r = new AdvancedBitReader(fs);

                _DeviceID = r.ReadBytes(256);
                _DeviceHash = r.ReadBytes(256);
                _AesKey = r.ReadBytes(256);
                _MasterKey = r.ReadBytes(256);
                _FAKey = r.ReadBytes(256);
                Device2FA = new DriveInfo(r.ReadString(Encoding.UTF8));

                Debug.WriteLine($"[EncryptionModule] debug data loaded");
                return true;
            }
        }
        public void SaveDebug()
        {
            EnsurePath();
            using (FileStream fs = new FileStream(PATH_DEBUG, FileMode.OpenOrCreate))
            {
                AdvancedBitWriter w = new AdvancedBitWriter(fs);

                w.WriteBytes(_DeviceID);
                w.WriteBytes(_DeviceHash);
                w.WriteBytes(_AesKey);
                w.WriteBytes(_MasterKey);
                w.WriteBytes(_FAKey);
                w.WriteString(Device2FA.Name, Encoding.UTF8);

                w.Flush();
                w.Dispose();
            }
        }
#endif
        public bool HasSavedDevice() => File.Exists(PATH_HASH_USB);
        public bool HasSavedMaster() => File.Exists(PATH_HASH_MASTER);
        public bool CheckMaster(string master)
        {
            Debug.Assert(File.Exists(PATH_HASH_MASTER), $"The hash file '{PATH_HASH_MASTER}' was not found, this should not have been called");

            _MasterData = Encoding.UTF8.GetBytes(master);

            byte[] savedSalt, savedHash;
            int saltSize, iter;

            using (FileStream fs = new FileStream(PATH_HASH_MASTER, FileMode.Open))
            {
                AdvancedBitReader r = new AdvancedBitReader(fs);
                saltSize = r.Read<int>();
                iter = r.Read<int>();

                savedSalt = r.ReadBytes((ulong)saltSize * 8UL);
                savedHash = r.ReadBytes(512);
            }

            byte[] generatedKey = PBKDF2.Derive(_MasterData, savedSalt, iter, 32, HashAlgorithmName.SHA512);
            byte[] generatedHash = PBKDF2.QuickSha512Hash(generatedKey);

            if (PBKDF2.SameHash(savedHash, generatedHash))
            {
                _MasterSalt = savedSalt;
                _LastIterSize = iter;
                _MasterKey = generatedKey;
                _MasterHash = generatedHash;

                return true;
            }

            return false;

        }
        public void GenerateMasterKey()
        {
            EnsurePath();

            int saltSize = _Settings.SaltSize;
            int iter = _Settings.Iterations;

            _MasterSalt = PBKDF2.GenerateSalt(saltSize);
            _MasterKey = PBKDF2.Derive(_MasterData, _MasterSalt, iter, 32, HashAlgorithmName.SHA512);
            _MasterHash = PBKDF2.QuickSha512Hash(_MasterKey);

            using (FileStream fs = new FileStream(PATH_HASH_MASTER, FileMode.OpenOrCreate))
            {
                AdvancedBitWriter w = new AdvancedBitWriter(fs);
                w.Write(saltSize);
                w.Write(iter);
                w.WriteBytes(_MasterSalt);
                w.WriteBytes(_MasterHash);

                w.Flush();
                w.Dispose();
            }
        }
        public bool LoadFAKey()
        {
            byte[] encryptedID = File.ReadAllBytes(PATH_ID_2FA);
            byte[] decryptedID = AES.Decrypt(encryptedID, _MasterKey);

            _Id2FA = Encoding.UTF8.GetString(decryptedID);

            try
            {

                byte[] encryptedKey = PeteE2fa.Request(_Device2FA, _Id2FA);
                if (encryptedKey == null) return false;

                _FAKey = AES.Decrypt(encryptedKey, _MasterKey);


                return true;

            }
            catch { return false; }
        }
        public bool StoreFAKey()
        {
            byte[] keyToStore = AES.Encrypt(_FAKey, _MasterKey);

            try
            {
                _Id2FA = PeteE2fa.Create(_Device2FA, keyToStore);
                if (_Id2FA != null)
                {
                    byte[] id = Encoding.UTF8.GetBytes(_Id2FA);

                    byte[] encryptedId = AES.Encrypt(id, _MasterKey);

                    File.WriteAllBytes(PATH_ID_2FA, encryptedId);
                }
                return _Id2FA != null;
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"Failed to store fa key [{ex.GetType().Name}] {ex.Message}");
                return false;
            }
        }
        public void LoadAesKey()
        {
            byte[] key = new byte[_MasterKey.Length + _FAKey.Length];
            Array.Copy(_MasterKey, key, _MasterKey.Length);
            Array.Copy(_FAKey, 0, key, _MasterKey.Length, _FAKey.Length);

            byte[] encryptedData = File.ReadAllBytes(PATH_KEY);
            byte[] data = AES.Decrypt(encryptedData, _MasterKey);

            using (MemoryStream ms = new MemoryStream(data))
            {
                AdvancedBitReader r = new AdvancedBitReader(ms);

                int saltSize = r.Read<int>();
                int iter = r.Read<int>();
                _AesSalt = r.ReadBytes((ulong)saltSize * 8UL);

                _AesKey = PBKDF2.Derive(key, _AesSalt, iter, 32, HashAlgorithmName.SHA512);
                
            }

        }
        public void GenerateAesKey()
        {
            byte[] key = new byte[_MasterKey.Length + _FAKey.Length];
            Array.Copy(_MasterKey, key, _MasterKey.Length);
            Array.Copy(_FAKey, 0, key, _MasterKey.Length, _FAKey.Length);

            int saltSize = _Settings.SaltSize;
            int iter = _Settings.Iterations;

            _AesSalt = PBKDF2.GenerateSalt(saltSize);

            _AesKey = PBKDF2.Derive(key, _AesSalt, iter, 32, HashAlgorithmName.SHA512);

            EnsurePath();
            using (MemoryStream ms = new MemoryStream())
            {
                AdvancedBitWriter w = new AdvancedBitWriter(ms);
                w.Write(saltSize);
                w.Write(iter);
                w.WriteBytes(_AesSalt);

                w.Flush();
                w.Dispose();

                byte[] data = ms.ToArray();
                byte[] encrypted = AES.Encrypt(data, _MasterKey);

                File.WriteAllBytes(PATH_KEY, encrypted);
                
            }
        }
        public void Generate2FAKey()
        {
            int saltSize = _Settings.SaltSize;
            int iter = _Settings.Iterations;

            _FAKey = PBKDF2.Derive(PBKDF2.GenerateSalt(32), saltSize, out byte[] salt, iter, 32, HashAlgorithmName.SHA512);

            _FAKeyHash = PBKDF2.QuickSha512Hash(_FAKey);

            EnsurePath();
            using (FileStream fs = new FileStream(PATH_HASH_2FA, FileMode.OpenOrCreate))
            {
                AdvancedBitWriter w = new AdvancedBitWriter(fs);
                w.Write(saltSize);
                w.Write(iter);
                w.WriteBytes(salt);
                w.WriteBytes(_FAKeyHash);

                w.Flush();
                w.Dispose();
            }
        }
        public void SetMaster(string master)
        {
            _MasterData = Encoding.UTF8.GetBytes(master);
        }
        public void SaveUsbHash(byte[] hash, byte[] usbID)
        {
            DeviceHash = hash;
            DeviceID = usbID;

            AdvancedBitWriter w = new AdvancedBitWriter();

            w.Write(hash.Length);
            w.WriteBytes(hash);

            w.Write(usbID.Length);
            w.WriteBytes(usbID);

            w.Flush();

            byte[] data = w.ToArray();
            w.Dispose();

            byte[] encrypted = AES.Encrypt(data, _MasterKey);

            EnsurePath();
            File.WriteAllBytes(PATH_HASH_USB, encrypted);

        }
        private void EnsurePath()
        {
            if (!Directory.Exists(PATH_DATA))
                Directory.CreateDirectory(PATH_DATA);
        }
        public void LoadUsbHash()
        {
            EnsurePath();
            if (File.Exists(PATH_HASH_USB))
            {
                byte[] data = File.ReadAllBytes(PATH_HASH_USB);
                byte[] decrypted = AES.Decrypt(data, _MasterKey);

                AdvancedBitReader r = new AdvancedBitReader();
                r.FromArray(decrypted);

                int hashLen = r.Read<int>();
                DeviceHash = r.ReadBytes((ulong)hashLen * 8UL);

                int idLen = r.Read<int>();
                DeviceID = r.ReadBytes((ulong)idLen * 8UL);
            }
        }
        public byte[] Encrypt(byte[] data) => AES.Encrypt(data, _AesKey);
        public byte[] Decrypt(byte[] data) => AES.Decrypt(data, _AesKey);
        #endregion
    }
}
