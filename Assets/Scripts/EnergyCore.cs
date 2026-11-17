using System;
using UnityEngine;

// Собираемый энерго-кристалл: летит влево вместе с миром, плавно вращается,
// даёт бонусные очки при сборе и возвращается в пул.
public class EnergyCore : MonoBehaviour
{
    [SerializeField] float despawnX = -30f;
    [SerializeField] int bonusPoints = 2;
    [SerializeField] float spinSpeed = 120f;
    [SerializeField] float bobAmplitude = 0.25f;
    [SerializeField] float bobFrequency = 3f;

    Action<EnergyCore> onDespawn;
    float baseY;
    float phase;
    bool collected;

    public int BonusPoints => bonusPoints;

    public void Init(float y, Action<EnergyCore> despawnCallback)
    {
        baseY = y;
        phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        onDespawn = despawnCallback;
        collected = false;
    }

    public int Collect()
    {
        if (collected) return 0;
        collected = true;
        onDespawn?.Invoke(this);
        return bonusPoints;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        Vector3 pos = transform.position;
        pos.x -= gm.Speed * Time.deltaTime;
        pos.y = baseY + Mathf.Sin(Time.time * bobFrequency + phase) * bobAmplitude;
        transform.position = pos;

        transform.Rotate(0.4f * spinSpeed * Time.deltaTime, spinSpeed * Time.deltaTime, 0f, Space.World);

        if (pos.x < despawnX)
            onDespawn?.Invoke(this);
    }
}
