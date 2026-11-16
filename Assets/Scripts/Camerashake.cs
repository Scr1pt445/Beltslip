using UnityEngine;

// Тряска камеры при взрыве. Вешается на Main Camera.
public class CameraShake : MonoBehaviour
{
    static CameraShake instance;

    Vector3 basePosition;
    float duration;
    float timeLeft;
    float magnitude;

    void Awake()
    {
        instance = this;
        basePosition = transform.localPosition;
    }

    public static void Shake(float duration, float magnitude)
    {
        if (instance == null) return;
        instance.duration = duration;
        instance.timeLeft = duration;
        instance.magnitude = magnitude;
    }

    void LateUpdate()
    {
        if (timeLeft <= 0f)
        {
            transform.localPosition = basePosition;
            return;
        }

        timeLeft -= Time.deltaTime;
        float fade = timeLeft / duration;
        transform.localPosition = basePosition + Random.insideUnitSphere * magnitude * fade;
    }
}