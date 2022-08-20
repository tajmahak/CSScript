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
        using (var aes = Aes.Create()) {
            aes.KeySize = key.Length * 8;
            aes.BlockSize = 128;
            aes.Padding = PaddingMode.Zeros;

            aes.Key = key;
            aes.GenerateIV();

            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            using (var encryptor = aes.CreateEncryptor()) {
                writer.Write(data.Length);
                writer.Write(aes.IV);
                writer.Write(__PerformCryptography(data, encryptor));
                return ms.ToArray();
            }
        }
    }

    // Выполняет симметричное дешифрование с помощью алгоритма AES с использованием ключа (128/192/256 бит)
    public static byte[] DecryptAES(byte[] data, byte[] key) {
        using (var aes = Aes.Create()) {
            aes.KeySize = key.Length * 8;
            aes.BlockSize = 128;
            aes.Padding = PaddingMode.Zeros;

            using (var ms = new MemoryStream(data))
            using (var reader = new BinaryReader(ms)) {
                var dataLength = reader.ReadInt32();

                aes.Key = key;
                aes.IV = reader.ReadBytes(aes.IV.Length);

                using (var decryptor = aes.CreateDecryptor()) {
                    var decryptData = reader.ReadBytes((int)(ms.Length - ms.Position));
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
        var key = new byte[128 / 8];
        using (var random = RandomNumberGenerator.Create()) {
            random.GetBytes(key);
        }
        var encrypt = EncryptAES(Encoding.UTF8.GetBytes(data), key);
        return Convert.ToBase64String(encrypt) + "-" + Convert.ToBase64String(key);
    }

    // Выполняет расшифровку строки, зашифрованной с помощью EncryptString
    public static string DecryptString(string encrypt) {
        var split = encrypt.Split('-');
        return Encoding.UTF8.GetString(DecryptAES(Convert.FromBase64String(split[0]), Convert.FromBase64String(split[1])));
    }


    private static byte[] __PerformCryptography(byte[] data, ICryptoTransform cryptoTransform) {
        using (var ms = new MemoryStream())
        using (var cryptoStream = new CryptoStream(ms, cryptoTransform, CryptoStreamMode.Write)) {
            cryptoStream.Write(data, 0, data.Length);
            cryptoStream.FlushFinalBlock();
            return ms.ToArray();
        }
    }
}
