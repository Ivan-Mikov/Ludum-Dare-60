using UnityEngine;

public class EnemyCircle : MonoBehaviour
{
    [Header("Движение и преследование")]
    [SerializeField] private float speed = 2f; 

    [Header("Эффект разрушения")]
    [SerializeField] private GameObject[] piecePrefabs; 
    [SerializeField] private int minPieces = 5;         
    [SerializeField] private int maxPieces = 8;         
    [SerializeField] private float explosionForce = 5f; 

    private Rigidbody2D rb;
    private Transform playerTransform;
    private SpriteRenderer spriteRenderer; 

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>(); 
    }

    void Start()
    {
        // Ищем новый компонент игрока вместо старого PointDrawer
        PlayerController playerScript = Object.FindFirstObjectByType<PlayerController>();
        if (playerScript != null)
        {
            playerTransform = playerScript.transform;
        }
    }

    void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        if (playerTransform != null && rb != null)
        {
            Vector2 heading = (Vector2)playerTransform.position - rb.position;
            Vector2 direction = heading.normalized;

            if (heading.sqrMagnitude < 0.01f)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            rb.linearVelocity = direction * speed;
            FlipSprite();
        }
    }

    private void FlipSprite()
    {
        if (spriteRenderer == null) return;

        if (rb.linearVelocity.x > 0.1f)
        {
            spriteRenderer.flipX = false; 
        }
        else if (rb.linearVelocity.x < -0.1f)
        {
            spriteRenderer.flipX = true; 
        }
    }

    // 1. Физическое касание игрока (твердое тело об твердое тело)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        // Если коснулись игрока
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            collision.gameObject.GetComponent<PlayerController>().ExplodeIntoPieces();
            Debug.Log("💀 ИГРОК ПРОИГРАЛ! Зафиксировано жесткое столкновение с красным кругом.");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerGameOver();
            }
        }
    }

    // 2. Попадание пули-триггера
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        // Если пробила пуля
        if (other.GetComponent<Bullet>() != null || other.GetComponent<RicochetBullet>() != null || other.GetComponent<LaserBeam>())
        {
            Debug.Log("💥 Враг уничтожен выстрелом пули!");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterEnemyKilled();
            }
            ExplodeIntoPieces();
        }
    }
    

    private void ExplodeIntoPieces()
    {
        if (piecePrefabs == null || piecePrefabs.Length == 0) return;

        Vector3 spawnPosition = transform.position;
        int spawnCount = Random.Range(minPieces, maxPieces + 1);

        for (int i = 0; i < spawnCount; i++)
        {
            // Каждый осколок — отдельный Instantiate!
            GameObject prefab = piecePrefabs[Random.Range(0, piecePrefabs.Length)];
            GameObject pieceObj = Instantiate(prefab, spawnPosition, Quaternion.identity);
        
            EnemyPiece pieceScript = pieceObj.GetComponent<EnemyPiece>();

            if (pieceScript != null)
            {
                float randomAngle = Random.Range(0f, Mathf.PI * 2f);
                Vector2 direction = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
            
                float speed = Random.Range(explosionForce * 0.6f, explosionForce * 1.4f);
            
                // Небольшой разброс по позиции, чтобы не спавнились в одной точке
                Vector3 offset = Random.insideUnitCircle * 0.3f;
                pieceObj.transform.position += offset;

                pieceScript.InitializePiece(direction, speed);
            }
        }

        Destroy(gameObject);
    }
}