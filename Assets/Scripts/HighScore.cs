using UnityEngine;

public interface IScoreStorage
{
    int GetInt(string key, int defaultValue);
    void SetInt(string key, int value);
    void Save();
}

public class PlayerPrefsScoreStorage : IScoreStorage
{
    public int GetInt(string key, int defaultValue) => PlayerPrefs.GetInt(key, defaultValue);
    public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);
    public void Save() => PlayerPrefs.Save();
}

// Хранит и обновляет лучший результат без привязки к MonoBehaviour и UI.
public class HighScore
{
    const string BestKey = "BestScore";

    readonly IScoreStorage storage;

    public int Best { get; private set; }

    public HighScore(IScoreStorage storage = null)
    {
        this.storage = storage ?? new PlayerPrefsScoreStorage();
        Best = this.storage.GetInt(BestKey, 0);
    }

    public bool TrySet(int score)
    {
        if (score <= Best) return false;

        Best = score;
        storage.SetInt(BestKey, Best);
        storage.Save();
        return true;
    }
}
