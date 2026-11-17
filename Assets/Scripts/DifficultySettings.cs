using UnityEngine;

// Настройки скорости мира и параметров генерации ворот, вынесенные из логики в ассет.
[CreateAssetMenu(fileName = "DifficultySettings", menuName = "Beltslip/Difficulty Settings")]
public class DifficultySettings : ScriptableObject
{
    [Header("Скорость мира")]
    [SerializeField] float startSpeed = 7f;
    [SerializeField] float maxSpeed = 16f;
    [SerializeField] float speedGainPerSecond = 0.15f;

    [Header("Проход и шаг ворот")]
    [SerializeField] float startGap = 4.5f;
    [SerializeField] float minGap = 2.8f;
    [SerializeField] float startSpacing = 11f;
    [SerializeField] float minSpacing = 9f;
    [SerializeField] float startMaxShift = 2f;
    [SerializeField] float endMaxShift = 3.5f;

    public float StartSpeed => startSpeed;
    public float MaxSpeed => maxSpeed;
    public float SpeedGainPerSecond => speedGainPerSecond;
    public float StartGap => startGap;
    public float MinGap => minGap;
    public float StartSpacing => startSpacing;
    public float MinSpacing => minSpacing;
    public float StartMaxShift => startMaxShift;
    public float EndMaxShift => endMaxShift;
}
