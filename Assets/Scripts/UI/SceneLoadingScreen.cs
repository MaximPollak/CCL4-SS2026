using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoadingScreen : MonoBehaviour
{
    private const int SpinnerDotCount = 12;

    private static SceneLoadingScreen activeLoadingScreen;

    [SerializeField] private float spinnerRotationSpeed = 180f;

    private RectTransform spinnerRoot;
    private AsyncOperation sceneLoadOperation;
    private float minimumVisibleTime;

    public static bool IsLoading => activeLoadingScreen != null;

    public static void LoadSceneWithScreen(string sceneName, float minimumScreenTime)
    {
        if (activeLoadingScreen != null)
        {
            return;
        }

        SceneLoadingScreen loadingScreen = CreateLoadingScreen();
        loadingScreen.StartCoroutine(loadingScreen.LoadSceneRoutine(sceneName, minimumScreenTime));
    }

    private static SceneLoadingScreen CreateLoadingScreen()
    {
        GameObject root = new GameObject("SceneLoadingScreen", typeof(RectTransform));
        DontDestroyOnLoad(root);

        SceneLoadingScreen loadingScreen = root.AddComponent<SceneLoadingScreen>();
        activeLoadingScreen = loadingScreen;

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        root.AddComponent<CanvasScaler>();
        root.AddComponent<GraphicRaycaster>();

        Image background = root.AddComponent<Image>();
        background.color = Color.black;

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        loadingScreen.CreateSpinner(root.transform);
        return loadingScreen;
    }

    private IEnumerator LoadSceneRoutine(string sceneName, float minimumScreenTime)
    {
        minimumVisibleTime = Mathf.Max(0f, minimumScreenTime);

        yield return null;

        sceneLoadOperation = SceneManager.LoadSceneAsync(sceneName);

        if (sceneLoadOperation == null)
        {
            Destroy(gameObject);
            yield break;
        }

        sceneLoadOperation.allowSceneActivation = false;

        float startTime = Time.unscaledTime;

        while (sceneLoadOperation.progress < 0.9f)
        {
            yield return null;
        }

        float visibleTime = Time.unscaledTime - startTime;

        if (visibleTime < minimumVisibleTime)
        {
            yield return new WaitForSecondsRealtime(minimumVisibleTime - visibleTime);
        }

        sceneLoadOperation.allowSceneActivation = true;

        while (!sceneLoadOperation.isDone)
        {
            yield return null;
        }

        // Keep the black screen briefly after activation so spawn restoration can finish hidden.
        yield return null;
        yield return new WaitForFixedUpdate();
        yield return new WaitForSecondsRealtime(0.15f);

        Destroy(gameObject);
    }

    private void Update()
    {
        if (spinnerRoot != null)
        {
            spinnerRoot.Rotate(0f, 0f, -spinnerRotationSpeed * Time.unscaledDeltaTime);
        }
    }

    private void OnDestroy()
    {
        if (activeLoadingScreen == this)
        {
            activeLoadingScreen = null;
        }
    }

    private void CreateSpinner(Transform parent)
    {
        GameObject spinnerObject = new GameObject("LoadingWheel", typeof(RectTransform));
        spinnerObject.transform.SetParent(parent, false);

        spinnerRoot = spinnerObject.GetComponent<RectTransform>();
        spinnerRoot.anchorMin = new Vector2(0.5f, 0.5f);
        spinnerRoot.anchorMax = new Vector2(0.5f, 0.5f);
        spinnerRoot.anchoredPosition = Vector2.zero;
        spinnerRoot.sizeDelta = new Vector2(96f, 96f);

        for (int i = 0; i < SpinnerDotCount; i++)
        {
            GameObject dotObject = new GameObject("Dot", typeof(RectTransform));
            dotObject.transform.SetParent(spinnerRoot, false);

            RectTransform dotRect = dotObject.GetComponent<RectTransform>();
            dotRect.sizeDelta = new Vector2(8f, 8f);

            float angle = i * Mathf.PI * 2f / SpinnerDotCount;
            dotRect.anchoredPosition = new Vector2(
                Mathf.Cos(angle) * 36f,
                Mathf.Sin(angle) * 36f
            );

            Image dotImage = dotObject.AddComponent<Image>();
            float alpha = Mathf.Lerp(0.2f, 1f, (i + 1f) / SpinnerDotCount);
            dotImage.color = new Color(1f, 1f, 1f, alpha);
        }
    }
}
