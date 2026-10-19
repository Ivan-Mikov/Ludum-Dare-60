using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StageManager : MonoBehaviour
{
    private bool isTutorialActive = false;

    public static StageManager Instance { get; private set; }

    // Статическая переменная сохраняет стадию ДАЖЕ после перезагрузки сцены
    private static GameManager.GameStage savedStage = GameManager.GameStage.Stage1_TutorialAndDefaultEnemies;
// Флаг указывает, что сцена перезагружена кнопкой R из-за проигрыша
    public static bool IsRestartingAfterDeath { get;  set; } = false;
// Добавьте это в любое место внутри класса StageManager (например, рядом с GetCurrentStage):
    public bool IsGameStarted => isGameStarted;
    public bool IsTutorialActive => isTutorialActive;

    public enum SpawnType { Chain, Swarm }

    [System.Serializable]
    public struct Wave
    {
        public string waveName;      
        public SpawnType spawnType;  
        public int enemiesPerGroup;  
        public int groupCount;       
    }

    [System.Serializable]
    public struct StageSetup
    {
        public string stageName;
        public float delayBetweenWaves; 
        public List<Wave> waves;     
    }

    [Header("Настройки стадий")]
    [SerializeField] private List<StageSetup> stages;

    private GameManager.GameStage currentStage = GameManager.GameStage.Stage1_TutorialAndDefaultEnemies;
    private int currentWaveIndex = 0;
    private bool isWaveRunning = false;
    private bool isPreparingStage = false; 
    private EnemySpawner spawner;
    private bool isGameStarted = false; 

    // Вспомогательное свойство для получения правильного индекса в списке stages (математический сдвиг)
    private int ListIndex => (int)currentStage - 1;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        currentStage = savedStage;
    }

    void Start()
    {
        spawner = GameObject.FindObjectOfType<EnemySpawner>();

        // ПРОВЕРКА: Если мы перезагрузились кнопкой R после смерти
        if (IsRestartingAfterDeath)
        {
            isGameStarted = true; // Сразу разрешаем игровой цикл Update

            // Сбрасываем флаг, чтобы при обычном выходе в меню игра снова ждала клика кнопки
            IsRestartingAfterDeath = false; 

            // Ищем на сцене Canvas/MenuPanel и насильно скрываем её, чтобы не нажимать кнопку повторно
            // Мы ищем GameManager, так как в нем уже заведена ссылка на menuPanel
            if (GameManager.Instance != null)
            {
                // Синхронизируем стадию в GameManager
                GameManager.Instance.SyncStageOnLoad(currentStage);

                // Насильно скрываем меню через метод, который мы добавим в GameManager ниже
                GameManager.Instance.HideMenuPanelDirectly();
            }

            // Мгновенно запускаем геймплей
            StartCurrentStage();
            Debug.Log("💀 StageManager: Обнаружен перезапуск после смерти! Меню скрыто, стадия запущена: " + currentStage);
        }
    }



    public void StartTutorialFromMenu()
    {
        isGameStarted = true;
        isTutorialActive = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCustomTutorialText("USE WASD OR ARROW KEYS TO MOVE");
        }

        StartCoroutine(TutorialRoutine());
    }
    
    private IEnumerator TutorialRoutine()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayMusic(SoundManager.MusicTrack.Normal);
        }

        // --- ПОДСТАДИЯ 1: УЧИМ ХОДИТЬ ---
        yield return new WaitUntil(() => Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0);
        yield return new WaitForSeconds(3f);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCustomTutorialText("OK, GREAT!");
        }
        yield return new WaitForSeconds(3f);

        // --- ПОДСТАДИЯ 2: УЧИМ СТРЕЛЯТЬ (4 МИШЕНИ) ---
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCustomTutorialText("LMB TO SHOOT TARGETS!");
        }

        if (GameManager.Instance != null && GameManager.Instance.phantomEnemyPrefab != null)
        {
            GameObject prefab = GameManager.Instance.phantomEnemyPrefab;
            Instantiate(prefab, new Vector3(-4.0f, 0.6f, 0f), Quaternion.identity);  
            Instantiate(prefab, new Vector3(0.2f, 0.6f, 0f), Quaternion.identity);   
            Instantiate(prefab, new Vector3(-4.0f, -1.0f, 0f), Quaternion.identity); 
            Instantiate(prefab, new Vector3(0.2f, -1.0f, 0f), Quaternion.identity);  
        }

        // Ждем уничтожения первых двух
        yield return new WaitUntil(() => GameObject.FindGameObjectsWithTag("Enemy").Length <= 2);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCustomTutorialText("WEAPON ENERGY DEPLETED! WAIT FOR RECHARGE!");
        }

        // Ждем уничтожения всех четырех начальных мишеней
        yield return new WaitUntil(() => GameObject.FindGameObjectsWithTag("Enemy").Length == 0);
        yield return new WaitForSeconds(1.5f);

        // --- ПОДСТАДИЯ 3: УЧИМ ПРОШИБАТЬ (3 В РЯД БЛИЗКО + ОДНОВРЕМЕННО С ТЕКСТОМ) ---
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCustomTutorialText("ONE BULLET CAN PIERCE MULTIPLE ENEMIES!");
        }

        if (GameManager.Instance != null && GameManager.Instance.phantomEnemyPrefab != null)
        {
            GameObject prefab = GameManager.Instance.phantomEnemyPrefab;
            // Спавним 3 фантома ОЧЕНЬ близко друг к другу (шаг 0.5f вместо 1.5f) по горизонтали (Y = 0.0f)
            Instantiate(prefab, new Vector3(-2.4f, 0.0f, 0f), Quaternion.identity);
            Instantiate(prefab, new Vector3(-1.9f, 0.0f, 0f), Quaternion.identity);
            Instantiate(prefab, new Vector3(-1.4f, 0.0f, 0f), Quaternion.identity);
        }

        // Ждем, пока игрок расправится с этой тройкой
        yield return new WaitUntil(() => GameObject.FindGameObjectsWithTag("Enemy").Length == 0);
        yield return new WaitForSeconds(1f);

        // --- ПОДСТАДИЯ 4: ЗАКРЕПЛЕНИЕ (5 В РЯД БЛИЗКО + ОДНОВРЕМЕННО С ТЕКСТОМ) ---
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCustomTutorialText("ONE MORE!");
        }

        if (GameManager.Instance != null && GameManager.Instance.phantomEnemyPrefab != null)
        {
            GameObject prefab = GameManager.Instance.phantomEnemyPrefab;
            // Спавним 5 фантомов в один супер-плотный ряд (шаг 0.5f)
            Instantiate(prefab, new Vector3(-2.9f, 0.0f, 0f), Quaternion.identity);
            Instantiate(prefab, new Vector3(-2.4f, 0.0f, 0f), Quaternion.identity);
            Instantiate(prefab, new Vector3(-1.9f, 0.0f, 0f), Quaternion.identity);
            Instantiate(prefab, new Vector3(-1.4f, 0.0f, 0f), Quaternion.identity);
            Instantiate(prefab, new Vector3(-0.9f, 0.0f, 0f), Quaternion.identity);
        }

        // Ждем полного уничтожения пяти мишеней
        yield return new WaitUntil(() => GameObject.FindGameObjectsWithTag("Enemy").Length == 0);
        yield return new WaitForSeconds(1f);

        // --- ПОДСТАДИЯ 5: ПРЕДУПРЕЖДЕНИЕ ОБ ОПАСНОСТИ ---
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCustomTutorialText("CAREFUL! REAL ENEMIES CAN BITE!");
        }
        yield return new WaitForSeconds(4f);

        // --- ФИНАЛ ОБУЧЕНИЯ ---
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCustomTutorialText("TUTORIAL PASSED!!!");
        }
        yield return new WaitForSeconds(3f);

        // Полный сброс и выход в чистое Главное Меню через перезагрузку
        isTutorialActive = false;
        isGameStarted = false;
        ResetSavedStage();
        
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }



    public void StartGameFromMenu()
    {
        isGameStarted = true; 

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SyncStageOnLoad(currentStage);
        }

        StartCurrentStage();
        Debug.Log("🎮 Игра успешно запущена из главного меню!");
    }

    void Update()
    {
        if (isTutorialActive) return; 
        if (!isGameStarted) return; 

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
        if (isPreparingStage) return; 

        if (!isWaveRunning)
        {
            int aliveEnemies = GameObject.FindGameObjectsWithTag("Enemy").Length;
            if (aliveEnemies == 0)
            {
                StartCoroutine(NextWaveRoutine());
            }
        }
    }
