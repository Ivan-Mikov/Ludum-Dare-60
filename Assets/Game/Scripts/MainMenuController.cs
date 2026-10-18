using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    [Header("Элементы UI")]
    [SerializeField] private GameObject menuPanel; // Перетащи сюда родительский объект меню, где лежат кнопки

    // Метод для кнопки Normal Mode
    public void SelectNormalMode()
    {
        Debug.Log("Выбран Normal Mode.");
        StartGameWithDifficulty(GameManager.Difficulty.Normal);
    }

    // Метод для кнопки Hard Mode
    public void SelectHardMode()
    {
        Debug.Log("Выбран Hard Mode!");
        StartGameWithDifficulty(GameManager.Difficulty.Hard);
    }

    // Кнопка туториала пока заглушена
    public void SelectTutorial()
    {
        Debug.Log("Запуск режима обучения...");

        // 1. Инициализируем стейдж туториала в GameManager
        GameManager.SetTutorialMode();

        // 2. Прячем UI главного меню
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }

        // 3. Запускаем корутину обучения в StageManager
        if (StageManager.Instance != null)
        {
            StageManager.Instance.StartTutorialFromMenu();
        }
    }


    private void StartGameWithDifficulty(GameManager.Difficulty difficulty)
    {
        // 1. Устанавливаем сложность в GameManager
        GameManager.SetDifficulty(difficulty);

        // 2. Прячем UI главного меню
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }

        // 3. Запускаем игровой процесс в StageManager
        if (StageManager.Instance != null)
        {
            StageManager.Instance.StartGameFromMenu();
        }
    }
    
    
}