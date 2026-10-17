using UnityEngine;

public class EnemyCircle : MonoBehaviour
{
    [Header("Движение и преследование")]
    [SerializeField] private float speed = 2f; 

    [Header("Эффект разрушения")]
    [SerializeField] private GameObject[] piecePrefabs; // Сюда в инспекторе перетащите префабы осколков
    [SerializeField] private int minPieces = 5;         // Минимальное количество осколков
    [SerializeField] private int maxPieces = 8;         // Максимальное количество осколков
    [SerializeField] private float explosionForce = 5f; // Сила разлета осколков

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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // КОНТАКТ С ИГРОКОМ: Проигрыш
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player") || collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("💀 ИГРОК ПРОИГРАЛ! Вас поймал красный круг.");
            return;
        }

        // КОНТАКТ С НАРИСОВАННЫМ ОБЪЕКТОМ: Смерть и разлет на куски
        if (collision.gameObject.layer == LayerMask.NameToLayer("DrawnObjects") || collision.gameObject.GetComponent<PolygonCollider2D>() != null)
        {
            Debug.Log("💥 Хахахах лох! Враг разбит на физичные кусочки.");
            ExplodeIntoPieces();
            Destroy(gameObject);
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
