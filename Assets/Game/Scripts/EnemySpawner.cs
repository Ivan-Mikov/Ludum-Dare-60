using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    [Header("Ссылки на префабы")]
    [SerializeField] private GameObject enemyPrefab; 
    [SerializeField] private GameObject warningSignPrefab; // Сюда перетащи префаб знака !
    
    [Header("Границы экрана осциллографа")]
    [SerializeField] private float minX = -5.32f;
    [SerializeField] private float maxX = 1.44f;
    [SerializeField] private float minY = -2.02f;
    [SerializeField] private float maxY = 2.3f;

    // Главный метод-исполнитель, который вызывается из StageManager
    public IEnumerator ExecuteSpawnCommand(StageManager.SpawnType type, int count)
    {
        if (enemyPrefab == null) yield break;

        // Каждая параллельная группа считает свои личные случайные координаты за экраном
        Vector3 baseOutsidePos = CalculateRandomOutsidePosition();
        Vector3 warningPosition = ClampToScreen(baseOutsidePos);

        if (type == StageManager.SpawnType.Chain)
        {
            yield return StartCoroutine(SpawnChainRoutine(warningPosition, baseOutsidePos, count));
        }
        else if (type == StageManager.SpawnType.Swarm)
        {
            yield return StartCoroutine(SpawnSwarmRoutine(warningPosition, baseOutsidePos, count));
        }
    }
    public GameObject EnemyPrefab => enemyPrefab; 

    // ЛОГИКА ЦЕПОЧКИ: Враги выходят друг за другом из одной точки
    private IEnumerator SpawnChainRoutine(Vector3 warningPos, Vector3 spawnPos, int count)
    {
        GameObject sign = null;
        if (warningSignPrefab != null) sign = Instantiate(warningSignPrefab, warningPos, Quaternion.identity);
        
        yield return new WaitForSeconds(1.0f); // Восклицательный знак горит ровно 1 сек перед цепочкой
        if (sign != null) Destroy(sign);

        for (int i = 0; i < count; i++)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) yield break;
            
            Instantiate(enemyPrefab, spawnPos, Quaternion.identity, transform.parent);
            yield return new WaitForSeconds(0.4f); // Дистанция (интервал) между врагами в цепи
        }
    }

    // ЛОГИКА ТОЛПЫ: Все враги пачки вываливаются в один кадр широкой шеренгой
        private IEnumerator SpawnSwarmRoutine(Vector3 warningPos, Vector3 spawnPos, int count)
    {
        GameObject sign = null;
        if (warningSignPrefab != null) sign = Instantiate(warningSignPrefab, warningPos, Quaternion.identity);
        
        yield return new WaitForSeconds(1.0f); // Восклицательный знак горит ровно 1 секунду
        if (sign != null) Destroy(sign);

        // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Раскладываем толпу шеренгой строго ЗА пределами экрана
        for (int i = 0; i < count; i++)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) yield break;

            Vector3 finalPos = spawnPos;

            // Проверяем, где находится базовая точка спавна относительно экрана
            bool isVerticalSpawn = (spawnPos.y > maxY || spawnPos.y < minY);

            if (isVerticalSpawn)
            {
                // Если спавн СВЕРХУ или СНИЗУ — разбрасываем врагов ШЕРЕНГОЙ по горизонтали (ось X)
                // Даем случайное смещение влево-вправо, но по оси Y жестко держим позицию ЗА экраном
                float randomXOffset = Random.Range(-2.0f, 2.0f);
                float randomDepthOffset = Random.Range(0f, 0.5f); // Небольшой разброс в глубину ЗА экраном
                
                finalPos.x += randomXOffset;
                // Сдвигаем еще глубже наружу в зависимости от того, верх это или низ
                finalPos.y += (spawnPos.y > maxY) ? randomDepthOffset : -randomDepthOffset;
            }
            else
            {
                // Если спавн СЛЕВА или СПРАВА — разбрасываем врагов ШЕРЕНГОЙ по вертикали (ось Y)
                float randomYOffset = Random.Range(-2.0f, 2.0f);
                float randomDepthOffset = Random.Range(0f, 0.5f);
                
                finalPos.y += randomYOffset;
                // Сдвигаем еще глубже наружу в зависимости от того, право это или лево
                finalPos.x += (spawnPos.x > maxX) ? randomDepthOffset : -randomDepthOffset;
            }

            // Спавним конкретного врага в гарантированно безопасной "слепой" зоне
            Instantiate(enemyPrefab, finalPos, Quaternion.identity, transform.parent);
        }
    }


    // Вспомогательный метод расчета координат спавна строго ЗА пределами экрана
    private Vector3 CalculateRandomOutsidePosition()
    {
        Vector3 pos = Vector3.zero;
        float offset = 1.2f; // Расстояние за экраном
        int side = Random.Range(0, 4);

        switch (side)
        {
            case 0: // Слева
                pos = new Vector3(minX - offset, Random.Range(minY, maxY), 0); 
                break;
            case 1: // Справа
                pos = new Vector3(maxX + offset, Random.Range(minY, maxY), 0); 
                break;
            case 2: // Снизу
                pos = new Vector3(Random.Range(minX, maxX), minY - offset, 0); 
                break;
            case 3: // Сверху
                pos = new Vector3(Random.Range(minX, maxX), maxY + offset, 0); 
                break;
        }
        return pos;
    }

    // Ограничение координат, чтобы знак ! гарантированно оставался на зеленом экране
    private Vector3 ClampToScreen(Vector3 targetPos)
    {
        targetPos.x = Mathf.Clamp(targetPos.x, minX + 0.4f, maxX - 0.4f);
        targetPos.y = Mathf.Clamp(targetPos.y, minY + 0.4f, maxY - 0.4f);
        return targetPos;
    }

    // Отрисовка границ в Scene View для удобства отладки
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0f);
        Vector3 size = new Vector3(maxX - minX, maxY - minY, 1f);
        Gizmos.DrawWireCube(center, size);
    }
}
