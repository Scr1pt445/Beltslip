using UnityEngine;

// Создаёт «ворота»: две колонны астероидов с проходом между ними.
// С ростом скорости проход сужается, ворота идут чаще, проход сильнее «гуляет» по высоте.
public class ObstacleSpawner : MonoBehaviour
{
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

    float distanceSinceLast;
    float lastGapCenter;

    public void ResetSpawner()
    {
        foreach (var gate in FindObjectsByType<Gate>(FindObjectsSortMode.None))
            Destroy(gate.gameObject);
        distanceSinceLast = 0f;
        lastGapCenter = 0f;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm.State != GameState.Playing) return;

        distanceSinceLast += gm.Speed * Time.deltaTime;
        float spacing = Mathf.Lerp(startSpacing, minSpacing, gm.Difficulty01);
        if (distanceSinceLast >= spacing)
        {
            distanceSinceLast -= spacing;
            SpawnGate(gm.Difficulty01);
        }
    }

    void SpawnGate(float difficulty)
    {
        float gap = Mathf.Lerp(startGap, minGap, difficulty);
        float half = gap * 0.5f;
        float shift = Mathf.Lerp(startMaxShift, endMaxShift, difficulty);

        float center = Mathf.Clamp(
            lastGapCenter + Random.Range(-shift, shift),
            fieldBottom + edgeMargin + half,
            fieldTop - edgeMargin - half);
        lastGapCenter = center;

        var gateObject = new GameObject("Gate");
        gateObject.transform.position = new Vector3(spawnX, 0f, 0f);
        var gate = gateObject.AddComponent<Gate>();
        gate.Init(ship);

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
        Vector3 pos = gate.transform.position + new Vector3(Random.Range(-0.4f, 0.4f), y, Random.Range(-0.3f, 0.3f));
        var rock = Instantiate(prefab, pos, Random.rotation, gate.transform);
        rock.transform.localScale = Vector3.one * size;
        gate.AddRock(rock.transform);
    }
}