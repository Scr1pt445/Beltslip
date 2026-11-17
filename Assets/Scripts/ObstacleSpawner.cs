using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

// Создаёт «ворота» через пул объектов: две колонны астероидов с проходом между ними.
// С ростом скорости проход сужается, ворота идут чаще, проход сильнее «гуляет» по высоте.
public class ObstacleSpawner : MonoBehaviour
{
    [SerializeField] Gate gatePrefab;
    [SerializeField] GameObject[] asteroidPrefabs;
    [SerializeField] EnergyCore energyCorePrefab;
    [SerializeField] RogueAsteroid rogueAsteroidPrefab;
    [SerializeField] Transform ship;
    [SerializeField] DifficultySettings difficulty;

    [Header("Поле")]
    [SerializeField] float spawnX = 24f;
    [SerializeField] float fieldBottom = -6.5f;
    [SerializeField] float fieldTop = 6.5f;
    [SerializeField] float edgeMargin = 1f; // минимум между краем поля и проходом

    [Header("Астероиды")]
    [SerializeField] float rockSize = 1.5f;
    [SerializeField, Range(0f, 0.5f)] float sizeJitter = 0.3f;

    ObjectPool<Gate> gatePool;
    ObjectPool<EnergyCore> corePool;
    ObjectPool<RogueAsteroid> roguePool;
    readonly Dictionary<GameObject, ObjectPool<GameObject>> rockPools = new();
    readonly List<Gate> activeGates = new();
    readonly List<EnergyCore> activeCores = new();
    readonly List<RogueAsteroid> activeRogues = new();

    float distanceSinceLast;
    float lastGapCenter;

    void Awake()
    {
        gatePool = new ObjectPool<Gate>(
            createFunc: () => Instantiate(gatePrefab, transform),
            actionOnGet: gate => gate.gameObject.SetActive(true),
            actionOnRelease: ReleaseGateContents,
            actionOnDestroy: gate => { if (gate) Destroy(gate.gameObject); },
            collectionCheck: false,
            defaultCapacity: 8,
            maxSize: 16);

        for (int i = 0; i < asteroidPrefabs.Length; i++)
        {
            var prefab = asteroidPrefabs[i];
            if (prefab == null || rockPools.ContainsKey(prefab)) continue;

            rockPools[prefab] = new ObjectPool<GameObject>(
                createFunc: () => Instantiate(prefab, transform),
                actionOnGet: rock => rock.SetActive(true),
                actionOnRelease: rock =>
                {
                    rock.SetActive(false);
                    rock.transform.SetParent(transform, false);
                },
                actionOnDestroy: rock => { if (rock) Destroy(rock); },
                collectionCheck: false,
                defaultCapacity: 20,
                maxSize: 64);
        }

        if (energyCorePrefab != null)
        {
            corePool = new ObjectPool<EnergyCore>(
                createFunc: () => Instantiate(energyCorePrefab, transform),
                actionOnGet: core => core.gameObject.SetActive(true),
                actionOnRelease: core => core.gameObject.SetActive(false),
                actionOnDestroy: core => { if (core) Destroy(core.gameObject); },
                collectionCheck: false,
                defaultCapacity: 4,
                maxSize: 12);
        }

        if (rogueAsteroidPrefab != null)
        {
            roguePool = new ObjectPool<RogueAsteroid>(
                createFunc: () => Instantiate(rogueAsteroidPrefab, transform),
                actionOnGet: rock => rock.gameObject.SetActive(true),
                actionOnRelease: rock => rock.gameObject.SetActive(false),
                actionOnDestroy: rock => { if (rock) Destroy(rock.gameObject); },
                collectionCheck: false,
                defaultCapacity: 4,
                maxSize: 12);
        }
    }

    public void ResetSpawner()
    {
        for (int i = activeGates.Count - 1; i >= 0; i--)
            DespawnGate(activeGates[i]);

        for (int i = activeCores.Count - 1; i >= 0; i--)
            DespawnCore(activeCores[i]);

        for (int i = activeRogues.Count - 1; i >= 0; i--)
            DespawnRogue(activeRogues[i]);

        distanceSinceLast = 0f;
        lastGapCenter = 0f;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;

        distanceSinceLast += gm.Speed * Time.deltaTime;
        float startSpacing = difficulty ? difficulty.StartSpacing : 11f;
        float minSpacing = difficulty ? difficulty.MinSpacing : 9f;
        float spacing = GateSpawnLogic.GetSpacing(startSpacing, minSpacing, gm.Difficulty01);
        if (distanceSinceLast >= spacing)
        {
            distanceSinceLast -= spacing;
            SpawnGate(gm.Difficulty01, spacing);
        }
    }

