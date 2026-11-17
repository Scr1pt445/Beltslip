using System;
using UnityEngine;

// Одиночный быстрый «бродячий» астероид между воротами на высокой сложности.
public class RogueAsteroid : MonoBehaviour
{
    [SerializeField] float despawnX = -30f;
    [SerializeField] float speedMultiplier = 1.35f;

    Action<RogueAsteroid> onDespawn;
    Vector3 spinAxis;
    float spinSpeed;
    float verticalDrift;

    public void Init(float driftY, Action<RogueAsteroid> despawnCallback)
    {
        verticalDrift = driftY;
        onDespawn = despawnCallback;
        spinAxis = UnityEngine.Random.onUnitSphere;
        spinSpeed = UnityEngine.Random.Range(90f, 180f);
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        Vector3 pos = transform.position;
        pos.x -= gm.Speed * speedMultiplier * Time.deltaTime;
        pos.y += verticalDrift * Time.deltaTime;
        transform.position = pos;

        transform.Rotate(spinAxis, spinSpeed * Time.deltaTime, Space.World);

        if (pos.x < despawnX)
            onDespawn?.Invoke(this);
    }
}
