using System;
using UnityEngine;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }
    /*
    [Header("Conversion Multipliers (tune for game balance)")]
    [SerializeField] private float takaPerScorePoint = 0.5f;
    [SerializeField] private float takaPerDistanceMeter = 1.2f;
    for multiplier, but for now just add score + distance
    */
    private SecureInt _wallet;

    public static event Action<int> OnBalanceChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _wallet = SaveSystem.LoadTaka();
    }

    public int CurrentBalance => _wallet.Value;

    public int ConvertRunToTaka(int finalScore, float finalDistanceMeters)
    {
        int earned = Mathf.RoundToInt(finalScore + finalDistanceMeters);    
        //int earned = Mathf.RoundToInt(finalScore* takaPerScorePoint + finalDistanceMeters * takaPerDistanceMeter); for multiplier

        AddTaka(earned);
        return earned;
    }

    public void AddTaka(int amount)
    {
        if (amount <= 0) return;
        _wallet = _wallet.Value + amount;
        BroadcastAndSave();
    }

    public bool SpendTaka(int amount)
    {
        if (amount <= 0) return true;
        if (_wallet.Value < amount) return false;

        _wallet = _wallet.Value - amount;
        BroadcastAndSave();
        return true;
    }

    private void BroadcastAndSave()
    {
        OnBalanceChanged?.Invoke(_wallet.Value);
        SaveSystem.SaveTaka(_wallet.Value);
    }
}
