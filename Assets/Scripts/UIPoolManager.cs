using UnityEngine;
using System.Collections.Generic;

public class UIPoolManager : MonoBehaviour
{
    public static UIPoolManager Instance;

    [Header("Pool Configuration")]
    public GameObject[] effectPrefabs;
    public int amountPerType = 4;

    // A list of lists. Each inner list represents one type of effect.
    private List<List<PooledImpactEffect>> allPools;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializePools();
        }
        else if (Instance != this)
        {
            Destroy(this);
        }
    }

    void InitializePools()
    {
        allPools = new List<List<PooledImpactEffect>>();

        foreach (GameObject prefab in effectPrefabs)
        {
            List<PooledImpactEffect> specificPool = new List<PooledImpactEffect>();

            for (int i = 0; i < amountPerType; i++)
            {
                GameObject obj = Instantiate(prefab, transform);
                obj.SetActive(false);

                var script = obj.GetComponent<PooledImpactEffect>();
                if (script == null) script = obj.AddComponent<PooledImpactEffect>();

                specificPool.Add(script);
            }

            allPools.Add(specificPool);
        }
    }

    public void SpawnRandomEffect(Vector3 position)
    {
        if (allPools.Count == 0) return;

        int randomTypeIndex = Random.Range(0, allPools.Count);
        List<PooledImpactEffect> selectedPool = allPools[randomTypeIndex];

        foreach (var effect in selectedPool)
        {
            if (!effect.gameObject.activeInHierarchy)
            {
                effect.Activate(position);
                return;
            }
        }
    }
}
