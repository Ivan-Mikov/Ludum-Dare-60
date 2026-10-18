using UnityEngine;

public class Bullet : MonoBehaviour
{
    private Vector2 moveDirection;
    private float moveSpeed;

    public void Initialize(Vector2 direction, float speed)
    {
        moveDirection = direction.normalized;
        moveSpeed = speed;

        // Поворачиваем спрайт пули по направлению движения
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // Уничтожаем пулю автоматически через 5 секунд после рождения
        Destroy(gameObject, 5f);
    }

    void Update()
    {
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Проверяем, что попали во врага или фантома
        if (collision.CompareTag("Enemy") || collision.gameObject.layer == LayerMask.NameToLayer("Enemy") || collision.GetComponent<PhantomEnemy>() != null)
        {
            // Границы экрана осциллографа (берем те же точные значения, что и для игрока/спавнера)
            float minX = -5.32f;
            float maxX = 1.44f;
            float minY = -2.02f;
            float maxY = 2.3f;

            // Получаем текущую позицию врага, в которого попали
            Vector3 enemyPos = collision.transform.position;

            // Проверяем, зашел ли враг на экран хотя бы центром/краем
            // Даем небольшую погрешность (например, 0.2f), чтобы урон засчитывался, как только край спрайта пересек рамку
            bool isXOnScreen = enemyPos.x >= (minX - 0.2f) && enemyPos.x <= (maxX + 0.2f);
            bool isYOnScreen = enemyPos.y >= (minY - 0.2f) && enemyPos.y <= (maxY + 0.2f);

            if (isXOnScreen && isYOnScreen)
            {
                // ТОЛЬКО ЕСЛИ ВРАГ НА ЭКРАНЕ:
                // Здесь пуля триггерит получение урона у врага (код зависит от вашего скрипта Enemy, например: collision.GetComponent<Enemy>().TakeDamage();)
                Debug.Log("🎯 Враг на экране! Урон нанесен.");
            }
            else
            {
                // ИГНОРИРУЕМ: Враг еще за пределами зеленого экрана, урон не наносится
                Debug.Log("🛡️ Враг еще за экраном! Урон заблокирован.");
            }
        }
    }

}