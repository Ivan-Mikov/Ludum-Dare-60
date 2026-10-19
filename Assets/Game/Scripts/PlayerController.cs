using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [Header("Движение")]
    [SerializeField] private float moveSpeed = 4f;

    [Header("Стрельба и Оружие")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint; 
    [SerializeField] private Transform gunTransform; // Перетащи сюда объект Пушки (Gun) из иерархии!
    [SerializeField] private float bulletSpeed = 10f;

    [Header("Патроны / Шкала энергии")]
    [SerializeField] private Image ammoBarFillImage;
    [SerializeField] private float maxAmmo = 100f;
    [SerializeField] private float shotCost = 33.34f; 
    [SerializeField] private float rechargeRate = 15f; 

    [Header("Эффект разрушения (Осколки)")]
    [SerializeField] private GameObject[] piecePrefabs; 
    [SerializeField] private int minPieces = 7;         
    [SerializeField] private int maxPieces = 10;         
    [SerializeField] private float explosionForce = 3f; 

    [SerializeField] private GameObject ricochetBulletPrefab; // Префаб пули для 3-й стадии
    
    [Header("Настройки Лазера (Стадия 4)")]
    [SerializeField] private GameObject laserPrefab;      // Сюда перетащи префаб LaserBeam
    [SerializeField] private float laserHeatRate = 20f;    // Скорость нагрева в секунду
    [SerializeField] private float laserCoolRate = 1f;    // Скорость остывания в секунду

    private LaserBeam spawnedLaser; // Ссылка на созданный лазер
    private bool isLaserFiring = false; // Состояние: лазер сейчас фигачит?
    private LaserBeam[] tripleLasers = new LaserBeam[3]; // Три луча для финальной стадии
    private bool isTripleLaserFiring = false;
    private GameManager.GameStage lastCheckedStage; // Храним стадию из прошлого кадра

    
    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer; 
    private Camera mainCamera;
    private float currentAmmo;
    private bool hasMoved = false; 

    private const float MinX = -5.32f;
    private const float MaxX = 1.44f;
    private const float MinY = -2.02f;
    private const float MaxY = 2.3f;
    
    [Header("Меню")]
    [SerializeField] private GameObject menuPanel; // Сюда перетащи ту же MenuPanel из Canvas


    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (rb != null) rb.freezeRotation = true;
        
        mainCamera = Camera.main;
        currentAmmo = maxAmmo;
    }


    void Update()
{
    // Проверяем, идет ли сейчас активная игра (или туториал)
    bool isGameplayActive = true;
    if (StageManager.Instance != null && !StageManager.Instance.IsGameStarted)
    {
        if (!StageManager.Instance.IsTutorialActive)
        {
            isGameplayActive = false; // Мы в главном меню, обычная игра не запущена
        }
    }

    // Если открыта панель настроек/паузы во время игры, тоже блокируем стрельбу
    if (menuPanel != null && menuPanel.activeSelf)
    {
        isGameplayActive = false;
    }

    // ---- ЕСЛИ СТРЕЛЬБА ЗАБЛОКИРОВАНА (В МЕНЮ) ----
    if (!isGameplayActive)
    {
        // Принудительно выключаем лучи лазеров, если они физически горели
        if (spawnedLaser != null && spawnedLaser.gameObject.activeSelf) spawnedLaser.DeactivateBeam();
        ToggleTripleLasers(false);
        
        // ВАЖНО: Не делаем return! Пропускаем код мимо стрельбы сразу к прицеливанию
    }
    else
    {
        // ---- СТАНДАРТНЫЙ БЛОК: СБРОС ПРИ СМЕНЕ СТАДИИ (работает только в игре) ----
        if (GameManager.Instance != null)
        {
            GameManager.GameStage currentStage = GameManager.Instance.CurrentStage;
        
            if (currentStage != lastCheckedStage)
            {
                currentAmmo = 0f;          
                isLaserFiring = false;     
                isTripleLaserFiring = false; 
            
                if (spawnedLaser != null) spawnedLaser.DeactivateBeam();
                ToggleTripleLasers(false);
            
                lastCheckedStage = currentStage; 
            }
        }
    }

    // ---- БОЕВАЯ ЛОГИКА (Выполняется, только если мы НЕ в главном меню) ----
    if (isGameplayActive)
    {
        bool isBossStage = (GameManager.Instance != null && GameManager.Instance.CurrentStage == GameManager.GameStage.Stage5_Bulldozer);
        bool isLaserStage = (GameManager.Instance != null && GameManager.Instance.CurrentStage == GameManager.GameStage.Stage4_Laser);

        if (isBossStage) 
        {
            HandleTripleLaserLogic();
        }
        else if (isLaserStage)
        {
            ToggleTripleLasers(false);
            HandleLaserLogic();
        }
        else
        {
            ToggleTripleLasers(false);
            if (spawnedLaser != null && spawnedLaser.gameObject.activeSelf) spawnedLaser.DeactivateBeam();

            HandleShooting();
            HandleAmmoRecharge();
        }
    }

    // ---- ПРИЦЕЛИВАНИЕ ЗА КУРСОРОМ (Работает ВСЕГДА, в том числе и в Главном Меню!) ----
    RotateGunTowardsCursor(); 
}





    void FixedUpdate()
    {
        // ---- ПОДВИЖНЫЙ СПРАЙТ (Работает ВСЕГДА, в том числе и в Главном Меню!) ----
        // Разворачиваем персонажа влево/вправо за мышкой в любом состоянии игры
        FlipCharacterTowardsCursor(); 

        // ---- БЛОКИРОВКА ХОДЬБЫ В ГЛАВНОМ МЕНЮ ----
        // Если игра в StageManager еще не началась (и это не туториал) — запрещаем бегать
        if (StageManager.Instance != null && !StageManager.Instance.IsGameStarted)
        {
            if (!StageManager.Instance.IsTutorialActive)
            {
                if (rb != null) rb.linearVelocity = Vector2.zero; // Полностью гасим скорость
                animator.SetBool("isRunning", false); // Принудительно включаем Idle-анимацию
                return; // Выходим из метода, не давая персонажу сдвинуться с места
            }
        }

        // Если открыта панель паузы/настроек во время геймплея — тоже стопорим игрока
        if (menuPanel != null && menuPanel.activeSelf) 
        {
            if (rb != null) rb.linearVelocity = Vector2.zero; 
            animator.SetBool("isRunning", false);
            return; 
        }

        // ---- ГЕЙМПЛЕЙНОЕ ПЕРЕМЕЩЕНИЕ (Работает только в бою или туториале) ----
        HandleMovement();
    }



    private void HandleMovement()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        Vector2 moveInput = new Vector2(moveX, moveY).normalized;

        if (moveInput.sqrMagnitude > 0.01f)
        {
            animator.SetBool("isRunning", true);

            if (!hasMoved)
            {
                hasMoved = true;
            }
        }
        else
        {
            animator.SetBool("isRunning", false);
        }

        Vector2 targetPosition = rb.position + moveInput * moveSpeed * Time.fixedDeltaTime;
        targetPosition.x = Mathf.Clamp(targetPosition.x, MinX, MaxX);
        targetPosition.y = Mathf.Clamp(targetPosition.y, MinY, MaxY);

        rb.MovePosition(targetPosition);
    }
    
    // 1. ПОВОРОТ ПУШКИ ЗА КУРСОРОМ
    // 1. ПОВОРОТ ПУШКИ ЗА КУРСОРОМ (Упрощенный вариант без багов)
    private void RotateGunTowardsCursor()
    {
        if (gunTransform == null) return;

        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        // Считаем направление от пушки к мыши
        Vector2 lookDirection = (mouseWorldPos - gunTransform.position).normalized;

        // Вычисляем угол в градусах
        float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;

        // Корректируем угол, если персонаж повернут влево через localScale
        if (transform.localScale.x < 0)
        {
            // Разворачиваем угол пушки, чтобы она не смотрела «внутрь» персонажа
            gunTransform.rotation = Quaternion.Euler(0f, 0f, angle + 180f);
        }
        else
        {
            gunTransform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    // 2. ИСПРАВЛЕННЫЙ ФЛИП ВСЕГО ПЕРСОНАЖА (Вместе с пушкой)
    private void FlipCharacterTowardsCursor()
    {
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector3 currentScale = transform.localScale;

        // Если мышь левее игрока — разворачиваем весь объект влево через минус в localScale
        if (mouseWorldPos.x < transform.position.x)
        {
            currentScale.x = -Mathf.Abs(currentScale.x); // Гарантируем минус
        }
        // Если мышь правее — смотрим вправо (плюс в localScale)
        else if (mouseWorldPos.x > transform.position.x)
        {
            currentScale.x = Mathf.Abs(currentScale.x);  // Гарантируем плюс
        }

        transform.localScale = currentScale;
    }


    private void HandleShooting()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (currentAmmo >= shotCost)
            {
                Shoot();
            }
            else
            {
                if (GameManager.Instance != null)
                {
                    SoundManager.Instance.PlaySFX(SoundManager.SoundEffect.Error);
                    GameManager.Instance.PlayDoubleLoopClickWarning();
                }
            }
        }
    }

    private void Shoot()
    {
        currentAmmo -= shotCost; //

        if (bulletPrefab == null) return; //

        Transform spawnPoint = firePoint != null ? firePoint : transform; //
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition); //
        mouseWorldPos.z = 0f; //

        Vector2 baseDirection = ((Vector2)mouseWorldPos - (Vector2)spawnPoint.position).normalized; //

        // --- СТАДИЯ 2: ДРОБОВИК (9 ПУЛЬ) ---
        if (GameManager.Instance != null && GameManager.Instance.CurrentStage == GameManager.GameStage.Stage2_Shotgun)
        {
            // ... (Ваш старый код дробовика остается без изменений) ...
            float baseAngle = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;
            float[] layer1Angles = { -20f, -10f, 0f, 10f, 20f };
            foreach (float offsetAngle in layer1Angles)
            {
                float currentAngle = baseAngle + offsetAngle;
                Vector2 shootDirection = new Vector2(Mathf.Cos(currentAngle * Mathf.Deg2Rad), Mathf.Sin(currentAngle * Mathf.Deg2Rad));
                GameObject bulletObj = Instantiate(bulletPrefab, spawnPoint.position, Quaternion.identity);
                if (bulletObj.TryGetComponent<Bullet>(out var bulletScript)) { bulletScript.Initialize(shootDirection, bulletSpeed); }
            }
            float[] layer2Angles = { -15f, -5f, 5f, 15f };
            Vector3 secondarySpawnPos = spawnPoint.position - (Vector3)baseDirection * 0.1f; 
            foreach (float offsetAngle in layer2Angles)
            {
                float currentAngle = baseAngle + offsetAngle;
                Vector2 shootDirection = new Vector2(Mathf.Cos(currentAngle * Mathf.Deg2Rad), Mathf.Sin(currentAngle * Mathf.Deg2Rad));
                GameObject bulletObj = Instantiate(bulletPrefab, secondarySpawnPos, Quaternion.identity);
                if (bulletObj.TryGetComponent<Bullet>(out var bulletScript)) { bulletScript.Initialize(shootDirection, bulletSpeed); }
            }
            SoundManager.Instance.PlaySFX(SoundManager.SoundEffect.CannonShot);
            Debug.Log("💥 КРУШИТЕЛЬНЫЙ ЗАЛП: 9 пуль (5 в первом ряду, 4 во втором)!");
        }
        // --- СТАДИЯ 3: РИКОШЕТНЫЕ ПУЛИ (ВОЗВРАТ К СТАРОЙ ПУШКЕ С МОДИФИКАТОРОМ) ---
        else if (GameManager.Instance != null && GameManager.Instance.CurrentStage == GameManager.GameStage.Stage3_Ricochet)
        {
            SoundManager.Instance.PlaySFX(SoundManager.SoundEffect.CannonShot);
            if (ricochetBulletPrefab != null)
            {
                GameObject bulletObj = Instantiate(ricochetBulletPrefab, spawnPoint.position, Quaternion.identity);
                if (bulletObj.TryGetComponent<RicochetBullet>(out var ricochetScript))
                {
                    ricochetScript.Initialize(baseDirection, bulletSpeed);
                }
            }
            else
            {
                // На случай если забыли назначить префаб в инспекторе, стреляем обычной
                GameObject bulletObj = Instantiate(bulletPrefab, spawnPoint.position, Quaternion.identity);
                if (bulletObj.TryGetComponent<Bullet>(out var bulletScript)) { bulletScript.Initialize(baseDirection, bulletSpeed); }
            }
        }
        // --- ОСТАЛЬНЫЕ СТАДИИ (ДЕФОЛТНАЯ СТРЕЛЬБА) ---
        else
        {
            SoundManager.Instance.PlaySFX(SoundManager.SoundEffect.CannonShot);
            GameObject bulletObj = Instantiate(bulletPrefab, spawnPoint.position, Quaternion.identity);
            Bullet bulletScript = bulletObj.GetComponent<Bullet>();
        
            if (bulletScript != null)
            {
                bulletScript.Initialize(baseDirection, bulletSpeed);
            }
        }
    }


    private void HandleAmmoRecharge()
    {
        if (currentAmmo < maxAmmo)
        {
            currentAmmo = Mathf.MoveTowards(currentAmmo, maxAmmo, rechargeRate * Time.deltaTime);
        }

        if (ammoBarFillImage != null)
        {
            ammoBarFillImage.fillAmount = currentAmmo / maxAmmo;
        }
    }
    
    public void ExplodeIntoPieces()
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
    
    private void HandleLaserLogic()
    {
        // Спавним лазер один раз, если его еще нет, и привязываем его к firePoint пушки
        // 1. СПАВН ЛАЗЕРА (Если еще не создан)
        if (spawnedLaser == null)
        {
            if (laserPrefab == null)
            {
                Debug.LogError("❌ ОШИБКА: Префаб лазера не назначен в PlayerController в инспекторе!");
                return;
            }

            // Берем точку спавна (firePoint), если она не назначена — берем сам трансформ игрока
            Transform spawnPoint = firePoint != null ? firePoint : transform;

            // Спавним лазер прямо в мировой позиции firePoint и сразу делаем его ДОЧЕРНИМ к firePoint
            GameObject laserObj = Instantiate(laserPrefab, spawnPoint.position, Quaternion.identity, spawnPoint);
        
            // Обнуляем локальные координаты и поворот ОТНОСИТЕЛЬНО firePoint
            laserObj.transform.localPosition = Vector3.zero;
            laserObj.transform.localRotation = Quaternion.identity;
        
            // Гарантируем, что масштаб лазера останется правильным (1,1,1) внутри дула
            laserObj.transform.localScale = Vector3.one;
        
            spawnedLaser = laserObj.GetComponent<LaserBeam>();
            spawnedLaser.gameObject.SetActive(false);
        
            currentAmmo = 0f; // Шкала лазера по умолчанию на 0%
            Debug.Log("⚙️ Лазер успешно привязан строго к точке firePoint.");
        }


        // Логика работы состояний лазера
        if (!isLaserFiring)
        {
            // --- РЕЖИМ НАГРЕВА ---
            if (Input.GetMouseButton(0))
            {
                // Зажимаем ЛКМ — шкала заполняется
                currentAmmo = Mathf.MoveTowards(currentAmmo, maxAmmo, laserHeatRate * Time.deltaTime);
                
                // Достигли максимума — БАБАХ, лазер включается непрерывным лучом!
                if (currentAmmo >= maxAmmo)
                {
                    isLaserFiring = true;
                    if (spawnedLaser != null) spawnedLaser.ActivateBeam();
                }
            }
            else
            {
                // Отпустили ЛКМ (и лазер еще не включился) — медленно остываем
                currentAmmo = Mathf.MoveTowards(currentAmmo, 0f, laserCoolRate * Time.deltaTime);
            }
        }
        else
        {
            SoundManager.Instance.OnLaser();
            // --- РЕЖИМ СТРЕЛЬБЫ ---
            // Лазер горит непрерывно и тратит шкалу тепла до нуля (даже если ЛКМ отпущен)
            currentAmmo = Mathf.MoveTowards(currentAmmo, 0f, laserCoolRate * Time.deltaTime);

            if (currentAmmo <= 0f)
            {
                isLaserFiring = false;
                if (spawnedLaser != null) spawnedLaser.DeactivateBeam();
                SoundManager.Instance.OffLaser();
            }
        }

        // Обновляем шкалу на экране осциллографа
        if (ammoBarFillImage != null)
        {
            ammoBarFillImage.fillAmount = currentAmmo / maxAmmo;
        }
    }
        
        private void HandleTripleLaserLogic()
    {
        // 1. СПАВН ТРОЙНОГО ЛАЗЕРА (один раз)
        if (tripleLasers[0] == null && laserPrefab != null)
        {
            Transform spawnPoint = firePoint != null ? firePoint : transform;
            float[] angles = { -18f, 0f, 18f };

            for (int i = 0; i < 3; i++)
            {
                GameObject laserObj = Instantiate(laserPrefab, spawnPoint.position, Quaternion.identity, spawnPoint);
                
                laserObj.transform.localPosition = Vector3.zero;
                laserObj.transform.localRotation = Quaternion.Euler(0, 0, angles[i]);   // ← Вот здесь поворот
                laserObj.transform.localScale = Vector3.one;

                tripleLasers[i] = laserObj.GetComponent<LaserBeam>();
                if (tripleLasers[i] != null)
                    tripleLasers[i].gameObject.SetActive(false);
            }

            currentAmmo = 0f;
            Debug.Log("✅ Тройной лазер успешно создан (3 отдельных луча)");
        }

        // 2. Логика работы (аналогично HandleLaserLogic)
        if (!isTripleLaserFiring)
        {
            if (Input.GetMouseButton(0))
            {
                currentAmmo = Mathf.MoveTowards(currentAmmo, maxAmmo, laserHeatRate * Time.deltaTime);

                if (currentAmmo >= maxAmmo)
                {
                    isTripleLaserFiring = true;
                    ToggleTripleLasers(true);
                }
            }
            else
            {
                currentAmmo = Mathf.MoveTowards(currentAmmo, 0f, laserCoolRate * Time.deltaTime);
            }
        }
        else
        {
            SoundManager.Instance.OnLaser();
            currentAmmo = Mathf.MoveTowards(currentAmmo, 0f, laserCoolRate * Time.deltaTime * 1.2f);

            if (currentAmmo <= 0f)
            {
                isTripleLaserFiring = false;
                ToggleTripleLasers(false);
                SoundManager.Instance.OffLaser();
            }
        }

        if (ammoBarFillImage != null)
        {
            ammoBarFillImage.fillAmount = currentAmmo / maxAmmo;
        }
    }

    private void ToggleTripleLasers(bool activate)
    {
        for (int i = 0; i < tripleLasers.Length; i++)
        {
            if (tripleLasers[i] != null)
            {
                if (activate)
                    tripleLasers[i].ActivateBeam();
                else
                    tripleLasers[i].DeactivateBeam();
            }
        }
    }


}
