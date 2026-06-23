using System.Collections.Generic;
using UnityEngine;

public class RandomItemSpawner : MonoBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private Transform spawnPointParent;
    [SerializeField] private Transform[] spawnPoints;

    [Header("Items")]
    [SerializeField] private GameObject[] itemPrefabs;
    [SerializeField] private int amountToSpawn = 3;

    [Header("Settings")]
    [SerializeField] private bool useEachItemOnlyOnce = true;
    [SerializeField] private bool matchSpawnPointRotation = false;
    [SerializeField] private bool parentItemsToSpawner = true;
    [SerializeField] private bool spawnOnStart = true;

    [Header("Ground Fix")]
    [SerializeField] private bool autoFixGroundHeight = true;
    [SerializeField] private float groundOffset = 0.02f;
    [SerializeField] private float extraRaiseOffset = 0f;

    private readonly List<GameObject> spawnedItems = new List<GameObject>();

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnItems();
        }
    }

    public void SpawnItems()
    {
        ClearSpawnedItems();

        List<Transform> availableSpawnPoints = GetSpawnPoints();
        List<GameObject> availableItems = new List<GameObject>(itemPrefabs);

        if (availableSpawnPoints.Count == 0)
        {
            Debug.LogWarning("No spawn points assigned.");
            return;
        }

        if (availableItems.Count == 0)
        {
            Debug.LogWarning("No item prefabs assigned.");
            return;
        }

        int spawnCount = Mathf.Min(amountToSpawn, availableSpawnPoints.Count);

        if (useEachItemOnlyOnce)
        {
            spawnCount = Mathf.Min(spawnCount, availableItems.Count);
        }

        Shuffle(availableSpawnPoints);
        Shuffle(availableItems);

        for (int i = 0; i < spawnCount; i++)
        {
            Transform spawnPoint = availableSpawnPoints[i];

            GameObject itemPrefab;

            if (useEachItemOnlyOnce)
            {
                itemPrefab = availableItems[i];
            }
            else
            {
                int randomItemIndex = Random.Range(0, availableItems.Count);
                itemPrefab = availableItems[randomItemIndex];
            }

            Quaternion rotation = matchSpawnPointRotation
                ? spawnPoint.rotation
                : itemPrefab.transform.rotation;

            Transform parent = parentItemsToSpawner ? transform : null;

            GameObject spawnedItem = Instantiate(
                itemPrefab,
                spawnPoint.position,
                rotation,
                parent
            );

            if (autoFixGroundHeight)
            {
                FixGroundHeight(spawnedItem, spawnPoint.position.y);
            }

            if (extraRaiseOffset != 0f)
            {
                spawnedItem.transform.position += Vector3.up * extraRaiseOffset;
            }

            spawnedItems.Add(spawnedItem);
        }
    }

    public void ClearSpawnedItems()
    {
        foreach (GameObject item in spawnedItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }

        spawnedItems.Clear();
    }

    private List<Transform> GetSpawnPoints()
    {
        List<Transform> points = new List<Transform>();

        if (spawnPointParent != null)
        {
            foreach (Transform child in spawnPointParent)
            {
                points.Add(child);
            }
        }

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            points.AddRange(spawnPoints);
        }

        return points;
    }

    private void FixGroundHeight(GameObject item, float groundY)
    {
        if (!TryGetObjectBounds(item, out Bounds bounds))
        {
            Debug.LogWarning("No Renderer found on spawned item: " + item.name);
            return;
        }

        float objectBottomY = bounds.min.y;
        float wantedBottomY = groundY + groundOffset;

        float difference = wantedBottomY - objectBottomY;

        item.transform.position += Vector3.up * difference;
    }

    private bool TryGetObjectBounds(GameObject item, out Bounds bounds)
    {
        Renderer[] renderers = item.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
        {
            bounds = new Bounds(item.transform.position, Vector3.zero);
            return false;
        }

        bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return true;
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);

            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}