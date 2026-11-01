using TMPro;
using UnityEngine;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI quoteText;
    [SerializeField] private string mainMenuScene = "StartScene";
    [SerializeField] private string[] quotes =
    {
        "Kiếp này bạn chưa đủ trình rồi, kiếp sau làm lại nhé!",
        "Zombie bảo: \"Thịt ngon đấy, làm ván nữa không bro?\"",
        "Tưởng thế nào, làm bữa tối cho Zombie rồi 😂"
    };

    private bool isGameOver;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ShowGameOver()
    {
        if (isGameOver) return;

        if (gameOverPanel == null)
        {
            Debug.LogError("Game Over Panel chưa được gán trong GameOverManager.", this);
            return;
        }

        isGameOver = true;
        if (quoteText != null && quotes != null && quotes.Length > 0)
        {
            quoteText.text = quotes[Random.Range(0, quotes.Length)];
        }

        gameOverPanel.SetActive(true);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetUIOpen(gameOverPanel, true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        if (SceneController.Instance == null)
        {
            Debug.LogError("Không tìm thấy SceneController để tải lại scene.", this);
            return;
        }

        SceneController.Instance.ReloadCurrentScene();
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        if (SceneController.Instance == null)
        {
            Debug.LogError("Không tìm thấy SceneController để mở menu chính.", this);
            return;
        }

        SceneController.Instance.LoadScene(mainMenuScene);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        if (SceneController.Instance == null)
        {
            Debug.LogError("Không tìm thấy SceneController để thoát game.", this);
            return;
        }

        SceneController.Instance.QuitGame();
    }
}
