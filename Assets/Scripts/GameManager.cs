using TMPro;
using UnityEngine;

public enum GameState { Menu, Playing, GameOver }

// Состояние игры, скорость мира, счёт, рекорд и UI.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Скорость мира")]
    [SerializeField] float startSpeed = 7f;
    [SerializeField] float maxSpeed = 16f;
    [SerializeField] float speedGainPerSecond = 0.15f;

    [Header("Ссылки")]
    [SerializeField] ShipController ship;
    [SerializeField] ObstacleSpawner spawner;

    [Header("UI")]
    [SerializeField] TMP_Text scoreText;
    [SerializeField] GameObject menuPanel;
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] TMP_Text finalScoreText;
    [SerializeField] TMP_Text bestScoreText;

    public GameState State { get; private set; } = GameState.Menu;
    public float Speed { get; private set; }
    public int Score { get; private set; }

    // 0 на старте, 1 на максимальной скорости — от этого зависит сложность спавна
    public float Difficulty01 => Mathf.InverseLerp(startSpeed, maxSpeed, Speed);

    const string BestKey = "BestScore";
    int best;
    float inputBlockedUntil;

    void Awake()
    {
        Instance = this;
        best = PlayerPrefs.GetInt(BestKey, 0);
        Application.targetFrameRate = 60;
    }

    void Start()
    {
        State = GameState.Menu;
        menuPanel.SetActive(true);
        gameOverPanel.SetActive(false);
        scoreText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (State == GameState.Playing)
        {
            Speed = Mathf.Min(maxSpeed, Speed + speedGainPerSecond * Time.deltaTime);
            return;
        }

        // Меню или экран поражения: ждём нажатия (с небольшой паузой после смерти,
        // чтобы зажатая кнопка не перезапускала игру мгновенно)
        if (Time.unscaledTime >= inputBlockedUntil && InputHelper.ActionPressed())
            StartGame();
    }

    void StartGame()
    {
        Score = 0;
        Speed = startSpeed;
        spawner.ResetSpawner();
        ship.ResetShip();

        menuPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        scoreText.gameObject.SetActive(true);
        UpdateScoreText();

        State = GameState.Playing;
    }

    public void AddScore()
    {
        if (State != GameState.Playing) return;
        Score++;
        UpdateScoreText();
    }

    public void GameOver()
    {
        if (State != GameState.Playing) return;

        State = GameState.GameOver;
        Speed = 0f; // мир замирает

        if (Score > best)
        {
            best = Score;
            PlayerPrefs.SetInt(BestKey, best);
            PlayerPrefs.Save();
        }

        finalScoreText.text = $"Score: {Score}";
        bestScoreText.text = $"Best: {best}";
        scoreText.gameObject.SetActive(false);
        gameOverPanel.SetActive(true);

        inputBlockedUntil = Time.unscaledTime + 0.8f;
    }

    void UpdateScoreText() => scoreText.text = Score.ToString();
}