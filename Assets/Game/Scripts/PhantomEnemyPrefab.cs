using UnityEngine;

public class PhantomEnemy : MonoBehaviour
{
    [Header("Настройки скорости")]
    [SerializeField] private float moveSpeed = 1.2f; // Сделали скорость еще меньше, чтобы было легко попасть

    [Header("Эффект разрушения (Осколки)")]
    [SerializeField] private GameObject[] piecePrefabs; // Сюда в инспекторе перетащи префабы осколков EnemyPiece
    [SerializeField] private int minPieces = 4;         
    [SerializeField] private int maxPieces = 7;         
    [SerializeField] private float explosionForce = 3f; // Сила разлета осколков

    private SpriteRenderer spriteRenderer;
    private Vector2[] patrolPoints;
    private int currentPointIndex = 0;
    private bool isInsideScreen = false;
    private Vector2 targetPosition;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.gravityScale = 0f;

        // Точки патрулирования внутри экрана осциллографа
        patrolPoints = new Vector2[]
        {
            new Vector2(-4.5f, 1.8f),  
            new Vector2(0.8f, 1.8f),   
            new Vector2(0.8f, -1.5f),  
            new Vector2(-4.5f, -1.5f)  
        };

        targetPosition = patrolPoints[0];

        UpdateSpriteDirection();
    }

    void Update()
    {
        // Плавный полет от точки к точке
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, targetPosition) < 0.1f)
        {
            if (!isInsideScreen)
            {
                isInsideScreen = true;
            }

            currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
            targetPosition = patrolPoints[currentPointIndex];

            // Разворот спрайта при смене направления
            UpdateSpriteDirection();
        }
    }

    private void UpdateSpriteDirection()
    {
        if (spriteRenderer == null) return;

        float directionX = targetPosition.x - transform.position.x;

        if (directionX > 0.1f)
        {
            spriteRenderer.flipX = false; 
        }
        else if (directionX < -0.1f)
        {
            spriteRenderer.flipX = true; 
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        // Если фантом врезался в игрока, тоже можно засчитать урон/проигрыш
        if (collision.gameObject.CompareTag("Player"))
        {
            if (GameManager.Instance != null) GameManager.Instance.TriggerGameOver();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Если фантом прошит пулей
        if (other.GetComponent<Bullet>() != null)
        {
            ExplodeIntoPieces();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterEnemyKilled();
            }

            Destroy(gameObject);
        }
    }


    // Твой кастомный метод взрыва, который берет префабы осколков и инициализирует их по твоему коду EnemyPiece
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
