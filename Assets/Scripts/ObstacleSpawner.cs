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
        if (gm.State != GameState.Playing) return;

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
        if (GameManager.Instance.Score >= 4 && Random.value < Mathf.Lerp(0.15f, 0.45f, difficulty01))
            gate.SetOscillation(Random.Range(0.55f, 1.2f), Random.Range(1.4f, 2.2f), Random.Range(0f, Mathf.PI * 2f));
        activeGates.Add(gate);

        FillColumn(gate, fieldBottom - rockSize, center - half); // нижняя колонна
        FillColumn(gate, center + half, fieldTop + rockSize);    // верхняя колонна

        if (corePool != null && Random.value < 0.55f)
        {
            bool insideGate = Random.value < 0.5f;
            float coreX = insideGate ? spawnX : spawnX + spacing * 0.5f;
            float coreY = insideGate
                ? center + Random.Range(-half * 0.45f, half * 0.45f)
                : Random.Range(fieldBottom + edgeMargin + 1f, fieldTop - edgeMargin - 1f);
            var core = corePool.Get();
            core.transform.position = new Vector3(coreX, coreY, 0f);
            core.Init(coreY, DespawnCore);
            activeCores.Add(core);
        }

        if (roguePool != null && GameManager.Instance.Score >= 8 && Random.value < Mathf.Lerp(0.15f, 0.35f, difficulty01))
        {
            float rogueY = Random.Range(fieldBottom + edgeMargin + 1.2f, fieldTop - edgeMargin - 1.2f);
            var rogue = roguePool.Get();
            rogue.transform.position = new Vector3(spawnX + spacing * 0.55f, rogueY, 0f);
            rogue.transform.localScale = Vector3.one * (rockSize * Random.Range(0.75f, 1.05f));
            rogue.Init(Random.Range(-0.6f, 0.6f), DespawnRogue);
            activeRogues.Add(rogue);
        }
    }

    // Укладывает астероиды снизу вверх; края колонн точно прилегают к проходу
    void FillColumn(Gate gate, float fromY, float toY)
    {
        float y = fromY;
        while (y < toY)
        {
            float size = rockSize * Random.Range(1f - sizeJitter, 1f + sizeJitter);
            float radius = size * 0.5f;
            bool last = y + size >= toY;
            float centerY = last ? toY - radius : y + radius;

            SpawnRock(gate, centerY, size);

            if (last) break;
            y = centerY + radius * 0.75f; // лёгкое перекрытие, чтобы не было щелей
        }
    }

    void SpawnRock(Gate gate, float y, float size)
    {
        var prefab = asteroidPrefabs[Random.Range(0, asteroidPrefabs.Length)];
        var rock = rockPools[prefab].Get();
        rock.transform.SetParent(gate.transform, false);
        rock.transform.localPosition = new Vector3(Random.Range(-0.4f, 0.4f), y, Random.Range(-0.3f, 0.3f));
        rock.transform.localRotation = Random.rotation;
        rock.transform.localScale = Vector3.one * size;
        gate.AddRock(rock.transform, prefab);
    }

    void DespawnGate(Gate gate)
    {
        activeGates.Remove(gate);
        gatePool.Release(gate);
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
