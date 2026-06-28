using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ReadableNoteOverlay : MonoBehaviour
{
    [TextArea(8, 16)]
    [SerializeField]
    private string noteText =
        "NO NOO NO NO NO PLEASE NO\n"
        + "THERE IS NO WAY OUT AND HE KNOWS\n"
        + "We were 15 then 14, 12, 8, 5 then 3 then just me.\n"
        + "IT is LISTENING and at this point, I think he can read my thoughts\n"
        + "WHAT IS HE?\n"
        + "I think NO NO NO NO I KNOW, I WILL DIE HERE!\n"
        + "I cannot take it anymore and there is no communication to the outside... I tried...\n"
        + "Oh god I think he is coming, he is right outsi...";

    [Header("Overlay")]
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.82f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private int fontSize = 28;
    [SerializeField] private Vector2 textPadding = new Vector2(120f, 80f);
    [SerializeField] private bool pauseGameWhileReading = true;

    [Header("Debug")]
    [SerializeField] private bool printDebugLogs = true;

    private static ReadableNoteOverlay activeOverlay;

    private GameObject overlayRoot;
    private float previousTimeScale = 1f;
    private bool isShowing;
    private int openedFrame = -1;

    public static bool IsOpen => activeOverlay != null && activeOverlay.isShowing;

    public void Show()
    {
        if (isShowing)
        {
            return;
        }

        if (activeOverlay != null && activeOverlay != this)
        {
            activeOverlay.Hide();
        }

        activeOverlay = this;
        isShowing = true;
        openedFrame = Time.frameCount;
        CreateOverlay();

        if (pauseGameWhileReading)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        if (printDebugLogs)
        {
            Debug.Log("Readable note overlay opened.", this);
        }
    }

    private void Update()
    {
        if (!isShowing || Keyboard.current == null)
        {
            return;
        }

        // Ignore the pickup/interact key press that opened the note so it cannot close instantly.
        if (Time.frameCount == openedFrame)
        {
            return;
        }

        if (
            Keyboard.current.eKey.wasPressedThisFrame
            || Keyboard.current.escapeKey.wasPressedThisFrame
            || Keyboard.current.spaceKey.wasPressedThisFrame
        )
        {
            Hide();
        }
    }

    private void Hide()
    {
        if (!isShowing)
        {
            return;
        }

        isShowing = false;

        if (pauseGameWhileReading)
        {
            Time.timeScale = previousTimeScale;
        }

        if (overlayRoot != null)
        {
            Destroy(overlayRoot);
        }

        if (activeOverlay == this)
        {
            activeOverlay = null;
        }

        if (printDebugLogs)
        {
            Debug.Log("Readable note overlay closed.", this);
        }
    }

    private void OnDisable()
    {
        Hide();
    }

    private void CreateOverlay()
    {
        overlayRoot = new GameObject("ReadableNoteOverlay", typeof(RectTransform));

        Canvas canvas = overlayRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue - 1;

        overlayRoot.AddComponent<CanvasScaler>();
        overlayRoot.AddComponent<GraphicRaycaster>();

        Image background = overlayRoot.AddComponent<Image>();
        background.color = backgroundColor;

        RectTransform rootRect = overlayRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        GameObject textObject = new GameObject("NoteText", typeof(RectTransform));
        textObject.transform.SetParent(overlayRoot.transform, false);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = noteText;
        text.color = textColor;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textPadding;
        textRect.offsetMax = -textPadding;
    }
}
