using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Отображение счёта, меню, паузы и экрана поражения по событиям GameManager.
public class GameUI : MonoBehaviour
{
    [SerializeField] TMP_Text scoreText;
    [SerializeField] GameObject menuPanel;
    [SerializeField] GameObject pausedPanel;
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] TMP_Text finalScoreText;
    [SerializeField] TMP_Text bestScoreText;
    [SerializeField] Button pauseButton;
    [SerializeField] Button resumeButton;

    bool subscribed;

    void OnEnable()
    {
        TrySubscribe();
        if (pauseButton) pauseButton.onClick.AddListener(HandlePauseClicked);
        if (resumeButton) resumeButton.onClick.AddListener(HandleResumeClicked);
    }

    void Start()
    {
        TrySubscribe();
        var gm = GameManager.Instance;
        if (gm == null) return;

        HandleScoreChanged(gm.Score);
        HandleStateChanged(gm.State);
    }

    void OnDisable()
    {
        Unsubscribe();
        if (pauseButton) pauseButton.onClick.RemoveListener(HandlePauseClicked);
        if (resumeButton) resumeButton.onClick.RemoveListener(HandleResumeClicked);
    }

    void TrySubscribe()
    {
        if (subscribed) return;
        var gm = GameManager.Instance;
        if (gm == null) return;

        gm.OnScoreChanged += HandleScoreChanged;
        gm.OnStateChanged += HandleStateChanged;
        subscribed = true;
    }

    void Unsubscribe()
    {
        if (!subscribed) return;
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnScoreChanged -= HandleScoreChanged;
            gm.OnStateChanged -= HandleStateChanged;
        }
        subscribed = false;
    }

    void HandlePauseClicked() => GameManager.Instance?.Pause();
    void HandleResumeClicked() => GameManager.Instance?.Resume();

    void HandleScoreChanged(int score)
    {
        scoreText.text = score.ToString();
    }

    void HandleStateChanged(GameState state)
    {
        menuPanel.SetActive(state == GameState.Menu);
        if (pausedPanel) pausedPanel.SetActive(state == GameState.Paused);
        gameOverPanel.SetActive(state == GameState.GameOver);
        scoreText.gameObject.SetActive(state == GameState.Playing || state == GameState.Paused);
        if (pauseButton) pauseButton.gameObject.SetActive(state == GameState.Playing);

        if (state == GameState.GameOver)
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            finalScoreText.text = $"Score: {gm.Score}";
            bestScoreText.text = $"Best: {gm.BestScore}";
        }
    }
}
