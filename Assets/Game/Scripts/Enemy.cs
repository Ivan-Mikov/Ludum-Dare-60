using UnityEngine;

public class EnemyCircle : MonoBehaviour
{
    [Header("Движение и преследование")]
    [SerializeField] private float speed = 2f; // Четкая фиксированная скорость врага

    private Rigidbody2D rb;
    private Transform playerTransform;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        PointDrawer playerScript = Object.FindFirstObjectByType<PointDrawer>();
        if (playerScript != null)
        {
            playerTransform = playerScript.transform;
        }
    }

    void FixedUpdate()
    {
        if (playerTransform != null && rb != null)
        {
            // 1. Вычисляем вектор расстояния до игрока
            Vector2 heading = (Vector2)playerTransform.position - rb.position;

            // 2. ЖЕСТКАЯ НОРМАЛИЗАЦИЯ: получаем чистый вектор направления длиной ровно 1
            Vector2 direction = heading.normalized;

            // Если враг уже идеально на игроке, обнуляем скорость во избежание дерганий
            if (heading.sqrMagnitude < 0.01f)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            // 3. Принудительно задаем скорость. Теперь она СТРОГО константна
            rb.linearVelocity = direction * speed;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // КОНТАКТ С ИГРОКОМ: Проигрыш
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player") || collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("💀 ИГРОК ПРОИГРАЛ! Вас поймал красный круг.");
            return;
        }

        // КОНТАКТ С НАРИСОВАННЫМ ОБЪЕКТОМ: Смерть врага
        if (collision.gameObject.layer == LayerMask.NameToLayer("DrawnObjects") || collision.gameObject.GetComponent<PolygonCollider2D>() != null)
        {
            Debug.Log("💥 Хахахах лох! Враг уничтожен нарисованным объектом.");
            Destroy(gameObject);
        }
    }
}