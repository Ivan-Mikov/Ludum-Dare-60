using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StageManager : MonoBehaviour
{
    private bool isTutorialActive = false;

    public static StageManager Instance { get; private set; }

    // Статическая переменная сохраняет индекс стадии ДАЖЕ после перезагрузки сцены через SceneManager
    private static int savedStageIndex = 0;

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

    private int currentStageIndex = 0;
    private int currentWaveIndex = 0;
    private bool isWaveRunning = false;
    private bool isPreparingStage = false; 
    private EnemySpawner spawner;
    private bool isGameStarted = false; // По умолчанию игра НЕ запущена (ждем меню)


    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Восстанавливаем стадию, на которой игрок проиграл
        currentStageIndex = savedStageIndex;
    }

    void Start()
    {
        spawner = GameObject.FindObjectOfType<EnemySpawner>();
        
        // УДАЛЯЕМ ИЛИ КОММЕНТИРУЕМ эти строки, чтобы игра не стартовала сама:
        // if (GameManager.Instance != null) GameManager.Instance.SyncStageOnLoad(currentStageIndex);
        // StartCurrentStage();
    }
    public void StartTutorialFromMenu()
    {
        isGameStarted = true;
        isTutorialActive = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCustomTutorialText("ИСПОЛЬЗУЙТЕ WASD ИЛИ СТРЕЛКИ ДЛЯ ХОДЬБЫ");
        }

        StartCoroutine(TutorialRoutine());
    }
    
    private IEnumerator TutorialRoutine()
{
    PlayerController player = FindObjectOfType<PlayerController>();
    
    // --- ПОДСТАДИЯ 1: УЧИМ ХОДИТЬ ---
    // Ждем, пока игрок нажмет хоть одну кнопку движения (мы используем флаг hasMoved, который уже есть у вас в PlayerController!)
    if (player != null)
    {
        // На всякий случай сбрасываем флаг, если игрок случайно нажал кнопку в меню
        // Если переменная hasMoved private в PlayerController, временно сделайте её public или воспользуйтесь осями:
        yield return new WaitUntil(() => Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0);
    }

    if (GameManager.Instance != null)
    {
        GameManager.Instance.SetCustomTutorialText("OK, GRATE!");
    }

    yield return new WaitForSeconds(5f);

    // --- ПОДСТАДИЯ 2: УЧИМ СТРЕЛЯТЬ ---
    // --- ПОДСТАДИЯ 2: УЧИМ СТРЕЛЯТЬ ---
    if (GameManager.Instance != null)
    {
        GameManager.Instance.SetCustomTutorialText("LMB to SHOOT!");
    }

    // ТОЧЕЧНОЕ ИСПРАВЛЕНИЕ: Берем правильный EnemyPrefab с большой буквы из скрипта EnemySpawner
    if (spawner != null)
    {
        EnemySpawner spawnerScript = spawner.GetComponent<EnemySpawner>();
        
        Instantiate(spawnerScript.EnemyPrefab, new Vector3(-5f, 0f, 0f), Quaternion.identity);
        Instantiate(spawnerScript.EnemyPrefab, new Vector3(-3f, 0f, 0f), Quaternion.identity);
    }

    // Ждем, пока игрок не уничтожит все мишени
    yield return new WaitUntil(() => GameObject.FindGameObjectsWithTag("Enemy").Length == 0);


    if (GameManager.Instance != null)
    {
        GameManager.Instance.SetCustomTutorialText("Tutorial completed!!!");
    }

    yield return new WaitForSeconds(3f);
    
    // Обучение плавно перетекает в обычную 1-ю стадию геймплея
    isTutorialActive = false;
    currentStageIndex = 0; 
    
    if (GameManager.Instance != null)
    {
        GameManager.Instance.SwitchToStage(0); // Включает Stage1 (индекс 0 в вашем SwitchToStage)
    }
    
    StartCurrentStage();
}


    // Этот метод вызовется из MainMenuController, когда игрок нажмет кнопку сложности
    public void StartGameFromMenu()
    {
        isGameStarted = true; // <-- РАЗРЕШАЕМ ИГРУ

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SyncStageOnLoad(currentStageIndex);
        }

        StartCurrentStage();
        Debug.Log("🎮 Игра успешно запущена из главного меню!");
    }



    void Update()
    {
        if (isTutorialActive) return; 
        if (!isGameStarted) return; // <-- ЕСЛИ ИГРА НЕ ЗАПУЩЕНА, НИЧЕГО НЕ ДЕЛАЕМ!

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

    private void StartCurrentStage()
    {
        if (GameManager.Instance.CurrentStage == GameManager.GameStage.Stage5_Bulldozer)
        {
            SoundManager.Instance.PlayMusic(SoundManager.MusicTrack.Boss);
        }
        else
        {
            SoundManager.Instance.PlayMusic(SoundManager.MusicTrack.Normal);
        }
        
        if (currentStageIndex >= stages.Count)
        {
            Debug.Log("🎉 ВСЕ СТАДИИ УСПЕШНО ПРОЙДЕНЫ! ИГРОК ПОБЕДИЛ!");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerWin();
            }
            return;
        }

        // Запоминаем текущую стадию на случай проигрыша
        savedStageIndex = currentStageIndex;

        currentWaveIndex = 0;
        isWaveRunning = true; 

        StartCoroutine(StageIntroductionRoutine());
    }

    private IEnumerator StageIntroductionRoutine()
    {
        isPreparingStage = true;

        if (GameManager.Instance != null)
        {
            string stageTitle = $"STAGE {currentStageIndex + 1} - {stages[currentStageIndex].stageName.ToUpper()}";
            GameManager.Instance.SetCustomTutorialText(stageTitle);
        }

        yield return new WaitForSeconds(5f);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCustomTutorialText("");
        }

        isPreparingStage = false;
        isWaveRunning = false; 
        Debug.Log($"⚔️ Заставка кончилась. Пошел спавн для: {stages[currentStageIndex].stageName}");
    }

    private IEnumerator NextWaveRoutine()
{
    isWaveRunning = true;

    if (currentStageIndex < stages[currentStageIndex].waves.Count)
    {
        StageSetup currentStage = stages[currentStageIndex];
        Wave currentWave = currentStage.waves[currentWaveIndex];
            
        List<Coroutine> activeSpawnCoroutines = new List<Coroutine>();

        // --- КРИТИЧЕСКИЙ ФИКС ДЛЯ HARD MODE ---
        int finalGroupCount = currentWave.groupCount;
        int finalEnemiesPerGroup = currentWave.enemiesPerGroup;

        // Если включен Hard Mode, умножаем пачки и врагов в них на 2
        if (GameManager.CurrentDifficulty == GameManager.Difficulty.Hard)
        {
            finalGroupCount *= 2;
            finalEnemiesPerGroup *= 2;
        }

        for (int i = 0; i < finalGroupCount; i++)
        {
            if (spawner != null)
            {
                // Передаем измененное количество врагов
                Coroutine c = StartCoroutine(spawner.ExecuteSpawnCommand(currentWave.spawnType, finalEnemiesPerGroup));
                activeSpawnCoroutines.Add(c);
            }
        }
        // ----------------------------------------

        foreach (var coroutine in activeSpawnCoroutines)
        {
            yield return coroutine;
        }

        yield return new WaitForSeconds(currentStage.delayBetweenWaves);

        currentWaveIndex++;
        isWaveRunning = false;
    }
    else
    {
        // ЕСТЕСТВЕННЫЙ ПЕРЕХОД: Волны на этой стадии кончились
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ClearAllStageRemnants(); 
            currentStageIndex++;
            savedStageIndex = currentStageIndex; // Обновляем сохранение

            // Проверяем на победу ДО переключения оружия
            if (currentStageIndex >= stages.Count)
            {
                savedStageIndex = 0;
                GameManager.Instance.TriggerWin();
            }
            else
            {
                // Говорим GameManager сменить пушку под новый индекс стадии
                GameManager.Instance.SwitchToStage(currentStageIndex);
                StartCurrentStage();
            }
        }
    }
}

// Специальный метод для дебага по кнопке N
public void ForceSkipToNextStage()
{
    StopAllCoroutines();
    currentStageIndex++;
    
    if (currentStageIndex >= stages.Count)
    {
        savedStageIndex = 0; // Сбрасываем при полной победе
        if (GameManager.Instance != null) GameManager.Instance.TriggerWin();
    }
    else
    {
        savedStageIndex = currentStageIndex;
        
        // Синхронизируем пушку в GameManager под актуальный индекс
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SwitchToStage(currentStageIndex);
        }
        
        StartCurrentStage();
    }
}


    // Позволяет сбросить статическую переменную, если игрок выиграет или захочет начать заново из главного меню
    public static void ResetSavedStage()
    {
        savedStageIndex = 0;
    }
    
    public void StopGameForMenu()
    {
        isGameStarted = false;
        isWaveRunning = false;
        isPreparingStage = false;
        isTutorialActive = false; // <-- ДОБАВИЛИ СЮДА
        currentStageIndex = 0; 
        currentWaveIndex = 0;
    
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ClearAllStageRemnants();
        }
    
        StopAllCoroutines(); 
    }

}
