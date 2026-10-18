using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using DG.Tweening;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameStage 
    { 
        Stage0_Tutorial, // <-- ДОБАВИЛИ СЮДА
        Stage1_TutorialAndDefaultEnemies, 
        Stage2_Shotgun,                   
        Stage3_Ricochet,                  
        Stage4_Laser,                     
        Stage5_Bulldozer,                 
        Win, 
        GameOver 
    }

    public enum Difficulty { Normal, Hard }
    // Статическое свойство, чтобы любая сцена и скрипт знали текущую сложность
    public static Difficulty CurrentDifficulty { get; private set; } = Difficulty.Normal;
    
    [Header("Текущая стадия игры")]
    [SerializeField] private GameStage currentStage = GameStage.Stage1_TutorialAndDefaultEnemies;

    [Header("Ссылки на компоненты UI")]
    [SerializeField] private TextMeshProUGUI tutorialTextMesh; 
    [SerializeField] private Image energyBarFillImage;     

    [Header("Настройки спавна фантомов (Стадия 1)")]
    [SerializeField] private GameObject phantomEnemyPrefab; 

    [Header("Настройки цвета шкалы энергии")]
    [SerializeField] private Color normalGameplayColor = Color.red; 
    [SerializeField] private Color clickWarningColor = Color.white; 

    [Header("Настройки анимации шкалы")]
    [SerializeField] private float pulseScaleY = 1.4f;         
    [SerializeField] private float animationDuration = 0.12f;   
    
    [Header("Финальный Босс")]
    [SerializeField] private GameObject bossPrefab; // Перетащи сюда префаб босса

    [Header("Меню")]
    [SerializeField] private GameObject menuPanel; // Сюда перетащи ту же MenuPanel из Canvas

    private Vector3 originalScale;
    private int stageKillsCount = 0; 

    public GameStage CurrentStage => currentStage;
    public bool IsGameOver => currentStage == GameStage.Win || currentStage == GameStage.GameOver;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (energyBarFillImage != null)
        {
            energyBarFillImage.type = Image.Type.Filled;
            energyBarFillImage.color = normalGameplayColor;
        }
    }

    void Start()
    {
        if (energyBarFillImage != null)
        {
            originalScale = energyBarFillImage.transform.localScale;
        }
    }

    void Update()
    {
        // Проверяем нажатие кнопки перезапуска, только если игра официально завершена (Победа или Поражение)
        if (IsGameOver)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                // СЦЕНАРИЙ 1: Игрок ПОБЕДИЛ финального босса
                // СЦЕНАРИЙ 1: Игрок победил босса — выходим в меню
                if (currentStage == GameStage.Win)
                {
                    Debug.Log("🎉 Победный возврат в меню.");

                    // 1. Сначала останавливаем и сбрасываем игровой цикл в StageManager
                    if (StageManager.Instance != null)
                    {
                        StageManager.Instance.StopGameForMenu();
                    }

                    // 2. СРАЗУ переводим стейдж в исходное состояние (иначе сработает блокировка GameOver)
                    currentStage = GameStage.Stage1_TutorialAndDefaultEnemies;

                    // 3. Теперь принудительно гасим и очищаем текст на экране
                    if (tutorialTextMesh != null)
                    {
                        tutorialTextMesh.text = "";
                        tutorialTextMesh.gameObject.SetActive(false);
                    }

                    // 4. Включаем панель главного меню обратно
                    if (menuPanel != null)
                    {
                        menuPanel.SetActive(true);
                    }
                }

                // СЦЕНАРИЙ 2: Игрок ПРОИГРАЛ (GameOver)
                else if (currentStage == GameStage.GameOver)
                {
                    Debug.Log("💀 Перезапускаем стадию, на которой игрок потерпел поражение...");
                
                    // Загружаем сцену заново (StageManager автоматически загрузит сохраненный индекс стадии)
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }
            }
            return; // Если игра окончена, дальнейший код Update (кнопка N) не должен выполняться
        }

        // Отладочный переход на следующую стадию во время геймплея
        if (Input.GetKeyDown(KeyCode.N))
        {
            AdvanceStageDebug();
        }
    }

    
    // Синхронизация стадии из StageManager при старте уровня
    public void SyncStageOnLoad(int stageIndex)
    {
        if (stageIndex < (int)GameStage.Win)
        {
            currentStage = (GameStage)stageIndex;
            TriggerStageStartMethod(currentStage);
            Debug.Log("🔄 GameManager синхронизировал стадию при старте: " + currentStage);
        }
    }

    public void SetCustomTutorialText(string text)
    {
        if (tutorialTextMesh == null) return;
        // ТАКЖЕ УДАЛИТЕ ИЛИ ЗАКОММЕНТИРУЙТЕ СТРОКУ НИЖЕ:
        // if (IsGameOver) return; 

        tutorialTextMesh.gameObject.SetActive(text != "");
        tutorialTextMesh.text = text;
        tutorialTextMesh.color = Color.white;
    }


    public void TriggerGameOver()
    {
        if (IsGameOver) return;

        currentStage = GameStage.GameOver; 
        
        if (tutorialTextMesh != null)
        {
            tutorialTextMesh.gameObject.SetActive(true);
            tutorialTextMesh.text = "GAME OVER! PRESS R TO RESTART STAGE";
        }
        
        Debug.Log("💀 Игра окончена! Персонаж уничтожен.");
    }

    public void TriggerWin()
    {
        if (IsGameOver) return;

        currentStage = GameStage.Win; 
        StageManager.ResetSavedStage(); // Сбрасываем прогресс стадий, так как игра пройдена!
        
        if (tutorialTextMesh != null)
        {
            tutorialTextMesh.gameObject.SetActive(true);
            tutorialTextMesh.text = "YOU WIN! PERFECT SIMULATION COMPLETED! PRESS R";
        }
        
        Debug.Log("🎉 Победа! Все стадии StageManager успешно пройдены.");
    }

    public void RegisterEnemyKilled()
    {
        stageKillsCount++; 

        if (currentStage == GameStage.Stage1_TutorialAndDefaultEnemies)
        {
            if (tutorialTextMesh != null) tutorialTextMesh.text = "STAGE 1: DESTROY INCOMING ENEMIES!";
        }
        else if (currentStage == GameStage.Stage2_Shotgun)
        {
            if (tutorialTextMesh != null) tutorialTextMesh.text = "STAGE 2: SWARMS DETECTED! USE SHOTGUN!";
        }
        else if (currentStage == GameStage.Stage3_Ricochet)
        {
            if (tutorialTextMesh != null) tutorialTextMesh.text = "STAGE 3: RICOCHET MODE ACTIVE!";
        }
    }

    private void UpdateStageUI() {}

    // --- МЕТОДЫ СТАДИЙ ---
    private void StartStage1_DefaultEnemies()
    {
        Debug.Log("🚀 Началась Стадия 1: Дефолтные враги.");
        stageKillsCount = 0;
    }

    private void StartStage2_Shotgun()
    {
        Debug.Log("🔥 Началась Стадия 2: Дробовик на 5 пуль.");
        stageKillsCount = 0; 
    }

    private void StartStage3_Ricochet()
    {
        Debug.Log("🧱 Началась Стадия 3: Рикошеты от стен.");
        stageKillsCount = 0;
    }

    private void StartStage4_Laser()
    {
        Debug.Log("⚡ Началась Стадия 4: Лазер с нагревом.");
        stageKillsCount = 0;
    }

    private void StartStage5_Bulldozer()
    {
        Debug.Log("👾 СТАДИЯ 5: ВНИМАНИЕ! ОБНАРУЖЕНА МАТЬ ВСЕХ ТВАРЕЙ!");
        stageKillsCount = 0;

        // ТОЧЕЧНЫЙ ФИКС: Запускаем спавн босса с задержкой в 5 секунд, пока висит надпись
        StartCoroutine(SpawnBossWithDelay(5f));
    }

    private IEnumerator SpawnBossWithDelay(float delay)
    {
        // Ждем ровно 5 секунд (пока на экране горит заставка STAGE 5)
        yield return new WaitForSeconds(delay);

        if (bossPrefab != null)
        {
            // Только теперь, когда заставка кончилась, спавним босса за экраном
            Vector3 spawnPos = new Vector3(-8.0f, 0.14f, 0.0f); 
            Instantiate(bossPrefab, spawnPos, Quaternion.identity);
            Debug.Log("👾 Босс успешно заспавнен после 5-секундного ожидания!");
        }
        else
        {
            Debug.LogError("❌ ОШИБКА: Префаб босса забыли перетащить в инспектор GameManager!");
        }
    }



    // --- СИСТЕМНЫЕ МЕТОДЫ И ОТЛАДКА ---

    // Полностью исправленная функция отладки для клавиши N
    public void AdvanceStageDebug()
    {
        if (IsGameOver) return;

        // 1. Моментально зачищаем сцену от старых врагов и осколков
        ClearAllStageRemnants();

        // 2. Сбрасываем старые таймеры заставок
        StopAllCoroutines(); 

        // 3. Передаем управление в StageManager — пусть он сам переключит на следующую стадию
        if (StageManager.Instance != null)
        {
            StageManager.Instance.ForceSkipToNextStage();
        }
    }


    public void PlayDoubleLoopClickWarning()
    {
        if (IsGameOver) return;

        energyBarFillImage.DOKill();
        energyBarFillImage.transform.DOKill();
        energyBarFillImage.color = normalGameplayColor;
        energyBarFillImage.transform.localScale = originalScale;
        energyBarFillImage.DOColor(clickWarningColor, animationDuration).SetLoops(4, LoopType.Yoyo).SetEase(Ease.InOutSine);
        energyBarFillImage.transform.DOScaleY(originalScale.y * pulseScaleY, animationDuration).SetLoops(4, LoopType.Yoyo).SetEase(Ease.InOutSine);
    }

    private void OnDestroy()
    {
        if (energyBarFillImage != null)
        {
            energyBarFillImage.DOKill();
            energyBarFillImage.transform.DOKill();
        }
    }
    
    public int GetStageKillsCount() => stageKillsCount;
    
    public void ClearAllStageRemnants()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            Destroy(enemy);
        }

        EnemyPiece[] pieces = GameObject.FindObjectsOfType<EnemyPiece>();
        foreach (EnemyPiece piece in pieces)
        {
            Destroy(piece.gameObject);
        }
    
        Debug.Log("🧹 Сцена полностью очищена от остатков прошлой стадии!");
    }
    
    // Метод переключения логики при смене стадии
    private void TriggerStageStartMethod(GameStage stage)
    {
        switch (stage)
        {
            case GameStage.Stage0_Tutorial:                  Debug.Log("🎓 Началось обучение."); break; // <-- ДОБАВИЛИ
            case GameStage.Stage1_TutorialAndDefaultEnemies: StartStage1_DefaultEnemies(); break;
            case GameStage.Stage2_Shotgun:                   StartStage2_Shotgun(); break;
            case GameStage.Stage3_Ricochet:                  StartStage3_Ricochet(); break;
            case GameStage.Stage4_Laser:                     StartStage4_Laser(); break;
            case GameStage.Stage5_Bulldozer:                 StartStage5_Bulldozer(); break;
        }
    }

    // Метод для корректного перехода на следующую стадию без двойных инкрементов
    public void SwitchToStage(int stageIndex)
    {
        if (IsGameOver) return;

        // ТОЧЕЧНЫЙ ФИКС: Если пришел индекс 5-й стадии (Босса)
        if (stageIndex == 4)
        {
            // Выключаем корутины StageManager, чтобы он не успел объявить победу раньше времени
            if (StageManager.Instance != null) StageManager.Instance.StopAllCoroutines();

            currentStage = GameStage.Stage5_Bulldozer;
            TriggerStageStartMethod(currentStage);
            Debug.Log("👾 GameManager: Перехвачен индекс 4! Спавним Босса.");
            return;
        }

        // Твой старый код ниже остается без изменений:
        if (stageIndex < (int)GameStage.Win)
        {
            currentStage = (GameStage)stageIndex;
            TriggerStageStartMethod(currentStage);
            Debug.Log("🎯 Оружие успешно переключено на стадию: " + currentStage);
        }
        else
        {
            TriggerWin();
        }
    }

    // Метод для загрузки игры из главного меню
    public static void SetDifficulty(Difficulty difficulty)
    {
        CurrentDifficulty = difficulty;
        StageManager.ResetSavedStage(); 
    }

    public static void SetTutorialMode()
    {
        CurrentDifficulty = Difficulty.Normal; // В туториале всегда нормальная сложность
        StageManager.ResetSavedStage(); 
    
        if (Instance != null)
        {
            Instance.currentStage = GameStage.Stage0_Tutorial;
        }
    }


}