    void SpawnGate(float difficulty01, float spacing)
    {
        float startGap = difficulty ? difficulty.StartGap : 4.5f;
        float minGap = difficulty ? difficulty.MinGap : 2.8f;
        float startMaxShift = difficulty ? difficulty.StartMaxShift : 2f;
        float endMaxShift = difficulty ? difficulty.EndMaxShift : 3.5f;

        float gap = GateSpawnLogic.GetGap(startGap, minGap, difficulty01);
        float half = gap * 0.5f;
        float center = GateSpawnLogic.GetNextGapCenter(
            lastGapCenter,
            Random.Range(-1f, 1f),
            startMaxShift,
            endMaxShift,
            difficulty01,
            gap,
            fieldBottom,
            fieldTop,
            edgeMargin);
        lastGapCenter = center;

        var gate = gatePool.Get();
        gate.transform.position = new Vector3(spawnX, 0f, 0f);
        gate.Init(ship, DespawnGate);

        float oscAmp = 0f;
        if (GameManager.Instance.Score >= 6 && Random.value < Mathf.Lerp(0.15f, 0.35f, difficulty01))
        {
            float maxSafeAmp = Mathf.Min(
                (center - half) - (fieldBottom + edgeMargin),
                (fieldTop - edgeMargin) - (center + half)
            );
            if (maxSafeAmp >= 0.45f)
            {
                oscAmp = Mathf.Min(Random.Range(0.45f, 0.85f), maxSafeAmp);
                gate.SetOscillation(oscAmp, Random.Range(1.0f, 1.5f), Random.Range(0f, Mathf.PI * 2f));
            }
        }
        activeGates.Add(gate);

        float colPad = rockSize + oscAmp + 1.5f;
        FillColumn(gate, fieldBottom - colPad, center - half); // нижняя колонна
        FillColumn(gate, center + half, fieldTop + colPad);    // верхняя колонна

        if (corePool != null)
        {
            // +1 циановый куб в проёме ворот
            float gateCoreY = center + Random.Range(-half * 0.35f, half * 0.35f);
            SpawnCore(spawnX, gateCoreY, 1, new Color(0.2f, 0.95f, 1f, 1f), new Color(0.2f, 1.5f, 1.8f, 1f));

            // +2 золотой куб между воротами (с шансом 45%)
            bool spawnedBonus = Random.value < 0.45f;
            if (spawnedBonus)
            {
                float bonusY = Mathf.Clamp(
                    center + Random.Range(-2.5f, 2.5f),
                    fieldBottom + edgeMargin + 1f,
                    fieldTop - edgeMargin - 1f);
                SpawnCore(spawnX + spacing * 0.5f, bonusY, 2, new Color(1f, 0.8f, 0.2f, 1f), new Color(1.8f, 1.2f, 0.15f, 1f));
            }
            else if (roguePool != null && GameManager.Instance.Score >= 12 && Random.value < Mathf.Lerp(0.12f, 0.25f, difficulty01))
            {
                float rogueY = Random.Range(fieldBottom + edgeMargin + 1.2f, fieldTop - edgeMargin - 1.2f);
                var rogue = roguePool.Get();
                rogue.transform.position = new Vector3(spawnX + spacing * 0.5f, rogueY, 0f);
                rogue.transform.localScale = Vector3.one * (rockSize * Random.Range(0.55f, 0.75f));
                rogue.Init(Random.Range(-0.25f, 0.25f), DespawnRogue);
                activeRogues.Add(rogue);
            }
        }
    }

    // Укладывает астероиды снизу вверх без нахлёста друг в друга; края колонн точно прилегают к проходу
    void FillColumn(Gate gate, float fromY, float toY)
    {
        float y = fromY;
        while (y < toY)
        {
            float remaining = toY - y;
            if (remaining < rockSize * 0.4f) break;

            float size = rockSize * Random.Range(1f - sizeJitter, 1f + sizeJitter);
            bool last = remaining <= size * 1.15f;
            if (last) size = Mathf.Clamp(remaining, rockSize * 0.55f, rockSize * (1f + sizeJitter));

            float radius = size * 0.5f;
            float centerY = last ? toY - radius : y + radius;

            SpawnRock(gate, centerY, size);

            if (last) break;
            y = centerY + radius * 0.98f;
        }
    }

    void SpawnRock(Gate gate, float y, float size)
    {
        var prefab = asteroidPrefabs[Random.Range(0, asteroidPrefabs.Length)];
        var rock = rockPools[prefab].Get();
        rock.transform.SetParent(gate.transform, false);
        rock.transform.localPosition = new Vector3(Random.Range(-0.18f, 0.18f), y, Random.Range(-0.08f, 0.08f));
        rock.transform.localRotation = Random.rotation;
        rock.transform.localScale = Vector3.one * size;
        gate.AddRock(rock.transform, prefab);
    }

    void DespawnGate(Gate gate)
    {
        activeGates.Remove(gate);
        gatePool.Release(gate);
    }

    void SpawnCore(float x, float y, int points, Color baseColor, Color emissionColor)
    {
        var core = corePool.Get();
        core.transform.position = new Vector3(x, y, 0f);
        core.Init(y, points, baseColor, emissionColor, DespawnCore);
        activeCores.Add(core);
    }

    void DespawnCore(EnergyCore core)
    {
        if (activeCores.Remove(core))
            corePool.Release(core);
    }

    void DespawnRogue(RogueAsteroid rogue)
    {
        if (activeRogues.Remove(rogue))
            roguePool.Release(rogue);
    }

    void ReleaseGateContents(Gate gate)
    {
        for (int i = 0; i < gate.RockCount; i++)
        {
            gate.GetRockAt(i, out var rock, out var sourcePrefab);
            rockPools[sourcePrefab].Release(rock);
        }
        gate.ClearRocks();
        gate.gameObject.SetActive(false);
    }
}
