using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    [SerializeField] private GameObject[] itemPrefabs;

    private void Awake()
    {
        RegisterItems();
    }

    public void RegisterItems()
    {
        foreach (GameObject itemPrefab in itemPrefabs)
        {
            GameState.Instance.RegisterItemPrefab(itemPrefab);
        }
    }
}
