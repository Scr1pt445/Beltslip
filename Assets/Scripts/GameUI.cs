using TMPro;
using UnityEngine;

// Отображение счёта, меню и экрана поражения по событиям GameManager.
public class GameUI : MonoBehaviour
{
    [SerializeField] TMP_Text scoreText;
    [SerializeField] GameObject menuPanel;
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] TMP_Text finalScoreText;
    [SerializeField] TMP_Text bestScoreText;

    bool subscribed;

    void OnEnable()
    {
        TrySubscribe();
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

    void HandleScoreChanged(int score)
    {
        scoreText.text = score.ToString();
    }

    void HandleStateChanged(GameState state)
    {
        menuPanel.SetActive(state == GameState.Menu);
        gameOverPanel.SetActive(state == GameState.GameOver);
        scoreText.gameObject.SetActive(state == GameState.Playing);

        if (state == GameState.GameOver)
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            finalScoreText.text = $"Score: {gm.Score}";
            bestScoreText.text = $"Best: {gm.BestScore}";
        }
    }
}
