using UnityEngine;

public class OscilloscopePlayer : MonoBehaviour
{
    [Header("Настройки физики (в пикселях)")]
    public float gravity = -1200f;     
    public float jumpVelocity = 350f;  
    public float maxVelocity = 400f;   

    private RectTransform rectTransform;
    private RectTransform parentRect;
    private float velocityY = 0f;
    private float halfHeight;
    private float parentHalfHeight;
    private bool isDead = false;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        parentRect = rectTransform.parent.GetComponent<RectTransform>();
        
        halfHeight = rectTransform.rect.height / 2f;
        parentHalfHeight = parentRect.rect.height / 2f;
        isDead = false;
    }

    void Update()
    {
        if (isDead) return;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            velocityY = jumpVelocity; 
        }

        velocityY += gravity * Time.deltaTime;
        velocityY = Mathf.Clamp(velocityY, -maxVelocity, maxVelocity);

        Vector3 newPosition = rectTransform.localPosition;
        newPosition.y += velocityY * Time.deltaTime;

        // Границы экрана
        float minY = -parentHalfHeight + halfHeight;
        float maxY = parentHalfHeight - halfHeight;

        if (newPosition.y < minY) { newPosition.y = minY; velocityY = 0f; }
        else if (newPosition.y > maxY) { newPosition.y = maxY; velocityY = 0f; }

        rectTransform.localPosition = newPosition;
    }

    // НАСТОЯЩИЙ ФИЗИЧЕСКИЙ ТРИГГЕР UNITY
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Проверяем, что врезались в объект с нужным тегом
        if (collision.gameObject.CompareTag("Obstacle") || collision.transform.parent.CompareTag("Obstacle"))
        {
            isDead = true;
            Debug.Log("БАХ! Система перегружена, прибор отключён!");
        }
    }
}