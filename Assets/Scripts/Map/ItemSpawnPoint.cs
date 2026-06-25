using UnityEngine;

public class ItemSpawnPoint : MonoBehaviour
{
    [SerializeField] private ItemSize[] allowedItemSizes =
    {
        ItemSize.Small,
        ItemSize.Medium,
        ItemSize.Big
    };

    public bool Allows(ItemSize itemSize)
    {
        if (allowedItemSizes == null || allowedItemSizes.Length == 0)
        {
            return false;
        }

        foreach (ItemSize allowedItemSize in allowedItemSizes)
        {
            if (allowedItemSize == itemSize)
            {
                return true;
            }
        }

        return false;
    }
}
