using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ItemObjectiveListUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private Vector2 anchoredPosition = new Vector2(-260f, -170f);
    [SerializeField] private Vector2 panelSize = new Vector2(430f, 520f);
    [SerializeField] private Color panelColor = new Color(0.02f, 0.04f, 0.06f, 0.86f);
    [SerializeField] private Color titleColor = Color.white;
    [SerializeField] private Color missingColor = new Color(0.95f, 0.92f, 0.82f, 1f);
    [SerializeField] private Color completeColor = new Color(0.45f, 1f, 0.68f, 1f);

    [Header("Text")]
    [SerializeField] private float titleFontSize = 28f;
    [SerializeField] private float rowFontSize = 20f;

    [Header("Debug")]
    [SerializeField] private bool printDebugLogs = true;

    private RectTransform panelTransform;
    private readonly List<TextMeshProUGUI> rowTexts = new List<TextMeshProUGUI>();
    private bool isVisible;

    private void Awake()
    {
        BuildPanel();
        SetVisible(false);
    }

    private void OnEnable()
    {
        GameState.Instance.OnItemObjectivesChanged += RefreshRows;
        RefreshRows();
    }

    private void OnDisable()
    {
        if (GameState.HasInstance)
        {
            GameState.Instance.OnItemObjectivesChanged -= RefreshRows;
        }
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        // Tab only toggles the checklist UI; it does not touch inventory, movement, or interactions.
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            SetVisible(!isVisible);
        }
    }

    private void BuildPanel()
    {
        if (panelTransform != null)
        {
            return;
        }

        GameObject panelObject = new GameObject("ItemObjectiveListPanel", typeof(RectTransform));
        panelObject.transform.SetParent(transform, false);

        panelTransform = panelObject.GetComponent<RectTransform>();
        panelTransform.anchorMin = new Vector2(1f, 1f);
        panelTransform.anchorMax = new Vector2(1f, 1f);
        panelTransform.pivot = new Vector2(1f, 1f);
        panelTransform.anchoredPosition = anchoredPosition;
        panelTransform.sizeDelta = panelSize;

        Image background = panelObject.AddComponent<Image>();
        background.color = panelColor;
        background.raycastTarget = false;

        VerticalLayoutGroup layout = panelObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 22, 22);
        layout.spacing = 12f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CreateText(panelObject.transform, "ChecklistTitle", "Required Items", titleFontSize, titleColor, FontStyles.Bold);

        foreach (GameState.ItemObjectiveDefinition objective in GameState.Instance.RequiredItemObjectives)
        {
            TextMeshProUGUI rowText = CreateText(
                panelObject.transform,
                "Objective_" + objective.repairTask,
                "",
                rowFontSize,
                missingColor,
                FontStyles.Normal
            );

            rowTexts.Add(rowText);
        }
    }

    private TextMeshProUGUI CreateText(
        Transform parent,
        string objectName,
        string text,
        float fontSize,
        Color color,
        FontStyles fontStyle
    )
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI textComponent = textObject.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.color = color;
        textComponent.fontStyle = fontStyle;
        textComponent.alignment = TextAlignmentOptions.Left;
        textComponent.enableWordWrapping = true;
        textComponent.raycastTarget = false;

        LayoutElement layoutElement = textObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = Mathf.Ceil(fontSize * 1.45f);
        layoutElement.preferredHeight = Mathf.Ceil(fontSize * 1.6f);

        return textComponent;
    }

    private void SetVisible(bool shouldShow)
    {
        isVisible = shouldShow;

        if (panelTransform != null)
        {
            panelTransform.gameObject.SetActive(isVisible);
        }

        if (isVisible)
        {
            RefreshRows();
        }

        if (printDebugLogs)
        {
            Debug.Log("Item objective list visible: " + isVisible, this);
        }
    }

    private void RefreshRows()
    {
        if (rowTexts.Count == 0)
        {
            return;
        }

        IReadOnlyList<GameState.ItemObjectiveDefinition> objectives = GameState.Instance.RequiredItemObjectives;

        for (int i = 0; i < rowTexts.Count && i < objectives.Count; i++)
        {
            GameState.ItemObjectiveDefinition objective = objectives[i];
            bool isComplete = GameState.Instance.IsSubmarineTaskComplete(objective.repairTask);
            int requiredCount = Mathf.Max(1, objective.requiredProgressCount);
            int currentCount = isComplete
                ? requiredCount
                : Mathf.Clamp(GameState.Instance.GetSubmarineTaskProgress(objective.repairTask), 0, requiredCount);
            TextMeshProUGUI rowText = rowTexts[i];
            string progressText = requiredCount > 1
                ? " " + currentCount + "/" + requiredCount
                : "";

            // Completion state is visualized in the row itself so the list stays readable at a glance.
            rowText.text = isComplete
                ? "[Done] " + objective.displayName + progressText
                : "[Missing] " + objective.displayName + progressText;
            rowText.color = isComplete ? completeColor : missingColor;
            rowText.fontStyle = isComplete
                ? FontStyles.Strikethrough
                : FontStyles.Normal;
        }
    }
}
