using UnityEngine;

// Корабль: тяга вверх при удержании, снос вниз без неё, наклон модели по скорости,
// парение в меню, взрыв при столкновении.
[RequireComponent(typeof(Rigidbody))]
public class ShipController : MonoBehaviour
{
    [Header("Движение")]
    [SerializeField] float thrust = 30f;
    [SerializeField] float gravity = 20f;
    [SerializeField] float maxVerticalSpeed = 9f;
    [SerializeField] float minY = -5f;
    [SerializeField] float maxY = 5f;

    [Header("Визуал")]
    [SerializeField] Transform model;           // дочерний объект с мешами, его наклоняем
    [SerializeField] float maxPitch = 25f;      // нос вверх/вниз
    [SerializeField] float maxRoll = 15f;       // крен для объёма
    [SerializeField] float rotationSmooth = 10f;
    [SerializeField] ParticleSystem engineFx;   // пламя двигателя (дочерний к Ship, НЕ к Model)
    [SerializeField] GameObject explosionPrefab;

    [Header("Тряска камеры")]
    [SerializeField] float shakeDuration = 0.35f;
    [SerializeField] float shakeMagnitude = 0.4f;

    Vector3 startPos;
    float verticalSpeed;
    bool alive = true;

    void Awake()
    {
        startPos = transform.position;
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    public void ResetShip()
    {
        transform.position = startPos;
        verticalSpeed = 0f;
        alive = true;
        model.gameObject.SetActive(true);
        model.localRotation = Quaternion.identity;
        if (engineFx) engineFx.Play();
    }

    void Update()
    {
        var gm = GameManager.Instance;

        if (gm.State == GameState.Menu)
        {
            Hover();
            return;
        }

        if (!alive || gm.State != GameState.Playing) return;

        bool thrusting = InputHelper.ThrustHeld();
        float accel = thrusting ? thrust : -gravity;
        verticalSpeed = Mathf.Clamp(verticalSpeed + accel * Time.deltaTime, -maxVerticalSpeed, maxVerticalSpeed);

        if (engineFx)
        {
            var emission = engineFx.emission;
            emission.rateOverTime = thrusting ? 120f : 40f;
        }

        Vector3 pos = transform.position;
        pos.y += verticalSpeed * Time.deltaTime;
        if (pos.y < minY || pos.y > maxY)
        {
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            verticalSpeed = 0f;
        }
        transform.position = pos;

        // Наклон: чем быстрее летим вверх/вниз, тем сильнее задираем/опускаем нос
        float t = verticalSpeed / maxVerticalSpeed;
        Quaternion target = Quaternion.Euler(-t * maxRoll, 0f, t * maxPitch);
        model.localRotation = Quaternion.Slerp(model.localRotation, target, rotationSmooth * Time.deltaTime);
    }

    void Hover()
    {
        Vector3 pos = startPos;
        pos.y += Mathf.Sin(Time.time * 2f) * 0.3f;
        transform.position = pos;
        model.localRotation = Quaternion.Euler(0f, 0f, Mathf.Cos(Time.time * 2f) * 5f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!alive || GameManager.Instance.State != GameState.Playing) return;
        Die();
    }

    void Die()
    {
        alive = false;
        if (explosionPrefab) Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        model.gameObject.SetActive(false);
        if (engineFx) engineFx.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        CameraShake.Shake(shakeDuration, shakeMagnitude);
        GameManager.Instance.GameOver();
    }
}
