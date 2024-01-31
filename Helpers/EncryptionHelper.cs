using System;
using NETCore.Encrypt;

namespace FoosballApi.Helpers;

public class EncryptionHelper
{
    public static string EncryptString(string plainText)
    {
        var hashed = EncryptProvider.Base64Encrypt(plainText);
        return hashed;
    }

    public static string DecryptString(string encryptedString)
    {
        var key = Environment.GetEnvironmentVariable("aesKey");
        var iv = Environment.GetEnvironmentVariable("aesIV");
        var decryptedString = EncryptProvider.AESDecrypt(encryptedString, key, iv);

        return decryptedString;
    }
}
