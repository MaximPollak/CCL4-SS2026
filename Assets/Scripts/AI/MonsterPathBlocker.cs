using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class MonsterPathBlocker : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private bool autoSetWallLayer = true;
    [SerializeField] private string wallLayerName = "Wall";
    [SerializeField] private bool autoSetColliderAsTrigger = false;
    [SerializeField] private BoxCollider blockerCollider;

    [Header("Debug")]
    [SerializeField] private Color gizmoColor = new Color(1f, 0f, 0f, 0.25f);

    private BoxCollider boxCollider;

    private void Reset()
    {
        Configure();
    }

    private void OnValidate()
    {
        Configure();
    }

    private void Awake()
    {
        Configure();
    }

    private void Configure()
    {
        if (blockerCollider == null)
        {
            blockerCollider = GetComponent<BoxCollider>();
        }

        boxCollider = blockerCollider;

        if (boxCollider != null && autoSetColliderAsTrigger)
        {
            boxCollider.isTrigger = true;
        }

        if (!autoSetWallLayer && gameObject.layer != 0)
        {
            return;
        }

        int wallLayer = LayerMask.NameToLayer(wallLayerName);

        if (wallLayer >= 0)
        {
            gameObject.layer = wallLayer;
        }
        else
        {
            Debug.LogWarning("MonsterPathBlocker could not find layer: " + wallLayerName, this);
        }
    }

    private void OnDrawGizmosSelected()
    {
        BoxCollider selectedCollider = GetComponent<BoxCollider>();

        if (selectedCollider == null)
        {
            return;
        }

        Gizmos.color = gizmoColor;
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(selectedCollider.center, selectedCollider.size);
        Gizmos.DrawWireCube(selectedCollider.center, selectedCollider.size);
        Gizmos.matrix = previousMatrix;
    }
}
