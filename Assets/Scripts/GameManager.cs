using System;
using UnityEngine;

public enum GameState { Menu, Playing, Paused, GameOver }

// Состояние игры, скорость мира и счёт. Про UI и звук не знает — сообщает событиями.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Настройки")]
    [SerializeField] DifficultySettings difficulty;
    [SerializeField] float restartDelay = 0.8f;

    [Header("Ссылки")]
    [SerializeField] ShipController ship;
    [SerializeField] ObstacleSpawner spawner;

    public event Action<GameState> OnStateChanged;
    public event Action<int> OnScoreChanged;

    public GameState State { get; private set; } = GameState.Menu;
    public float Speed { get; private set; }
    public int Score { get; private set; }
    public int BestScore => highScore != null ? highScore.Best : 0;

    // 0 на старте, 1 на максимальной скорости — от этого зависит сложность спавна
    public float Difficulty01 => Mathf.InverseLerp(StartSpeed, MaxSpeed, Speed);

    float StartSpeed => difficulty ? difficulty.StartSpeed : 7f;
    float MaxSpeed => difficulty ? difficulty.MaxSpeed : 16f;
    float SpeedGainPerSecond => difficulty ? difficulty.SpeedGainPerSecond : 0.15f;

    HighScore highScore;
    float inputBlockedUntil;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        highScore = new HighScore();
    }

    void Start()
    {
        SetState(GameState.Menu);
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && State == GameState.Playing)
            Pause();
    }

    void Update()
    {
        if (State == GameState.Playing)
        {
            if (InputHelper.PausePressed())
            {
                Pause();
                return;
            }

            Speed = Mathf.Min(MaxSpeed, Speed + SpeedGainPerSecond * Time.deltaTime);
            return;
        }

        if (State == GameState.Paused)
        {
            if (InputHelper.PausePressed())
                Resume();
            return;
        }

        // Меню или экран поражения: ждём нажатия (с небольшой паузой после смерти,
        // чтобы зажатая кнопка не перезапускала игру мгновенно)
        if (Time.unscaledTime >= inputBlockedUntil && InputHelper.ActionPressed())
            StartGame();
    }

    void StartGame()
    {
        Time.timeScale = 1f;
        Score = 0;
        Speed = StartSpeed;
        spawner.ResetSpawner();
        ship.ResetShip();

        OnScoreChanged?.Invoke(Score);
        SetState(GameState.Playing);
    }

    public void Pause()
    {
        if (State != GameState.Playing) return;
        Time.timeScale = 0f;
        SetState(GameState.Paused);
    }

    public void Resume()
    {
        if (State != GameState.Paused) return;
        Time.timeScale = 1f;
        SetState(GameState.Playing);
    }

    public void AddScore()
    {
        if (State != GameState.Playing) return;
        Score++;
        OnScoreChanged?.Invoke(Score);
    }

    public void GameOver()
    {
        if (State != GameState.Playing) return;

        Time.timeScale = 1f;
        Speed = 0f; // мир замирает
        highScore.TrySet(Score);
        inputBlockedUntil = Time.unscaledTime + restartDelay;

        SetState(GameState.GameOver);
    }

    void SetState(GameState state)
    {
        State = state;
        OnStateChanged?.Invoke(state);
    }
}
