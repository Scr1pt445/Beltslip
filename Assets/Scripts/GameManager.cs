using System;
using UnityEngine;

public enum GameState { Menu, Playing, GameOver }

// Состояние игры, скорость мира и счёт. Про UI и звук не знает — сообщает событиями.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Скорость мира")]
    [SerializeField] float startSpeed = 7f;
    [SerializeField] float maxSpeed = 16f;
    [SerializeField] float speedGainPerSecond = 0.15f;

    [Header("Правила")]
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
    public float Difficulty01 => Mathf.InverseLerp(startSpeed, maxSpeed, Speed);

    HighScore highScore;
    float inputBlockedUntil;

    void Awake()
    {
        Instance = this;
        highScore = new HighScore();
    }

    void Start()
    {
        SetState(GameState.Menu);
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

        OnScoreChanged?.Invoke(Score);
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
