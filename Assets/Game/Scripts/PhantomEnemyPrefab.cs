using UnityEngine;

public class PhantomEnemy : MonoBehaviour
{
    [Header("Эффект разрушения (Осколки)")]
    [SerializeField] private GameObject[] piecePrefabs; 
    [SerializeField] private int minPieces = 4;         
    [SerializeField] private int maxPieces = 7;         
    [SerializeField] private float explosionForce = 3f; 

    void Start()
    {
        // Настраиваем физику, чтобы объект был недвижимой стеной
        if (TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.bodyType = RigidbodyType2D.Kinematic; // Кинематический объект не сдвинуть обычным толчком
            rb.linearVelocity = Vector2.zero;
            rb.constraints = RigidbodyConstraints2D.FreezeAll; // Полная блокировка
        }

        // Гарантируем, что коллайдер твердый, а не прозрачный триггер
        if (TryGetComponent<Collider2D>(out var col))
        {
            col.isTrigger = false;
        }

        // Принудительно ставим тег для StageManager
        gameObject.tag = "Enemy";
    }

    // Так как IsTrigger выключен, Unity вызывает OnCollisionEnter2D вместо OnTriggerEnter2D
    private void OnTriggerEnter2D(Collider2D other)
    {
        SoundManager.Instance.PlaySFX(SoundManager.SoundEffect.EnemyDeath);
        
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
