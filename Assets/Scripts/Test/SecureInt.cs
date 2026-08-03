using System;
using UnityEngine;

[Serializable]
public struct SecureInt
{
    private int _encryptedValue;
    private int _encryptedShadow;
    private int _key;

    public static event Action OnTamperDetected;

    public SecureInt(int value)
    {
        _key = GenerateKey();
        _encryptedValue = 0;
        _encryptedShadow = 0;
        Encode(value);
    }

    private static int GenerateKey()
    {
        return UnityEngine.Random.Range(int.MinValue, int.MaxValue);
    }

    private void Encode(int value)
    {
        _encryptedValue = value ^ _key;
        _encryptedShadow = ~value ^ RotateLeft(_key, 13);
    }

    private static int RotateLeft(int v, int bits)
    {
        uint u = (uint)v;
        return (int)((u << bits) | (u >> (32 - bits)));
    }

    public int Value
    {
        get
        {
            int primary = _encryptedValue ^ _key;
            int shadow = ~(_encryptedShadow ^ RotateLeft(_key, 13));

            if (primary != shadow)
            {
                Debug.LogWarning("[SecureInt] Tamper detected — shadow copy mismatch.");
                OnTamperDetected?.Invoke();
                return 0;
            }
            return primary;
        }
        set => Encode(value);
    }

    public static implicit operator int(SecureInt s) => s.Value;
    public static implicit operator SecureInt(int i) => new SecureInt(i);

    public static SecureInt operator +(SecureInt a, int b) => new SecureInt(a.Value + b);
    public static SecureInt operator -(SecureInt a, int b) => new SecureInt(a.Value - b);

    public override string ToString() => Value.ToString();
}
