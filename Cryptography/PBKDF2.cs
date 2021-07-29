using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Pete.Cryptography
{
    public static class PBKDF2
    {
        #region Variables
        public const int AES_SIZE = 256 / 8;
        #endregion

        #region Functions
        public static byte[] GenerateSalt(int saltSize)
        {
            byte[] salt = new byte[saltSize];
            AES.RNG.GetBytes(salt);
            return salt;
        }
        public static byte[] DeriveSHA256(byte[] key, byte[] salt, int iterations) => Derive(key, salt, iterations, 32, HashAlgorithmName.SHA256);
        public static byte[] Derive(byte[] key, int saltSize, out byte[] salt, int iterations, int length, HashAlgorithmName hash)
        {
            salt = GenerateSalt(saltSize);
            return Derive(key, salt, iterations, length, hash);
        }
        public static byte[] Derive(byte[] key, byte[] salt, int iterations, int length, HashAlgorithmName hash)
        {
            using Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(key, salt, iterations, hash);
            return pbkdf2.GetBytes(length);
        }
        public static byte[] QuickSha512Hash(byte[] data) => SHA512.Create().ComputeHash(data);
        public static bool SameHash(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;
            return true;
        }
        #endregion
    }
}