// Добавьте это в любое место внутри класса StageManager:
    public GameManager.GameStage GetCurrentStage() => currentStage;

    public void StartCurrentStage()
    {
        // Обычная установка музыки
        if (GameManager.Instance != null && GameManager.Instance.CurrentStage == GameManager.GameStage.Stage5_Bulldozer)
        {
            SoundManager.Instance.PlayMusic(SoundManager.MusicTrack.Boss);
        }
        else
        {
            SoundManager.Instance.PlayMusic(SoundManager.MusicTrack.Normal);
        }
    
        // Проверяем выход за пределы списка стадий
        if (ListIndex < 0 || ListIndex >= stages.Count)
        {
            Debug.Log("🎉 ВСЕ СТАДИИ УСПЕШНО ПРОЙДЕНЫ! ИГРОК ПОБЕДИЛ!");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerWin();
            }
            return;
        }

        // Сохраняем текущую стадию в статическую переменную (переживет перезагрузку)
        savedStage = currentStage;

        currentWaveIndex = 0;
        isWaveRunning = true; 

        // Запускаем корутину заставки
        StartCoroutine(StageIntroductionRoutine());
    }



    private IEnumerator StageIntroductionRoutine()
    {
        isPreparingStage = true;

        if (GameManager.Instance != null && ListIndex >= 0 && ListIndex < stages.Count)
        {
            // Здесь выводим красивый номер стадии: из enum вычитаем туториал. Stage1 станет "STAGE 1"
            string stageTitle = $"STAGE {(int)currentStage} - {stages[ListIndex].stageName.ToUpper()}";
            GameManager.Instance.SetCustomTutorialText(stageTitle);
        }

        yield return new WaitForSeconds(5f);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCustomTutorialText("");
        }

        isPreparingStage = false;
        isWaveRunning = false; 
        Debug.Log($"⚔️ Заставка кончилась. Пошел спавн для: {stages[ListIndex].stageName}");
    }

    private IEnumerator NextWaveRoutine()
    {
        isWaveRunning = true;

        // Фикс проверки: сравниваем текущую волну с количеством волн в текущей стадии списка
        if (ListIndex >= 0 && ListIndex < stages.Count && currentWaveIndex < stages[ListIndex].waves.Count)
        {
            StageSetup stageSetup = stages[ListIndex];
            Wave currentWave = stageSetup.waves[currentWaveIndex];
                
            List<Coroutine> activeSpawnCoroutines = new List<Coroutine>();

            int finalGroupCount = currentWave.groupCount;
            int finalEnemiesPerGroup = currentWave.enemiesPerGroup;

            if (GameManager.CurrentDifficulty == GameManager.Difficulty.Hard)
            {
                finalGroupCount *= 2;
                finalEnemiesPerGroup *= 1;
            }

            for (int i = 0; i < finalGroupCount; i++)
            {
                if (spawner != null)
                {
                    Coroutine c = StartCoroutine(spawner.ExecuteSpawnCommand(currentWave.spawnType, finalEnemiesPerGroup));
                    activeSpawnCoroutines.Add(c);
                }
            }

            foreach (var coroutine in activeSpawnCoroutines)
            {
                yield return coroutine;
            }

            yield return new WaitForSeconds(stageSetup.delayBetweenWaves);

            currentWaveIndex++;
            isWaveRunning = false;
        }
        else
        {
            // ЕСТЕСТВЕННЫЙ ПЕРЕХОД: Волны на этой стадии кончились
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ClearAllStageRemnants(); 
                
                // Переходим на следующий Enum
                currentStage++;
                savedStage = currentStage; 

                // Проверяем на победу (если вышли за пределы списка боевых стадий)
                if (ListIndex >= stages.Count)
                {
                    savedStage = GameManager.GameStage.Stage1_TutorialAndDefaultEnemies;
                    GameManager.Instance.TriggerWin();
                }
                else
                {
                    GameManager.Instance.SwitchToStage(currentStage);
                    StartCurrentStage();
                }
            }
        }
    }

    public void ForceSkipToNextStage()
    {
        StopAllCoroutines();
        currentStage++;
        
        if (ListIndex >= stages.Count)
        {
            savedStage = GameManager.GameStage.Stage1_TutorialAndDefaultEnemies;
            if (GameManager.Instance != null) GameManager.Instance.TriggerWin();
        }
        else
        {
            savedStage = currentStage;
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SwitchToStage(currentStage);
            }
            
            StartCurrentStage();
        }
    }

    public static void ResetSavedStage()
    {
        savedStage = GameManager.GameStage.Stage1_TutorialAndDefaultEnemies;
    }
    
    public void StopGameForMenu()
    {
        isGameStarted = false;
        isWaveRunning = false;
        isPreparingStage = false;
        isTutorialActive = false; 
        currentStage = GameManager.GameStage.Stage1_TutorialAndDefaultEnemies;
        savedStage = GameManager.GameStage.Stage1_TutorialAndDefaultEnemies;
        currentWaveIndex = 0;
    
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ClearAllStageRemnants();
        }
    
        StopAllCoroutines(); 
    }
}
