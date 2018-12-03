﻿using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Collections.Generic;

namespace Crypto_Notepad
{
    /// <summary>
    /// This class extracts information needed for decryption from a .cnp file
    /// </summary>
    class AESMetadata
    {
        /// <summary>
        /// Offset to actual AES data; size of metadata
        /// </summary>
        public int OffsetToData { get; private set; }
        public byte[] InitialVector { get; private set; }
        public byte[] Salt { get; private set; }

        public AESMetadata()
        {
            this.InitialVector = new byte[16];
            this.Salt = null;
        }

        public void DeleteMetadataFromBuffer(ref byte[] rawData)
        {
            byte[] buffer = new byte[rawData.Length - this.OffsetToData];
            System.Buffer.BlockCopy(rawData, this.OffsetToData, buffer, 0, rawData.Length - this.OffsetToData);
            rawData = buffer;
        }

        private bool ReadData(byte[] rawData, int offset, ref byte[] dataOut)
        {
            // Buffer to store bytes
            List<byte> buffer = new List<byte>();
            const byte nullTerminator = 0;
            bool foundData = false;

            // Push data to buffer
            for (int i = offset; i < rawData.Length; i++) {
                if (rawData[i] == nullTerminator) { foundData = true; break; }
                else { buffer.Add(rawData[i]); }
            }

            if (foundData == true)
            {
                dataOut = buffer.ToArray();
                return true;
            }
            return false;
        }

        public bool GetMetadata(byte[] rawData)
        {
            int offset = 0;
            byte[] buffer = null;

            if (!this.ReadData(rawData, 0, ref buffer)) { return false; }
            this.InitialVector = buffer;
            offset += buffer.Length + 1;

            if (!this.ReadData(rawData, offset, ref buffer)) { return false; }
            this.Salt = buffer;
            offset += buffer.Length + 1;

            this.OffsetToData = offset;

            return true;
        }
    }

    class AES
    {
        public static string Encrypt(string plainText, string password,
        string salt = null, string hashAlgorithm = "SHA1",
        int passwordIterations = 2, int keySize = 256)
        {
            if (string.IsNullOrEmpty(plainText))
                return "";

            byte[] plainTextBytes;
            byte[] saltValueBytes;

            // In case user wants a random salt or salt is null/empty for some other reason
            if (string.IsNullOrEmpty(salt))
            {
                saltValueBytes = new byte[64]; // Nice and long
                RandomNumberGenerator rng = RandomNumberGenerator.Create();
                rng.GetNonZeroBytes(saltValueBytes);
                rng.Dispose();
            }
            else
            {
                saltValueBytes = Encoding.ASCII.GetBytes(salt);
            }

            plainTextBytes = Encoding.UTF8.GetBytes(plainText);

            PasswordDeriveBytes derivedPassword = new PasswordDeriveBytes
             (password, saltValueBytes, hashAlgorithm, passwordIterations);

            // Null password; adds *some* memory dump protection
            password = null;

            byte[] keyBytes = derivedPassword.GetBytes(keySize / 8);
            RijndaelManaged symmetricKey = new RijndaelManaged();
            symmetricKey.Mode = CipherMode.CBC;
            symmetricKey.GenerateIV();

            byte[] cipherTextBytes = null;

            using (MemoryStream memStream = new MemoryStream())
            {
                byte[] nullByte = { 0 };
                memStream.Write(symmetricKey.IV, 0, symmetricKey.IV.Length);
                memStream.Write(nullByte, 0, 1);
                memStream.Write(saltValueBytes, 0, saltValueBytes.Length);
                memStream.Write(nullByte, 0, 1);

                using (ICryptoTransform encryptor = symmetricKey.CreateEncryptor
                (keyBytes, symmetricKey.IV))
                {
                    using (CryptoStream cryptoStream = new CryptoStream
                             (memStream, encryptor, CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                        cryptoStream.FlushFinalBlock();
                        cipherTextBytes = memStream.ToArray();
                        memStream.Close();
                        cryptoStream.Close();
                    }
                }
            }

            symmetricKey.Dispose();
            return Convert.ToBase64String(cipherTextBytes);
        }

        public static string Decrypt(string cipherText, string password, string salt = "Kosher",
        string hashAlgorithm = "SHA1",
        int passwordIterations = 2,
        int keySize = 256)
        {
            if (string.IsNullOrEmpty(cipherText))
                return null;

            byte[] initialVectorBytes;
            byte[] saltValueBytes;
            byte[] cipherTextBytes = Convert.FromBase64String(cipherText);

            // Extract metadata from file
            AESMetadata metadata = new AESMetadata();
            if (!metadata.GetMetadata(cipherTextBytes))
            {
                // Metadata parsing error
                DialogResult result = MessageBox.Show("Unable to parse file metadata.\nAttempt to open anyway?\n(May result in a \'Incorrect Key\' error if the salt is wrong.)",
                "Missing or Corrupted Metadata", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Asterisk);
                if (result == DialogResult.Yes)
                {
                    // Default initialization vector from builds v1.1.2 and older
                    const string default_IV = "16CHARSLONG12345";

                    initialVectorBytes = Encoding.ASCII.GetBytes(default_IV);
                    saltValueBytes = Encoding.ASCII.GetBytes(salt);
                }
                else { return null; }
            }
            else
            {
                saltValueBytes = metadata.Salt;
                initialVectorBytes = metadata.InitialVector;
                metadata.DeleteMetadataFromBuffer(ref cipherTextBytes);
            }

            PasswordDeriveBytes derivedPassword = new PasswordDeriveBytes
                (password, saltValueBytes, hashAlgorithm, passwordIterations);
            byte[] keyBytes = derivedPassword.GetBytes(keySize / 8);

            RijndaelManaged symmetricKey = new RijndaelManaged();
            symmetricKey.Mode = CipherMode.CBC;

            byte[] plainTextBytes = new byte[cipherTextBytes.Length];
            int byteCount = 0;

            using (MemoryStream memStream = new MemoryStream(cipherTextBytes))
            {
                using (ICryptoTransform decryptor = symmetricKey.CreateDecryptor
                         (keyBytes, initialVectorBytes))
                {
                    using (CryptoStream cryptoStream
                    = new CryptoStream(memStream, decryptor, CryptoStreamMode.Read))
                    {
                        byteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                        memStream.Close();
                        cryptoStream.Close();
                    }
                }

                symmetricKey.Dispose();
            }
            
            return Encoding.UTF8.GetString(plainTextBytes, 0, byteCount);
        }
    }
}
