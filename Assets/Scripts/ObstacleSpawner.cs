using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

// Создаёт «ворота» через пул объектов: две колонны астероидов с проходом между ними.
// С ростом скорости проход сужается, ворота идут чаще, проход сильнее «гуляет» по высоте.
public class ObstacleSpawner : MonoBehaviour
{
    [SerializeField] Gate gatePrefab;
    [SerializeField] GameObject[] asteroidPrefabs;
    [SerializeField] Transform ship;

    [Header("Поле")]
    [SerializeField] float spawnX = 24f;
    [SerializeField] float fieldBottom = -6.5f;
    [SerializeField] float fieldTop = 6.5f;
    [SerializeField] float edgeMargin = 1f; // минимум между краем поля и проходом

    [Header("Сложность: старт -> максимум")]
    [SerializeField] float startGap = 4.5f;
    [SerializeField] float minGap = 2.8f;
    [SerializeField] float startSpacing = 11f;   // расстояние между воротами
    [SerializeField] float minSpacing = 9f;
    [SerializeField] float startMaxShift = 2f;   // насколько проход может сместиться от предыдущего
    [SerializeField] float endMaxShift = 3.5f;

    [Header("Астероиды")]
    [SerializeField] float rockSize = 1.5f;
    [SerializeField, Range(0f, 0.5f)] float sizeJitter = 0.3f;

    ObjectPool<Gate> gatePool;
    readonly Dictionary<GameObject, ObjectPool<GameObject>> rockPools = new();
    readonly List<Gate> activeGates = new();

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
    }

    public void ResetSpawner()
    {
        for (int i = activeGates.Count - 1; i >= 0; i--)
            DespawnGate(activeGates[i]);

        distanceSinceLast = 0f;
        lastGapCenter = 0f;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm.State != GameState.Playing) return;

        distanceSinceLast += gm.Speed * Time.deltaTime;
        float spacing = GateSpawnLogic.GetSpacing(startSpacing, minSpacing, gm.Difficulty01);
        if (distanceSinceLast >= spacing)
        {
            distanceSinceLast -= spacing;
            SpawnGate(gm.Difficulty01);
        }
    }

    void SpawnGate(float difficulty)
    {
        float gap = GateSpawnLogic.GetGap(startGap, minGap, difficulty);
        float half = gap * 0.5f;
        float center = GateSpawnLogic.GetNextGapCenter(
            lastGapCenter,
            Random.Range(-1f, 1f),
            startMaxShift,
            endMaxShift,
            difficulty,
            gap,
            fieldBottom,
            fieldTop,
            edgeMargin);
        lastGapCenter = center;

        var gate = gatePool.Get();
        gate.transform.position = new Vector3(spawnX, 0f, 0f);
        gate.Init(ship, DespawnGate);
        activeGates.Add(gate);

        FillColumn(gate, fieldBottom - rockSize, center - half); // нижняя колонна
        FillColumn(gate, center + half, fieldTop + rockSize);    // верхняя колонна
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
