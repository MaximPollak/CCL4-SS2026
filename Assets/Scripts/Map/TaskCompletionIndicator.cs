using UnityEngine;

public class TaskCompletionIndicator : MonoBehaviour
{
    [Header("Visible Indicator")]
    [SerializeField] private Renderer indicatorRenderer;
    [SerializeField] private Material incompleteMaterial;
    [SerializeField] private Material completeMaterial;

    [Header("Completion Light")]
    [SerializeField] private Light completionLight;

    private void Awake()
    {
        SetComplete(false);
    }

    public void SetComplete(bool isComplete)
    {
        if (indicatorRenderer != null)
        {
            Material targetMaterial = isComplete ? completeMaterial : incompleteMaterial;

            if (targetMaterial != null)
            {
                indicatorRenderer.material = targetMaterial;
            }
        }

        if (completionLight != null)
        {
            completionLight.enabled = isComplete;
        }
    }
}