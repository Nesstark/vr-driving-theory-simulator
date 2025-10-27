using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ScenarioManager
{
    // Use a very high sorting order so the overlay stays above most project UI canvases.
    private const int DefaultSortingOrder = 32767;

    // Public default fade duration (seconds). Change this to affect all SceneManager fade calls.
    public static float DefaultFadeDuration = 2f;
    // Configurable cooldown to prevent rapid repeated scene loads (seconds).
    public static float LoadCooldownSeconds = 10f;
    // Internal state to prevent double loads
    private static bool s_isLoading = false;
    private static float s_lastLoadTime = -Mathf.Infinity;
    private static ScenarioManagerBehaviour s_instance;

    // Attempts to begin a load. Returns true if the load may proceed and internal state
    // (s_isLoading / s_lastLoadTime) has been updated. Callers should return early when
    // this method returns false.
    private static bool TryBeginLoad()
    {
        if (s_isLoading || (Time.realtimeSinceStartup - s_lastLoadTime) < LoadCooldownSeconds)
        {
            Debug.Log($"ScenarioManager: Load suppressed. Cooldown active ({LoadCooldownSeconds}s).");
            return false;
        }

        EnsureInstance();
        s_isLoading = true;
        s_lastLoadTime = Time.realtimeSinceStartup;
        return true;
    }

    private static void EnsureInstance()
    {
        if (s_instance != null) return;
        var go = new GameObject("__ScenarioManager_Runtime");
        Object.DontDestroyOnLoad(go);
        s_instance = go.AddComponent<ScenarioManagerBehaviour>();
    }

    public static void LoadNextScene(float fadeDuration = -1f)
    {
        if (!TryBeginLoad()) return;
        int nextIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex + 1;
        float d = fadeDuration > 0f ? fadeDuration : DefaultFadeDuration;
        s_instance.StartFadeAndLoadByIndex(nextIndex, d);
    }

    public static void LoadPreviousScene(float fadeDuration = -1f)
    {
        if (!TryBeginLoad()) return;
        int prevIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex - 1;
        float d = fadeDuration > 0f ? fadeDuration : DefaultFadeDuration;
        s_instance.StartFadeAndLoadByIndex(prevIndex, d);
    }

    public static void LoadSceneByIndex(int index, float fadeDuration = -1f)
    {
        if (!TryBeginLoad()) return;
        float d = fadeDuration > 0f ? fadeDuration : DefaultFadeDuration;
        s_instance.StartFadeAndLoadByIndex(index, d);
    }

    public static void LoadSceneByName(string name, float fadeDuration = -1f)
    {
        if (!TryBeginLoad()) return;
        float d = fadeDuration > 0f ? fadeDuration : DefaultFadeDuration;
        s_instance.StartFadeAndLoadByName(name, d);
    }

    public static bool SceneExists(string sceneName)
    {
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameFromPath = Path.GetFileNameWithoutExtension(scenePath);
                if (sceneNameFromPath == sceneName) return true;
        }
        return false;
    }

    // Internal behaviour to host coroutines and the fade UI
    private class ScenarioManagerBehaviour : MonoBehaviour
    {
        private Canvas _canvas;
        private Image _overlayImage;
        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            CreateOverlay();
        }

        private void CreateOverlay()
        {
            // Root canvas
            var canvasGO = new GameObject("ScenarioManager_OverlayCanvas");
            canvasGO.transform.SetParent(transform, false);
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = DefaultSortingOrder;
            // Ensure canvas remains on top of other canvases. Also make sure it's pixel-perfect.
            _canvas.pixelPerfect = true;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            // Full-screen image
            var imgGO = new GameObject("OverlayImage");
            imgGO.transform.SetParent(canvasGO.transform, false);
            _overlayImage = imgGO.AddComponent<Image>();
            _overlayImage.color = Color.black;
            _overlayImage.raycastTarget = false; // start non-blocking

            // Stretch to full screen
            var rt = _overlayImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;

            _canvasGroup = imgGO.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        public void StartFadeAndLoadByName(string sceneName, float duration)
        {
            StartCoroutine(FadeAndLoadSceneByNameCoroutine(sceneName, duration));
        }

        public void StartFadeAndLoadByIndex(int buildIndex, float duration)
        {
            // Validate index
            if (buildIndex < 0 || buildIndex >= UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogWarning($"Requested build index {buildIndex} is out of range.");
                return;
            }
            string scenePath = SceneUtility.GetScenePathByBuildIndex(buildIndex);
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);
            StartCoroutine(FadeAndLoadSceneByNameCoroutine(sceneName, duration));
        }

        private IEnumerator FadeAndLoadSceneByNameCoroutine(string sceneName, float duration)
        {
            if (!ScenarioManager.SceneExists(sceneName))
            {
                Debug.LogWarning($"Scene '{sceneName}' not found in build settings.");
                yield break;
            }

            // Ensure overlay exists and is on top
            if (_canvas == null) CreateOverlay();

            // Block input by enabling raycast target and blocksRaycasts
            _overlayImage.raycastTarget = true;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;

            float half = Mathf.Max(0.01f, duration) * 0.5f;

            // Fade out
            yield return StartCoroutine(Fade(0f, 1f, half));

            // Begin async load and wait until ready
            var op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false;

            // Wait until load progress reaches 0.9 (scene loaded, waiting for activation)
            while (op.progress < 0.9f)
            {
                yield return null;
            }

            // Activate the scene
            op.allowSceneActivation = true;
            while (!op.isDone)
            {
                yield return null;
            }

            // Force canvas update to ensure the overlay is still on top before we start fading in.
            Canvas.ForceUpdateCanvases();
            // Small yield to allow render/update to settle (prevents a single-frame flash)
            yield return null;

            // Fade in
            yield return StartCoroutine(Fade(1f, 0f, half));

            // Unblock input
            _overlayImage.raycastTarget = false;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
            // Mark load complete so further loads can proceed after cooldown
            ScenarioManager.s_isLoading = false;
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            float t = 0f;
            _canvasGroup.alpha = from;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
                yield return null;
            }
            _canvasGroup.alpha = to;
        }
    }
}
