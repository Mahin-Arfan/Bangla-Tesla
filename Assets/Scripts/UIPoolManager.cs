using UnityEngine;
using System.Collections.Generic;

public class UIPoolManager : MonoBehaviour
{
    public static UIPoolManager Instance;

    [Header("Pool Configuration")]
    public GameObject[] effectPrefabs; // Drag your different Prefabs here (Bang, Pow, Boom)
    public int amountPerType = 4;

    // A list of lists. Each inner list represents one type of effect.
    private List<List<PooledImpactEffect>> allPools;

    void Awake()
    {
        Instance = this;
        InitializePools();
    }

    void InitializePools()
    {
        allPools = new List<List<PooledImpactEffect>>();

        // Loop through every prefab type you dragged in
        foreach (GameObject prefab in effectPrefabs)
        {
            List<PooledImpactEffect> specificPool = new List<PooledImpactEffect>();

            for (int i = 0; i < amountPerType; i++)
            {
                GameObject obj = Instantiate(prefab, transform); // Keep hierarchy clean
                obj.SetActive(false);

                // Add the script if missing (safety check)
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

        // 1. Pick a random TYPE of effect (e.g., Type 0 is "Pow", Type 1 is "Bang")
        int randomTypeIndex = Random.Range(0, allPools.Count);
        List<PooledImpactEffect> selectedPool = allPools[randomTypeIndex];

        // 2. Find an available object in that specific pool
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
