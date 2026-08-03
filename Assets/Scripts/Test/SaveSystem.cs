using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public static class SaveSystem
{
    private const string SAVE_KEY = "GAR_SAVE_BLOB";

    private static readonly byte[] AesKey = Encoding.UTF8.GetBytes("R1cksh@wG@r@geSecretKey32Bytes!!"); // 32 bytes -> AES-256
    private static readonly byte[] AesIV = Encoding.UTF8.GetBytes("Init1alVect0r16!"); // 16 bytes

    [Serializable]
    private class SaveData
    {
        public int taka;
        public List<string> unlockedVehicleIds = new List<string>();
        public string equippedVehicleId;
    }

    private static SaveData _cache;

    private static SaveData GetOrLoad()
    {
        if (_cache != null) return _cache;

        if (!PlayerPrefs.HasKey(SAVE_KEY))
        {
            _cache = new SaveData();
            return _cache;
        }

        try
        {
            string encrypted = PlayerPrefs.GetString(SAVE_KEY);
            string json = Decrypt(encrypted);
            _cache = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
        }
        catch
        {
            Debug.LogWarning("[SaveSystem] Save data missing, corrupted, or tampered. Resetting.");
            _cache = new SaveData();
        }
        return _cache;
    }

    private static void Persist()
    {
        string json = JsonUtility.ToJson(_cache);
        PlayerPrefs.SetString(SAVE_KEY, Encrypt(json));
        PlayerPrefs.Save();
    }


    public static SecureInt LoadTaka() => new SecureInt(GetOrLoad().taka);

    public static void SaveTaka(int value)
    {
        GetOrLoad().taka = value;
        Persist();
    }

    public static List<string> LoadUnlockedVehicles() => new List<string>(GetOrLoad().unlockedVehicleIds);

    public static void SaveUnlockedVehicle(string vehicleId)
    {
        var data = GetOrLoad();
        if (!data.unlockedVehicleIds.Contains(vehicleId))
        {
            data.unlockedVehicleIds.Add(vehicleId);
            Persist();
        }
    }


    public static string LoadEquippedVehicle() => GetOrLoad().equippedVehicleId;

    public static void SaveEquippedVehicle(string vehicleId)
    {
        GetOrLoad().equippedVehicleId = vehicleId;
        Persist();
    }


    private static string Encrypt(string plainText)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = AesKey;
            aes.IV = AesIV;
            using (var encryptor = aes.CreateEncryptor())
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                return Convert.ToBase64String(cipherBytes);
            }
        }
    }

    private static string Decrypt(string cipherText)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = AesKey;
            aes.IV = AesIV;
            using (var decryptor = aes.CreateDecryptor())
            {
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                return Encoding.UTF8.GetString(plainBytes);
            }
        }
    }
}
