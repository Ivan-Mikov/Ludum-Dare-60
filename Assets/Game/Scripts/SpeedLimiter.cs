using UnityEngine;

public class SpeedLimiter : MonoBehaviour
{
    [Header("Ограничение скорости")]
    [SerializeField] private float maxSpeed = 8f; // Максимальная скорость, с которой может лететь объект

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        // Проверяем текущую скорость объекта
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            // Жестко обрезаем скорость до максимального лимита, сохраняя направление движения
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }
}