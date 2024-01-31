using System;
using NETCore.Encrypt;

namespace FoosballApi.Helpers;

public class EncryptionHelper
{
    public static string EncryptString(string plainText)
    {
        var hashed = EncryptProvider.Base64Encrypt(plainText);
        string replacement = "SlashReplacement";
        hashed = hashed.Replace("/", replacement);
        return hashed;
    }

    public static string DecryptString(string encryptedString)
    {
        string replacement = "SlashReplacement";
        var decryptedString = EncryptProvider.Base64Decrypt(encryptedString);
        string originalEncodedString = decryptedString.Replace(replacement, "/");
        return originalEncodedString;
    }
}
