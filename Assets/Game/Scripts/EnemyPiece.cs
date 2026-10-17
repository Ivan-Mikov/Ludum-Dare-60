using UnityEngine;

public class EnemyPiece : MonoBehaviour
{
    [Header("Настройки времени жизни")]
    [SerializeField] private float lifetime = 1.5f; // Живут чуть меньше, чтобы не мусорить
    [SerializeField] private float fadeSpeed = 2f; 

    private SpriteRenderer spriteRenderer;
    private float timer;
    private Vector3 moveDirection;
    private float rotationSpeed;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Этот метод мы вызовем из скрипта врага сразу после спавна
    public void InitializePiece(Vector3 direction, float speed)
    {
        moveDirection = direction * speed;
        rotationSpeed = Random.Range(-200f, 200f); // Случайная скорость вращения
        timer = lifetime;
    }

    void Update()
    {
        // 1. Двигаем осколок чисто по координатам
        transform.position += moveDirection * Time.deltaTime;

        // 2. Вращаем его вокруг своей оси
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);

        // 3. Плавно растворяем в воздухе
        timer -= Time.deltaTime;
        if (timer <= 0 && spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a -= fadeSpeed * Time.deltaTime;
            spriteRenderer.color = color;
            
            if (color.a <= 0) Destroy(gameObject);
        }
    }
}