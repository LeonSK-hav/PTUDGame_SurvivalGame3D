using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; }

    [Header("UI Transition (Optional)")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 0.5f;

    private void Awake()
    {
        // Singleton Pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Fade in khi vừa vào scene mới
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.gameObject.SetActive(true);
            StartCoroutine(FadeRoutine(1f, 0f, fadeDuration, () => {
                fadeCanvasGroup.gameObject.SetActive(false);
            }));
        }
    }

    /// <summary>
    /// Chuyển scene bằng tên
    /// </summary>
    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    /// <summary>
    /// Chuyển scene bằng Index trong Build Settings
    /// </summary>
    public void LoadSceneByIndex(int sceneIndex)
    {
        StartCoroutine(LoadSceneRoutine(sceneIndex));
    }

    /// <summary>
    /// Tải lại Scene hiện tại
    /// </summary>
    public void ReloadCurrentScene()
    {
        LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Thoát ứng dụng (Game / Build / Editor)
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Game is quitting...");
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        // Hiệu ứng Fade Out trước khi chuyển scene
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.gameObject.SetActive(true);
            yield return StartCoroutine(FadeRoutine(0f, 1f, fadeDuration, null));
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    private IEnumerator LoadSceneRoutine(int sceneIndex)
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.gameObject.SetActive(true);
            yield return StartCoroutine(FadeRoutine(0f, 1f, fadeDuration, null));
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneIndex);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    private IEnumerator FadeRoutine(float startAlpha, float endAlpha, float duration, System.Action onComplete)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            yield return null;
        }
        fadeCanvasGroup.alpha = endAlpha;
        onComplete?.Invoke();
    }
}