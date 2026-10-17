using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Настройки спавна")]
    [SerializeField] private GameObject enemyPrefab; 
    [SerializeField] private float spawnInterval = 2.5f; // Немного увеличили интервал для баланса
    
    [Header("Видимые границы экрана осциллографа")]
    [SerializeField] private float minX = -5f;
    [SerializeField] private float maxX = 5f;
    [SerializeField] private float minY = -3f;
    [SerializeField] private float maxY = 3f;

    private float timer;

    void Start()
    {
        timer = spawnInterval;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            SpawnEnemyOutside();
            timer = spawnInterval;
        }
    }

    private void SpawnEnemyOutside()
    {
        if (enemyPrefab == null) return;

        Vector3 spawnPosition = Vector3.zero;
        float offset = 1.2f; // Отступ за экраном, чтобы враг не появлялся внезапно

        // Выбираем случайную сторону для спавна: 0 - Лево, 1 - Право, 2 - Низ, 3 - Верх
        int side = Random.Range(0, 4);

        switch (side)
        {
            case 0: // Слева за экраном
                spawnPosition = new Vector3(minX - offset, Random.Range(minY, maxY), 0);
                break;
            case 1: // Справа за экраном
                spawnPosition = new Vector3(maxX + offset, Random.Range(minY, maxY), 0);
                break;
            case 2: // Снизу за экраном
                spawnPosition = new Vector3(Random.Range(minX, maxX), minY - offset, 0);
                break;
            case 3: // Сверху за экраном
                spawnPosition = new Vector3(Random.Range(minX, maxX), maxY + offset, 0);
                break;
        }

        // Просто спавним врага. Он сам активирует ИИ преследования в первом же кадре
        Instantiate(enemyPrefab, spawnPosition, Quaternion.identity, transform.parent);
    }
}