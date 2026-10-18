using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StageManager : MonoBehaviour
{
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
        
        // Синхронизируем GameManager под восстановленную стадию
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SyncStageOnLoad(currentStageIndex);
        }

        StartCurrentStage();
    }

    void Update()
    {
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

    if (currentWaveIndex < stages[currentStageIndex].waves.Count)
    {
        StageSetup currentStage = stages[currentStageIndex];
        Wave currentWave = currentStage.waves[currentWaveIndex];
        
        List<Coroutine> activeSpawnCoroutines = new List<Coroutine>();

        for (int i = 0; i < currentWave.groupCount; i++)
        {
            if (spawner != null)
            {
                Coroutine c = StartCoroutine(spawner.ExecuteSpawnCommand(currentWave.spawnType, currentWave.enemiesPerGroup));
                activeSpawnCoroutines.Add(c);
            }
        }

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
}
