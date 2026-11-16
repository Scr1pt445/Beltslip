using System.Collections.Generic;
using UnityEngine;

// Ворота из астероидов: едут влево со скоростью мира, крутят камни,
// дают очко, когда корабль их прошёл, и удаляются за экраном.
public class Gate : MonoBehaviour
{
    const float DespawnX = -30f;

    Transform ship;
    bool scored;

    readonly List<Transform> rocks = new();
    readonly List<Vector3> spinAxes = new();
    readonly List<float> spinSpeeds = new();

    public void Init(Transform shipTransform) => ship = shipTransform;

    public void AddRock(Transform rock)
    {
        rocks.Add(rock);
        spinAxes.Add(Random.onUnitSphere);
        spinSpeeds.Add(Random.Range(15f, 60f));
    }

    void Update()
    {
        var gm = GameManager.Instance;

        transform.position += Vector3.left * gm.Speed * Time.deltaTime;

        for (int i = 0; i < rocks.Count; i++)
            rocks[i].Rotate(spinAxes[i], spinSpeeds[i] * Time.deltaTime, Space.World);

        if (!scored && gm.State == GameState.Playing && transform.position.x < ship.position.x)
        {
            scored = true;
            gm.AddScore();
        }

        if (transform.position.x < DespawnX)
            Destroy(gameObject);
    }
}