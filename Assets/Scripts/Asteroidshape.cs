using UnityEngine;

// Превращает стандартную сферу в неровный камень: смещает вершины шумом.
// Каждый экземпляр получает свою форму. Коллайдер остаётся сферой.
[RequireComponent(typeof(MeshFilter))]
public class AsteroidShape : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] float roughness = 0.45f;
    [SerializeField] float noiseScale = 2.2f;

    Mesh mesh;

    void Awake()
    {
        var meshFilter = GetComponent<MeshFilter>();
        mesh = Instantiate(meshFilter.sharedMesh);

        Vector3[] vertices = mesh.vertices;
        Vector3 seed = Random.insideUnitSphere * 100f;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 dir = vertices[i].normalized;
            float n = Noise3D(dir * noiseScale + seed);           // ~0..1
            float radius = 0.5f * (1f + (n - 0.5f) * 2f * roughness);
            vertices[i] = dir * radius;
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        meshFilter.mesh = mesh;
    }

    static float Noise3D(Vector3 p)
    {
        return (Mathf.PerlinNoise(p.x, p.y) + Mathf.PerlinNoise(p.y, p.z) + Mathf.PerlinNoise(p.z, p.x)) / 3f;
    }

    void OnDestroy()
    {
        if (mesh) Destroy(mesh);
    }
}