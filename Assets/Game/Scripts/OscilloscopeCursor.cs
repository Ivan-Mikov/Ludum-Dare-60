using System.Collections.Generic;
using UnityEngine;

public class PointDrawer : MonoBehaviour
{
    [Header("Настройки рисования")]
    [SerializeField] private GameObject drawnObjectPrefab; 
    [SerializeField] private float minDistanceBetweenPoints = 0.1f; 
    [Header("Автозамыкание фигур")]
    [SerializeField] private float snapDistance = 0.6f; // Чуть увеличили для комфортного замыкания

    private Rigidbody2D rb;
    private Camera mainCamera;
    private LineRenderer currentLineRenderer;
    private List<Vector2> points = new List<Vector2>();
    private Vector2 lastPoint;
    private bool isDrawing = false;
    private bool shouldClosedThisFrame = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        if (rb != null) rb.freezeRotation = true;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) StartDrawing();
        
        // Рисуем только если флаг замыкания еще не взведен
        if (isDrawing && Input.GetMouseButton(0) && !shouldClosedThisFrame) 
        {
            Drawing();
        }

        if (Input.GetMouseButtonUp(0) || shouldClosedThisFrame) 
        {
            StopDrawing();
        }
    }

    void FixedUpdate()
    {
        if (mainCamera != null && rb != null)
        {
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            rb.MovePosition(new Vector2(mousePosition.x, mousePosition.y));
        }
    }

    private void StartDrawing()
    {
        isDrawing = true;
        shouldClosedThisFrame = false;
        points.Clear();
        
        GameObject newLine = Instantiate(drawnObjectPrefab, transform.position, Quaternion.identity, transform.parent); 
        currentLineRenderer = newLine.GetComponent<LineRenderer>();
        
        newLine.GetComponent<Rigidbody2D>().simulated = false;
        newLine.GetComponent<PolygonCollider2D>().enabled = false;

        currentLineRenderer.useWorldSpace = false;
        AddPoint(Vector2.zero);
    }

    private void Drawing()
    {
        Vector2 localCurrentPos = currentLineRenderer.transform.InverseTransformPoint(transform.position);
        Vector2 localLastPos = points[points.Count - 1];

        if (Vector2.Distance(localCurrentPos, localLastPos) > minDistanceBetweenPoints)
        {
            // Условие автозамыкания: точек больше 3, и мы вернулись к старту
            if (points.Count > 3 && Vector2.Distance(localCurrentPos, Vector2.zero) < snapDistance)
            {
                AddPoint(Vector2.zero); 
                shouldClosedThisFrame = true; // Сигнализируем о мягком завершении в конце кадра
                return;
            }

            AddPoint(localCurrentPos);
        }
    }

    private void AddPoint(Vector2 localPoint)
    {
        points.Add(localPoint);
        currentLineRenderer.positionCount = points.Count;
        currentLineRenderer.SetPosition(points.Count - 1, new Vector3(localPoint.x, localPoint.y, 0));
    }

        private void StopDrawing()
    {
        if (!isDrawing) return;
        isDrawing = false;
        shouldClosedThisFrame = false;

        // Если точек мало для полигона, жестко и гарантированно удаляем фантом
        if (points.Count < 3) 
        {
            if (currentLineRenderer != null) 
            {
                Destroy(currentLineRenderer.gameObject);
                currentLineRenderer = null;
            }
            return;
        }

        GameObject finishedObject = currentLineRenderer.gameObject;
        Rigidbody2D objRb = finishedObject.GetComponent<Rigidbody2D>();
        PolygonCollider2D objCollider = finishedObject.GetComponent<PolygonCollider2D>();

        if (objCollider == null || objRb == null) return;

        objCollider.points = points.ToArray();
        objCollider.enabled = true;
        
        PushPlayerOutBounds(objCollider);

        objRb.simulated = true;
        SetCenterOfMass(objRb, points);

        // --- МЕХАНИКА: ОЖИВЛЕНИЕ ОБЪЕКТА ---
        // Генерируем случайное направление (случайный угол в радианах)
        float randomAngle = Random.Range(0f, Mathf.PI * 2f);
        Vector2 randomDirection = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
        
        // Задаем очень маленькую начальную скорость (например, 0.5f)
        float startSpeed = 0.5f; 
        objRb.linearVelocity = randomDirection * startSpeed;
        
        // Сбрасываем ссылку, чтобы быть готовым к новому рисунку
        currentLineRenderer = null;
    }

    private void SetCenterOfMass(Rigidbody2D rb, List<Vector2> pointsList)
    {
        Vector2 center = Vector2.zero;
        foreach (var p in pointsList) center += p;
        center /= pointsList.Count;

        Vector3 worldCenter = rb.transform.TransformPoint(center);
        rb.transform.position = worldCenter;

        // Важно: проверяем, что LineRenderer еще существует перед изменением позиций
        LineRenderer lr = rb.GetComponent<LineRenderer>();
        for (int i = 0; i < pointsList.Count; i++)
        {
            pointsList[i] -= center;
            if (lr != null)
            {
                lr.SetPosition(i, new Vector3(pointsList[i].x, pointsList[i].y, 0));
            }
        }

        PolygonCollider2D objCollider = rb.GetComponent<PolygonCollider2D>();
        if (objCollider != null)
        {
            objCollider.points = pointsList.ToArray();
        }
        rb.centerOfMass = Vector2.zero;
    }


    private void PushPlayerOutBounds(PolygonCollider2D collider)
    {
        // Проверяем, находится ли точка игрока внутри созданного полигона
        if (collider.OverlapPoint(transform.position))
        {
            Vector2 playerPos = transform.position;

            // Находим БЛИЖАЙШУЮ точку на границе коллайдера к курсору игрока
            Vector2 closestPoint = collider.ClosestPoint(playerPos);

            // Направление выталкивания — от ближайшей точки к игроку (или от центра, если они совпали)
            Vector2 pushDirection = (playerPos - closestPoint).normalized;
            if (pushDirection == Vector2.zero)
            {
                Vector2 boundsCenter = collider.bounds.center;
                pushDirection = (playerPos - boundsCenter).normalized;
                if (pushDirection == Vector2.zero) pushDirection = Vector2.up;
            }

            // Сдвигаем игрока ровно за край границы + микро-зазор (0.15f), чтобы не было залипания
            Vector2 safePosition = closestPoint + (pushDirection * 0.15f);

            // Мягко перемещаем трансформ и физическое тело без диких прыжков
            transform.position = safePosition;
            if (rb != null)
            {
                rb.position = safePosition;
                rb.linearVelocity = Vector2.zero; // Сбрасываем накопленную скорость, чтобы не было рывка
            }
        }
    }
}
