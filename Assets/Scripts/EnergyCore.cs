using System;
using UnityEngine;

// Собираемый энерго-куб: летит влево вместе с миром, плавно вращается,
// даёт очки (+1 в воротах — циановый, +2 между воротами — золотой) и возвращается в пул.
public class EnergyCore : MonoBehaviour
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    [SerializeField] float despawnX = -30f;
    [SerializeField] float spinSpeed = 120f;
    [SerializeField] float bobAmplitude = 0.2f;
    [SerializeField] float bobFrequency = 3f;

    Renderer cachedRenderer;
    MaterialPropertyBlock propBlock;
    Action<EnergyCore> onDespawn;
    int points = 1;
    float baseY;
    float phase;
    bool collected;

    public int BonusPoints => points;

    void Awake()
    {
        cachedRenderer = GetComponent<Renderer>();
        propBlock = new MaterialPropertyBlock();
    }

    public void Init(float y, int pointValue, Color baseColor, Color emissionColor, Action<EnergyCore> despawnCallback)
    {
        baseY = y;
        points = pointValue;
        phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        onDespawn = despawnCallback;
        collected = false;

        if (cachedRenderer != null)
        {
            propBlock.SetColor(BaseColorId, baseColor);
            propBlock.SetColor(EmissionColorId, emissionColor);
            cachedRenderer.SetPropertyBlock(propBlock);
        }
    }

    public int Collect()
    {
        if (collected) return 0;
        collected = true;
        onDespawn?.Invoke(this);
        return points;
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
