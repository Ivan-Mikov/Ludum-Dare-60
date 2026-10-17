using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using DG.Tweening;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum TutorialState { DrawLines, EnergyNotice, KillPhantoms, Complete }
    [Header("Текущий этап обучения")]
    [SerializeField] private TutorialState currentStage = TutorialState.DrawLines;

    [Header("Ссылки на компоненты UI")]
    [SerializeField] private TextMeshProUGUI tutorialTextMesh; 
    [SerializeField] private Image energyBarFillImage;     

    [Header("Настройки спавна фантомов")]
    [SerializeField] private GameObject phantomEnemyPrefab; 

    [Header("Настройки цвета шкалы (Задаются кодом)")]
    [SerializeField] private Color normalGameplayColor = Color.red; 
    [SerializeField] private Color clickWarningColor = Color.white; 

    [Header("Настройки одиночной анимации")]
    [SerializeField] private float pulseScaleY = 1.4f;         
    [SerializeField] private float animationDuration = 0.12f;   

    private PointDrawer pointDrawer;
    private Vector3 originalScale;

    private int linesDrawnCount = 0;
    private int phantomsKilledCount = 0;

    public TutorialState CurrentStage => currentStage;

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

        pointDrawer = Object.FindFirstObjectByType<PointDrawer>();
        UpdateTutorialText();
    }

    void Update()
    {
        if (pointDrawer == null || energyBarFillImage == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (energyBarFillImage.fillAmount < 0.2f)
            {
                PlayDoubleLoopClickWarning();
            }
        }
    }

    public void RegisterLineDrawn()
    {
        if (currentStage != TutorialState.DrawLines && currentStage != TutorialState.EnergyNotice) return;

        linesDrawnCount++;

        if (linesDrawnCount == 2)
        {
            currentStage = TutorialState.EnergyNotice;
        }
        else if (linesDrawnCount >= 4)
        {
            currentStage = TutorialState.KillPhantoms;
            // Первого фантома спавним сразу, без задержки
            SpawnPhantomEnemy(); 
        }

        UpdateTutorialText();
    }

    public void RegisterPhantomKilled()
    {
        if (currentStage != TutorialState.KillPhantoms) return;

        phantomsKilledCount++;
        UpdateTutorialText();

        if (phantomsKilledCount >= 3)
        {
            currentStage = TutorialState.Complete;
            UpdateTutorialText();
            // Запускаем таймер на скрытие текста через 5 секунд
            StartCoroutine(HideTutorialTextAfterDelay(5f));
            Debug.Log("🎉 Обучение успешно завершено!");
        }
        else
        {
            // ЗАДЕРЖКА В 2 СЕКУНДЫ: вызываем метод спавна через встроенный таймер Unity
            Invoke(nameof(SpawnPhantomEnemy), 2f);
        }
    }

    private void SpawnPhantomEnemy()
    {
        if (phantomEnemyPrefab == null) return;

        Vector3 spawnPos = Vector3.zero;

        if (phantomsKilledCount == 0) spawnPos = new Vector3(1.44f, 0.14f, 0f);
        else if (phantomsKilledCount == 1) spawnPos = new Vector3(-1.94f, 2.30f, 0f);
        else spawnPos = new Vector3(-1.94f, -2.02f, 0f);

        Instantiate(phantomEnemyPrefab, spawnPos, Quaternion.identity);
    }

    private void UpdateTutorialText()
    {
        if (tutorialTextMesh == null) return;

        switch (currentStage)
        {
            case TutorialState.DrawLines:
                tutorialTextMesh.text = $"HOLD LMB TO DRAW SHAPES! ({linesDrawnCount}/4)";
                break;
            case TutorialState.EnergyNotice:
                tutorialTextMesh.text = $"CAUTION! DRAWING CONSUMES ENERGY SCALE! ({linesDrawnCount}/4)";
                break;
            case TutorialState.KillPhantoms:
                tutorialTextMesh.text = $"PHANTOM DETECTED! PUSH SHAPES TO DESTROY IT! ({phantomsKilledCount}/3)";
                break;
            case TutorialState.Complete:
                tutorialTextMesh.text = "TUTORIAL COMPLETE! PREPARE FOR SIMULATION.";
                break;
        }
    }

    // Корутина для плавного исчезновения финального текста
    private IEnumerator HideTutorialTextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (tutorialTextMesh != null)
        {
            // Плавно скрываем за полсекунды через DOTween
            tutorialTextMesh.DOFade(0f, 0.5f).OnComplete(() => {
                tutorialTextMesh.gameObject.SetActive(false);
            });
        }
    }

    private void PlayDoubleLoopClickWarning()
    {
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
}
