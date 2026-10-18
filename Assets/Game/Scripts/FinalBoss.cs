using UnityEngine;
using System.Collections;

public class FinalBoss : MonoBehaviour
{
    public static FinalBoss Instance { get; private set; }

    [Header("Здоровье и фазы")]
    [SerializeField] private float maxShieldHP = 7;
    [SerializeField] private float vulnerableTime = 9f;           // Сколько секунд босс остаётся уязвимым
    [SerializeField] private float damageCooldown = 0.08f;        // Защита от спама лазером

    [Header("Хелсбар")]
    [SerializeField] private Transform healthBarFill;
    [SerializeField] private SpriteRenderer fillRenderer;

    [Header("Движение")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float targetXPosition = -4.5f;

    [Header("Спавн врагов")]
    [SerializeField] private int enemiesPerWave = 25;
    [SerializeField] private float delayBetweenSpawnPackets = 0.25f;

    [Header("Эффект смерти")]
    [SerializeField] private GameObject[] piecePrefabs;
    [SerializeField] private int minPieces = 8;
    [SerializeField] private int maxPieces = 14;
    [SerializeField] private float explosionForce = 5f;

    private float currentHP;
    private bool isInvulnerable = true;
    private bool isPositioned = false;
    private bool isInVulnerablePhase = false;

    private float lastDamageTime;
    private Vector3 originalFillScale;
    private EnemySpawner enemySpawner;

    public bool IsInvulnerable => isInvulnerable;

    void Awake()
    {
        if (Instance == null) Instance = this;

        // --- МОДИФИКАТОРЫ ДЛЯ HARD MODE ---
        if (GameManager.CurrentDifficulty == GameManager.Difficulty.Hard)
        {
            maxShieldHP *= 3f;       // ХП босса в 3 раза больше
            enemiesPerWave *= 2;     // Врагов спавнит в 2 раза больше
            Debug.Log($"💀 ХАРДКОРНЫЙ БОСС: ХП = {maxShieldHP}, Врагов в волне = {enemiesPerWave}");
        }
        // ----------------------------------

        currentHP = maxShieldHP;
    }


    void Start()
    {
        enemySpawner = FindObjectOfType<EnemySpawner>();

        if (healthBarFill != null)
            originalFillScale = healthBarFill.localScale;

        UpdateHealthBar();
        StartCoroutine(BossBattleRoutine());
    }

    void Update()
    {
        if (!isPositioned)
        {
            transform.position = Vector3.MoveTowards(transform.position,
                new Vector3(targetXPosition, transform.position.y, 0f),
                moveSpeed * Time.deltaTime);

            if (Mathf.Abs(transform.position.x - targetXPosition) < 0.05f)
                isPositioned = true;
        }
    }

    // ====================== ОСНОВНАЯ ЛОГИКА БОССА ======================
    private IEnumerator BossBattleRoutine()
    {
        yield return new WaitUntil(() => isPositioned);

        while (currentHP > 0)
        {
            // === ФАЗА 1: ЩИТ АКТИВЕН ===
            isInvulnerable = true;
            isInVulnerablePhase = false;
            UpdateVisuals();

            Debug.Log("🛡️ Босс поднял щит. Спавним орду...");

            yield return StartCoroutine(SpawnEnemyWave());

            // Ждём, пока игрок не убьёт всех врагов
            yield return new WaitUntil(() => AreAllEnemiesDead());

            // === ФАЗА 2: ЩИТ СНЯТ — БОСС УЯЗВИМ ===
            isInvulnerable = false;
            isInVulnerablePhase = true;
            UpdateVisuals();

            Debug.Log($"⚡ Босс уязвим! У вас {vulnerableTime} секунд!");

            float timer = 0f;
            while (timer < vulnerableTime && currentHP > 0)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            // Если босс не умер — снова поднимаем щит
            if (currentHP > 0)
            {
                Debug.Log("🛡️ Время уязвимости закончилось. Босс снова закрывается щитом.");
            }
        }

        BossDeath();
    }

    private IEnumerator SpawnEnemyWave()
    {
        if (enemySpawner == null) yield break;

        int spawned = 0;

        while (spawned < enemiesPerWave)
        {
            int toSpawn = Mathf.Min(5, enemiesPerWave - spawned);
            StartCoroutine(enemySpawner.ExecuteSpawnCommand(StageManager.SpawnType.Swarm, toSpawn));
            spawned += toSpawn;
            yield return new WaitForSeconds(delayBetweenSpawnPackets);
        }
    }

    private bool AreAllEnemiesDead()
    {
        return GameObject.FindGameObjectsWithTag("Enemy").Length <= 1;
    }

    // ====================== ПОЛУЧЕНИЕ УРОНА ======================
    public void TakeLaserDamage(float damageAmount)
    {
        if (isInvulnerable || currentHP <= 0) return;
        if (Time.time - lastDamageTime < damageCooldown) return;

        lastDamageTime = Time.time;

        currentHP -= damageAmount;
        UpdateHealthBar();

        StopCoroutine(DamageFlash());
        StartCoroutine(DamageFlash());

        if (currentHP <= 0)
            BossDeath();
    }

    private void UpdateVisuals()
    {
        if (fillRenderer != null)
            fillRenderer.color = isInvulnerable ? Color.red : Color.green;
    }

    private void UpdateHealthBar()
    {
        if (healthBarFill == null) return;

        float percent = Mathf.Clamp01(currentHP / maxShieldHP);
        healthBarFill.localScale = new Vector3(originalFillScale.x * percent, originalFillScale.y, originalFillScale.z);
    }

    private IEnumerator DamageFlash()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) yield break;

        sr.color = new Color(0.6f, 0.6f, 0.6f);
        yield return new WaitForSeconds(0.08f);
        sr.color = Color.white;
    }

    private void BossDeath()
    {
        ExplodeIntoPieces();
        Debug.Log("🎉 БОСС УНИЧТОЖЕН! ПОБЕДА!");
        StopAllCoroutines();

        if (GameManager.Instance != null)
            GameManager.Instance.TriggerWin();
    }

    private void ExplodeIntoPieces()
    {
        if (piecePrefabs == null || piecePrefabs.Length == 0) return;

        int count = Random.Range(minPieces, maxPieces + 1);
        Vector3 pos = transform.position;

        for (int i = 0; i < count; i++)
        {
            GameObject piece = Instantiate(piecePrefabs[Random.Range(0, piecePrefabs.Length)], pos, Quaternion.identity);
            EnemyPiece script = piece.GetComponent<EnemyPiece>();

            if (script != null)
            {
                Vector2 dir = Random.insideUnitCircle.normalized;
                float speed = Random.Range(explosionForce * 0.7f, explosionForce * 1.6f);
                piece.transform.position += (Vector3)Random.insideUnitCircle * 0.4f;
                script.InitializePiece(dir, speed);
            }
        }

        Destroy(gameObject, 0.1f);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            var player = collision.gameObject.GetComponent<PlayerController>();
            if (player != null) player.ExplodeIntoPieces();

            if (GameManager.Instance != null)
                GameManager.Instance.TriggerGameOver();
        }
    }
}