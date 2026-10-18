using UnityEngine;
using System.Collections;

public class FinalBoss : MonoBehaviour
{
    public static FinalBoss Instance { get; private set; }

    [Header("Настройки здоровья")]
    [SerializeField] private float maxLaserShieldHP = 500f; // Секунды удержания лазера
    
    [Header("Ссылки на Хотбар из Квадратов")]
    [SerializeField] private Transform healthBarFill;      // Сюда перетащи объект HealthBarFill
    [SerializeField] private SpriteRenderer fillRenderer;  // Сюда перетащи SpriteRenderer объекта HealthBarFill

    [Header("Настройки движения")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float targetXPosition = -4.5f; // Докуда выползает слева

    [Header("Параметры Спавна Врагов")]
    [SerializeField] private int totalEnemiesToSpawn = 100;
    [SerializeField] private float delayBetweenSpawnPackets = 0.2f;

    
    [Header("Эффект разрушения")]
    [SerializeField] private GameObject[] piecePrefabs; 
    [SerializeField] private int minPieces = 5;         
    [SerializeField] private int maxPieces = 8;         
    [SerializeField] private float explosionForce = 5f; 

    
    private float currentHP;
    private bool isInvulnerable = true; 
    private bool isPositioned = false;
    private bool phaseRoutineStarted = false;
    private EnemySpawner enemySpawner;
    private Vector3 originalFillScale;

    public bool IsInvulnerable => isInvulnerable;

    void Awake()
    {
        if (Instance == null) Instance = this;
        currentHP = maxLaserShieldHP;
    }

    void Start()
    {
        enemySpawner = GameObject.FindObjectOfType<EnemySpawner>();

        if (healthBarFill != null)
        {
            originalFillScale = healthBarFill.localScale;
        }

        UpdateHealthBarVisual();
        StartCoroutine(BossPhaseRoutine());
    }

    void Update()
    {
        // Выползаем слева
        if (!isPositioned)
        {
            transform.position = Vector3.MoveTowards(transform.position, 
                new Vector3(targetXPosition, transform.position.y, transform.position.z), 
                moveSpeed * Time.deltaTime);

            if (Mathf.Abs(transform.position.x - targetXPosition) < 0.05f)
            {
                isPositioned = true;
            }
        }

        // ТОЧЕЧНЫЙ ФИКС: Если все враги на сцене перебиты (остался только 1 объект — сам босс)
        if (isPositioned && isInvulnerable && phaseRoutineStarted)
        {
            int aliveEnemies = GameObject.FindGameObjectsWithTag("Enemy").Length;
        
            // Если обычных врагов больше нет, снимаем неуязвимость
            if (aliveEnemies <= 1) 
            {
                SetInvulnerableState(false);
            }
        }
    }


    private IEnumerator BossPhaseRoutine()
    {
        yield return new WaitUntil(() => isPositioned);
        phaseRoutineStarted = true;

        while (currentHP > 0)
        {
            // ФАЗА 1: Призыв орды
            SetInvulnerableState(true);
            Debug.Log($"👾 МАТЬ ВРАГОВ: Спавню {totalEnemiesToSpawn} прихвостней!");

            if (enemySpawner != null)
            {
                int spawnedCount = 0;
                while (spawnedCount < totalEnemiesToSpawn)
                {
                    StartCoroutine(enemySpawner.ExecuteSpawnCommand(StageManager.SpawnType.Swarm, 4));
                    spawnedCount += 4;
                    yield return new WaitForSeconds(delayBetweenSpawnPackets);
                }
            }

            // Ждем, пока игрок выкосит абсолютно всех мобов
            yield return new WaitUntil(() => !isInvulnerable);

            // ФАЗА 2: Щит упал, босс уязвим
            Debug.Log("⚡ ЩИТ БОССА СДОХ! ЖГИТЕ ЕЁ!");
        
            // Даем игроку 7 секунд чистого времени на урон
            yield return new WaitForSeconds(7f);
        
            // ТОЧЕЧНЫЙ ФИКС: Если за эти 7 секунд босс не умер, он снова закрывается щитом и спавнит новую сотню!
            if (currentHP > 0)
            {
                Debug.Log("🛡️ БОСС: Время уязвимости истекло! Перезарядка щита!");
                // Принудительно возвращаем в Update состояние поиска врагов для следующего круга
                isInvulnerable = true; 
            }
        }
    }


    private void SetInvulnerableState(bool invulnerable)
    {
        isInvulnerable = invulnerable;
        if (fillRenderer != null)
        {
            // Зеленый — уязвим, Красный — неуязвим (активен щит)
            fillRenderer.color = invulnerable ? Color.red : Color.green;
        }
    }

    public void TakeLaserDamage(float damageAmount)
    {
        if (isInvulnerable || currentHP <= 0) return;

        currentHP -= damageAmount;
        UpdateHealthBarVisual();

        StopCoroutine(DamageFlash());
        StartCoroutine(DamageFlash());

        if (currentHP <= 0)
        {
            BossDeath();
        }
    }

    private void UpdateHealthBarVisual()
    {
        if (healthBarFill == null) return;

        float hpPercentage = Mathf.Clamp01(currentHP / maxLaserShieldHP);
        // Скейлим дочерний квадрат по оси X на основе процента здоровья
        healthBarFill.localScale = new Vector3(originalFillScale.x * hpPercentage, originalFillScale.y, originalFillScale.z);
    }

    private IEnumerator DamageFlash()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = Color.gray;
            yield return new WaitForSeconds(0.05f);
            sr.color = Color.white;
        }
    }

    private void BossDeath()
    {
        ExplodeIntoPieces();
        Debug.Log("🎉 ПОБЕДА! МАТЬ ВРАГОВ СТЕРТА В ПОРОШОК!");
        StopAllCoroutines();
        if (GameManager.Instance != null) GameManager.Instance.TriggerWin();
        Destroy(gameObject);
    }
    
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
