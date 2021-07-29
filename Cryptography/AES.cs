using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Pete.Cryptography
{
    public static class AES
    {
        #region Variables
        internal static readonly RandomNumberGenerator RNG = RandomNumberGenerator.Create();

        #endregion

        #region Functions
        #region Encrypt
        public static byte[] Encrypt(byte[] data, byte[] iv, byte[] key)
        {
            byte[] encrypted;
            using (AesManaged aes = new AesManaged())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                aes.Key = key;
                aes.IV = iv;
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using MemoryStream mem = new MemoryStream();
                using (CryptoStream crypto = new CryptoStream(mem, encryptor, CryptoStreamMode.Write))
                {
                    crypto.Write(data, 0, data.Length);
                }
                encrypted = mem.ToArray();
            }
            return encrypted;
        }
        public static byte[] Encrypt(byte[] data, byte[] key)
        {
            byte[] iv = new byte[16];
            RNG.GetBytes(iv);
            byte[] encrypted = Encrypt(data, iv, key);
            Array.Resize(ref encrypted, encrypted.Length + iv.Length);
            Array.Copy(iv, 0, encrypted, encrypted.Length - iv.Length, iv.Length);
            return encrypted;
        }
        #endregion
        #region Decrypt
        public static byte[] Decrypt(byte[] encrypted, byte[] iv, byte[] key)
        {
            using AesManaged aes = new AesManaged
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,

                Key = key,
                IV = iv
            };

            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using MemoryStream mem = new MemoryStream(encrypted);
            using MemoryStream decryptMem = new MemoryStream();
            using CryptoStream crypto = new CryptoStream(mem, decryptor, CryptoStreamMode.Read);
            byte[] buff = new byte[32 * 1024]; // 32 kb
            int read;
            do
            {
                read = crypto.Read(buff, 0, buff.Length);
                decryptMem.Write(buff, 0, read);
            }
            while (read == buff.Length); // only read partial buffer if there is nothing left to read
            return decryptMem.ToArray();
        }
        public static byte[] Decrypt(byte[] encrypted, byte[] key)
        {
            byte[] iv = new byte[16];
            Array.Copy(encrypted, encrypted.Length - iv.Length, iv, 0, iv.Length);
            Array.Resize(ref encrypted, encrypted.Length - iv.Length);
            return Decrypt(encrypted, iv, key);
        }
        #endregion
        #endregion
    }
}
