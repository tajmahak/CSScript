using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

// utils.crypto
// КРИПТОГРАФИЯ
// ------------------------------------------------------------

//## #namespace

// Утилиты для работы с криптографией
public static class CryptoUtils
{
    // Выполняет симметричное шифрование с помощью алгоритма AES с использованием ключа (128/192/256 бит)
    public static byte[] EncryptAES(byte[] data, byte[] key) {
        using (Aes aes = Aes.Create()) {
            aes.KeySize = key.Length * 8;
            aes.BlockSize = 128;
            aes.Padding = PaddingMode.Zeros;

            aes.Key = key;
            aes.GenerateIV();

            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            using (ICryptoTransform encryptor = aes.CreateEncryptor()) {
                writer.Write(data.Length);
                writer.Write(aes.IV);
                writer.Write(__PerformCryptography(data, encryptor));
                return ms.ToArray();
            }
        }
    }

    // Выполняет симметричное дешифрование с помощью алгоритма AES с использованием ключа (128/192/256 бит)
    public static byte[] DecryptAES(byte[] data, byte[] key) {
        using (Aes aes = Aes.Create()) {
            aes.KeySize = key.Length * 8;
            aes.BlockSize = 128;
            aes.Padding = PaddingMode.Zeros;

            using (MemoryStream ms = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(ms)) {
                int dataLength = reader.ReadInt32();

                aes.Key = key;
                aes.IV = reader.ReadBytes(aes.IV.Length);

                using (ICryptoTransform decryptor = aes.CreateDecryptor()) {
                    byte[] decryptData = reader.ReadBytes((int)(ms.Length - ms.Position));
                    decryptData = __PerformCryptography(decryptData, decryptor);

                    if (decryptData.Length != dataLength) {
                        Array.Resize(ref decryptData, dataLength);
                    }

                    return decryptData;
                }
            }
        }
    }

    // Выполняет шифрование строки. Для расшифровки используется DecryptString
    public static string EncryptString(string data) {
        byte[] key = new byte[128 / 8];
        using (RandomNumberGenerator random = RandomNumberGenerator.Create()) {
            random.GetBytes(key);
        }
        byte[] encrypt = EncryptAES(Encoding.UTF8.GetBytes(data), key);
        return Convert.ToBase64String(encrypt) + "-" + Convert.ToBase64String(key);
    }

    // Выполняет расшифровку строки, зашифрованной с помощью EncryptString
    public static string DecryptString(string encrypt) {
        string[] split = encrypt.Split('-');
        return Encoding.UTF8.GetString(DecryptAES(Convert.FromBase64String(split[0]), Convert.FromBase64String(split[1])));
    }


    private static byte[] __PerformCryptography(byte[] data, ICryptoTransform cryptoTransform) {
        using (MemoryStream ms = new MemoryStream())
        using (CryptoStream cryptoStream = new CryptoStream(ms, cryptoTransform, CryptoStreamMode.Write)) {
            cryptoStream.Write(data, 0, data.Length);
            cryptoStream.FlushFinalBlock();
            return ms.ToArray();
        }
    }
}
