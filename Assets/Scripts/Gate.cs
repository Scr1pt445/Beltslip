using System;
using System.Collections.Generic;
using UnityEngine;

// Ворота из астероидов: едут влево со скоростью мира, крутят камни,
// дают очко, когда корабль их прошёл, и возвращаются в пул за экраном.
public class Gate : MonoBehaviour
{
    [SerializeField] float despawnX = -30f;

    Transform ship;
    Action<Gate> onDespawn;
    float oscAmplitude;
    float oscFrequency;
    float oscPhase;

    readonly List<Transform> rocks = new();
    readonly List<GameObject> rockPrefabs = new();
    readonly List<Vector3> spinAxes = new();
    readonly List<float> spinSpeeds = new();

    public int RockCount => rocks.Count;

    public void Init(Transform shipTransform, Action<Gate> despawnCallback)
    {
        ship = shipTransform;
        onDespawn = despawnCallback;
        oscAmplitude = 0f;
        oscFrequency = 0f;
        oscPhase = 0f;
    }

    public void SetOscillation(float amplitude, float frequency, float phase)
    {
        oscAmplitude = amplitude;
        oscFrequency = frequency;
        oscPhase = phase;
    }

    public void AddRock(Transform rock, GameObject sourcePrefab)
    {
        rocks.Add(rock);
        rockPrefabs.Add(sourcePrefab);
        spinAxes.Add(UnityEngine.Random.onUnitSphere);
        spinSpeeds.Add(UnityEngine.Random.Range(15f, 60f));
    }

    public void GetRockAt(int index, out GameObject rock, out GameObject sourcePrefab)
    {
        rock = rocks[index].gameObject;
        sourcePrefab = rockPrefabs[index];
    }

    public void ClearRocks()
    {
        rocks.Clear();
        rockPrefabs.Clear();
        spinAxes.Clear();
        spinSpeeds.Clear();
    }

    void Update()
    {
        var gm = GameManager.Instance;

        Vector3 pos = transform.position;
        pos.x -= gm.Speed * Time.deltaTime;
        if (oscAmplitude > 0f && gm.State == GameState.Playing)
            pos.y = Mathf.Sin(Time.time * oscFrequency + oscPhase) * oscAmplitude;
        transform.position = pos;

        for (int i = 0; i < rocks.Count; i++)
            rocks[i].Rotate(spinAxes[i], spinSpeeds[i] * Time.deltaTime, Space.World);

        if (transform.position.x < despawnX)
            onDespawn?.Invoke(this);
    }
}
