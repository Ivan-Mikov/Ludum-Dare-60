using UnityEngine;

public class OscilloscopeSpawner : MonoBehaviour
{
    [Header("Шаблон препятствия")]
    public GameObject obstaclePrefab; // Сюда перетащим наш префаб

    [Header("Настройки времени (в секундах)")]
    public float spawnRate = 2.5f;     // Как часто вылетают столбы

    [Header("Рандом высоты зазора")]
    public float minHeightOffset = -100f; // Максимальный сдвиг вниз
    public float maxHeightOffset = 100f;  // Максимальный сдвиг вверх

    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnRate)
        {
            SpawnObstacle();
            timer = 0f;
        }
    }

    void SpawnObstacle()
    {
        // Считаем случайное смещение по высоте, чтобы зазор всегда был в разных местах
        float randomY = Random.Range(minHeightOffset, maxHeightOffset);
        
        // Позиция спавна: X берем от Спавнера, Y — случайный сдвиг
        Vector3 spawnPos = new Vector3(transform.localPosition.x, randomY, 0f);

        // Создаем новые столбы
        GameObject newObstacle = Instantiate(obstaclePrefab, transform.parent);
        
        // Выставляем правильные координаты внутри нашей Gameplay_Area
        newObstacle.transform.localPosition = spawnPos;
    }
}