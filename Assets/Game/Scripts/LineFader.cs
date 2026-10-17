using System.Collections;
using UnityEngine;

public class LineFader : MonoBehaviour
{
    [Header("Тайминги (в секундах)")]
    [SerializeField] private float lifetimeBeforeFade = 3f; // Время полноценной жизни
    [SerializeField] private float fadeDuration = 3f;        // Время плавного исчезновения

    private LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer != null)
        {
            // Запускаем корутину затухания сразу при материализации объекта
            StartCoroutine(FadeAndDestroyRoutine());
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] Компонент LineRenderer не найден! Затухание отменено.");
            Destroy(gameObject);
        }
    }

    private IEnumerator FadeAndDestroyRoutine()
    {
        // 1. Ждем 3 секунды полноценной жизни объекта
        yield return new WaitForSeconds(lifetimeBeforeFade);

        // Запоминаем исходный градиент цветов из LineRenderer
        Gradient startGradient = lineRenderer.colorGradient;
        GradientAlphaKey[] startAlphaKeys = startGradient.alphaKeys;
        GradientColorKey[] colorKeys = startGradient.colorKeys;

        float elapsedTime = 0f;

        // 2. Плавно гасим прозрачность в течение следующих 3 секунд
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            
            // Вычисляем текущий процент прозрачности (от 1 до 0)
            float alphaPercentage = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);

            // Создаем новые ключи прозрачности для градиента
            GradientAlphaKey[] newAlphaKeys = new GradientAlphaKey[startAlphaKeys.Length];
            for (int i = 0; i < startAlphaKeys.Length; i++)
            {
                // Умножаем изначальную альфу каждой точки градиента на текущий коэффициент затухания
                newAlphaKeys[i] = new GradientAlphaKey(startAlphaKeys[i].alpha * alphaPercentage, startAlphaKeys[i].time);
            }

            // Собираем обновленный градиент и отдаем его LineRenderer
            Gradient newGradient = new Gradient();
            newGradient.SetKeys(colorKeys, newAlphaKeys);
            lineRenderer.colorGradient = newGradient;

            yield return null; // Ждем следующий кадр
        }

        // 3. Линия полностью растворилась — удаляем объект со сцены
        Destroy(gameObject);
    }
}
