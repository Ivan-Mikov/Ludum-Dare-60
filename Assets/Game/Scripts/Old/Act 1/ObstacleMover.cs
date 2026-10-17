using UnityEngine;

public class ObstacleMover : MonoBehaviour
{
    public float speed = 200f;       
    public float destroyX = -500f;   

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Задаем физическую скорость влево прямо при старте
        rb.linearVelocity = new Vector2(-speed, 0f);
    }

    void Update()
    {
        // Проверяем удаление
        if (transform.localPosition.x <= destroyX)
        {
            Destroy(gameObject);
        }
    }
}