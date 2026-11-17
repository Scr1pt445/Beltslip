using UnityEngine;

// Звуки игры и гул двигателя по событиям GameManager. Игровая логика про звук не знает.
public class GameAudio : MonoBehaviour
{
    [Header("Источники")]
    [SerializeField] AudioSource sfxSource;
    [SerializeField] AudioSource engineSource;

    [Header("Клипы")]
    [SerializeField] AudioClip scoreClip;
    [SerializeField] AudioClip explosionClip;
    [SerializeField] AudioClip clickClip;
    [SerializeField] AudioClip engineClip;

    [Header("Двигатель")]
    [SerializeField] float idleVolume = 0.15f;
    [SerializeField] float thrustVolume = 0.45f;
    [SerializeField] float idlePitch = 0.9f;
    [SerializeField] float thrustPitch = 1.15f;
    [SerializeField] float volumeSmooth = 10f;

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
        HandleStateChanged(gm.State);
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || engineSource == null || gm.State != GameState.Playing) return;

        bool thrusting = InputHelper.ThrustHeld();
        float targetVol = thrusting ? thrustVolume : idleVolume;
        float targetPitch = thrusting ? thrustPitch : idlePitch;

        engineSource.volume = Mathf.MoveTowards(engineSource.volume, targetVol, volumeSmooth * Time.deltaTime);
        engineSource.pitch = Mathf.MoveTowards(engineSource.pitch, targetPitch, volumeSmooth * Time.deltaTime);
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
        if (score > 0 && sfxSource && scoreClip)
            sfxSource.PlayOneShot(scoreClip);
    }

    void HandleStateChanged(GameState state)
    {
        if (state == GameState.Playing)
        {
            if (sfxSource && clickClip)
                sfxSource.PlayOneShot(clickClip);

            if (engineSource && engineClip)
            {
                engineSource.clip = engineClip;
                engineSource.loop = true;
                engineSource.volume = idleVolume;
                engineSource.pitch = idlePitch;
                if (!engineSource.isPlaying)
                    engineSource.Play();
            }
            return;
        }

        if (engineSource && engineSource.isPlaying)
            engineSource.Stop();

        if (state == GameState.GameOver && sfxSource && explosionClip)
            sfxSource.PlayOneShot(explosionClip);
    }
}
