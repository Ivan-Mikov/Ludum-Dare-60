using UnityEngine;

public class RicochetBullet : MonoBehaviour
{
    private Vector2 moveDirection;
    private float moveSpeed;

    // Точные границы экрана осциллографа из твоего скрипта Bullet
    private const float MinX = -5.32f;
    private const float MaxX = 1.44f;
    private const float MinY = -2.02f;
    private const float MaxY = 2.3f;

    public void Initialize(Vector2 direction, float speed)
    {
        moveDirection = direction.normalized;
        moveSpeed = speed;

        // Поворачиваем спрайт пули по направлению движения
        UpdateRotation();

        // Снаряды получают увеличенное время жизни на Стадии 3 (10 секунд)
        Destroy(gameObject, 10f);
    }

    void Update()
    {
        // Движение пули
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);

        // Проверка рикошета от границ экрана
        CheckWallCollisions();
    }

    private void CheckWallCollisions()
    {
        Vector2 pos = transform.position;

        // Отскок от левой или правой стены
        if (pos.x <= MinX && moveDirection.x < 0)
        {
            moveDirection = Vector2.Reflect(moveDirection, Vector2.right);
            UpdateRotation();
        }
        else if (pos.x >= MaxX && moveDirection.x > 0)
        {
            moveDirection = Vector2.Reflect(moveDirection, Vector2.left);
            UpdateRotation();
        }

        // Отскок от нижней или верхней стены
        if (pos.y <= MinY && moveDirection.y < 0)
        {
            moveDirection = Vector2.Reflect(moveDirection, Vector2.up);
            UpdateRotation();
        }
        else if (pos.y >= MaxY && moveDirection.y > 0)
        {
            moveDirection = Vector2.Reflect(moveDirection, Vector2.down);
            UpdateRotation();
        }
    }

    private void UpdateRotation()
    {
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    // Полная копия логики из твоего скрипта Bullet
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Проверяем, что попали во врага или фантома
        if (collision.CompareTag("Enemy") || collision.gameObject.layer == LayerMask.NameToLayer("Enemy") || collision.GetComponent<PhantomEnemy>() != null)
        {
            // Получаем текущую позицию врага, в которого попали
            Vector3 enemyPos = collision.transform.position;

            // Проверяем, зашел ли враг на экран хотя бы центром/краем
            bool isXOnScreen = enemyPos.x >= (MinX - 0.2f) && enemyPos.x <= (MaxX + 0.2f);
            bool isYOnScreen = enemyPos.y >= (MinY - 0.2f) && enemyPos.y <= (MaxY + 0.2f);

            if (isXOnScreen && isYOnScreen)
            {
                // ТОЛЬКО ЕСЛИ ВРАГ НА ЭКРАНЕ:
                Debug.Log("🎯 Враг на экране! Урон нанесен рикошетной пулей.");
                
                // Саму пулю здесь НЕ уничтожаем, давая скрипту врага обработать урон и самому уничтожить пулю, если это заложено в его логике.
            }
            else
            {
                // ИГНОРИРУЕМ: Враг еще за пределами зеленого экрана, урон не наносится
                Debug.Log("🛡️ Враг еще за экраном! Урон заблокирован.");
            }
        }
    }
}
