using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PointDrawer : MonoBehaviour
{
    [Header("Настройки рисования")]
    [SerializeField] private GameObject drawnObjectPrefab; 
    [SerializeField] private float minDistanceBetweenPoints = 0.1f; 
    [SerializeField] private float thickness = 0.25f;
    
    [Header("Автозамыкание фигур")]
    [SerializeField] private float snapDistance = 0.6f; 

    [Header("Настройки Энергии (Осциллограф)")]
    [SerializeField] private Image energyBarFillImage;
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField] private float consumeRate = 30f;
    [SerializeField] private float rechargeRate = 15f;

    private Rigidbody2D rb;
    private Collider2D playerCollider; 
    private Camera mainCamera;
    private LineRenderer currentLineRenderer;
    private List<Vector2> points = new List<Vector2>();
    
    private bool isDrawing = false;
    private bool shouldClosedThisFrame = false;

    private float currentEnergy;
    private bool isEnergyLocked = false;
    // Публичный доступ для GameManager, чтобы он мог читать состояние блокировки
    public bool IsEnergyLocked => isEnergyLocked;


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        mainCamera = Camera.main;

        if (rb != null) rb.freezeRotation = true;

        currentEnergy = maxEnergy;
    }


    void Update()
    {
        HandleEnergy();
        HandleDrawingInput();
    }

    private void FixedUpdate()
    {
        MoveToCursor();        // ← Движение за курсором теперь здесь
    }

    private void MoveToCursor()
    {
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;

        // ЖЕСТКОЕ ОГРАНИЧЕНИЕ КУРСОРA В ПРЕДЕЛАХ ЗЕЛЕНОГО ЭКРАНА
        float clampedX = Mathf.Clamp(mousePos.x, -5.32f, 1.44f);
        float clampedY = Mathf.Clamp(mousePos.y, -2.02f, 2.3f);
        Vector2 clampedPos = new Vector2(clampedX, clampedY);

        if (rb != null)
            rb.MovePosition(clampedPos);
        else
            transform.position = clampedPos;
    }


    private void HandleEnergy()
    {
        if (energyBarFillImage != null)
            energyBarFillImage.fillAmount = currentEnergy / maxEnergy;

        if (!isDrawing)
        {
            currentEnergy = Mathf.MoveTowards(currentEnergy, maxEnergy, rechargeRate * Time.deltaTime);

            if (isEnergyLocked && currentEnergy >= maxEnergy * 0.2f)
            {
                isEnergyLocked = false;
            }
        }
        else
        {
            currentEnergy -= consumeRate * Time.deltaTime;

            if (currentEnergy <= 0f)
            {
                currentEnergy = 0f;
                isEnergyLocked = true;
                StopDrawing();
                Debug.Log("Энергия закончилась — рисование прервано");
            }
        }
    }

    private void HandleDrawingInput()
    {
        if (Input.GetMouseButtonDown(0)) // Нажали ЛКМ
        {
            if (!isEnergyLocked && currentEnergy >= maxEnergy * 0.2f)
            {
                StartNewLine();
            }
            else if (!isEnergyLocked)
            {
                Debug.Log("⚠️ Заряд слишком низкий! Нужно хотя бы 20%");
            }
        }

        if (isDrawing && currentLineRenderer != null)
        {
            Drawing();
        }

        if (Input.GetMouseButtonUp(0) && isDrawing)
        {
            StopDrawing();
        }
    }

    private void StartNewLine()
    {
        if (drawnObjectPrefab == null) return;

        GameObject newLine = Instantiate(drawnObjectPrefab, Vector3.zero, Quaternion.identity);
        currentLineRenderer = newLine.GetComponent<LineRenderer>();
        points.Clear();
        shouldClosedThisFrame = false;
        isDrawing = true;

        if (currentLineRenderer != null)
        {
            currentLineRenderer.positionCount = 0;
            currentLineRenderer.useWorldSpace = false;

            Vector2 firstPoint = currentLineRenderer.transform.InverseTransformPoint(transform.position);
            AddPoint(firstPoint);
        }
    }

    private void Drawing()
    {
        Vector2 localCurrentPos = currentLineRenderer.transform.InverseTransformPoint(transform.position);
        Vector2 localLastPos = points[points.Count - 1];

        if (Vector2.Distance(localCurrentPos, localLastPos) > minDistanceBetweenPoints)
        {
            if (points.Count > 3 && Vector2.Distance(localCurrentPos, Vector2.zero) < snapDistance)
            {
                AddPoint(Vector2.zero);
                shouldClosedThisFrame = true;
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

        bool closedFigure = shouldClosedThisFrame;
        shouldClosedThisFrame = false;

        if (points.Count < 3 || currentLineRenderer == null)
        {
            if (currentLineRenderer != null)
                Destroy(currentLineRenderer.gameObject);
            currentLineRenderer = null;
            return;
        }

        GameObject finishedObject = currentLineRenderer.gameObject;
        Rigidbody2D objRb = finishedObject.GetComponent<Rigidbody2D>();
        PolygonCollider2D objCollider = finishedObject.GetComponent<PolygonCollider2D>();

        if (objCollider == null || objRb == null)
        {
            Destroy(finishedObject);
            currentLineRenderer = null;
            return;
        }

        Vector2[] polyPoints = CreateThickMeshPoints(points, closedFigure);
        objCollider.points = polyPoints;
        objCollider.enabled = true;
        
        SetCenterOfMass(objRb, objCollider, points);
        objRb.simulated = true;

        // Отталкивание игрока при наложении
        if (playerCollider != null)
        {
            ColliderDistance2D dist = playerCollider.Distance(objCollider);
            if (dist.isOverlapped)
            {
                Vector2 pushDir = dist.normal != Vector2.zero ? dist.normal : Vector2.up;
                transform.position += (Vector3)(pushDir * (thickness + 0.3f));
                if (rb != null) rb.position = transform.position;
            }
        }

        float randomAngle = Random.Range(0f, Mathf.PI * 2f);
        objRb.linearVelocity = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * 1.6f;
        
        // --- СИГНАЛ ДЛЯ ТУТОРИАЛА ---
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterLineDrawn();
        }

        currentLineRenderer = null; // Эта строка у тебя уже есть

    }

    // ==================== ТВОИ СТАРЫЕ МЕТОДЫ (с небольшими правками) ====================

    private Vector2[] CreateThickMeshPoints(List<Vector2> linePoints, bool isClosed)
    {
        List<Vector2> forwardPoints = new List<Vector2>();
        List<Vector2> backwardPoints = new List<Vector2>();

        for (int i = 0; i < linePoints.Count; i++)
        {
            Vector2 normal = Vector2.zero;

            if (i == 0)
            {
                Vector2 dir = (linePoints[1] - linePoints[0]).normalized;
                normal = new Vector2(-dir.y, dir.x);
            }
            else if (i == linePoints.Count - 1 && !isClosed)
            {
                Vector2 dir = (linePoints[i] - linePoints[i - 1]).normalized;
                normal = new Vector2(-dir.y, dir.x);
            }
            else
            {
                int nextIndex = (i + 1) % linePoints.Count;
                Vector2 dir1 = (linePoints[i] - linePoints[i - 1]).normalized;
                Vector2 dir2 = (linePoints[nextIndex] - linePoints[i]).normalized;
                Vector2 avgDir = (dir1 + dir2).normalized;
                normal = new Vector2(-avgDir.y, avgDir.x);
            }

            forwardPoints.Add(linePoints[i] + normal * (thickness * 0.5f));
            backwardPoints.Add(linePoints[i] - normal * (thickness * 0.5f));
        }

        List<Vector2> combined = new List<Vector2>(forwardPoints);
        backwardPoints.Reverse();
        combined.AddRange(backwardPoints);

        if (isClosed)
            combined.Add(combined[0]);

        return combined.ToArray();
    }

    private void SetCenterOfMass(Rigidbody2D rb, PolygonCollider2D collider, List<Vector2> originalPoints)
    {
        Vector2 center = Vector2.zero;
        foreach (var p in originalPoints) center += p;
        center /= originalPoints.Count;

        Vector3 worldCenter = rb.transform.TransformPoint(center);
        rb.transform.position = worldCenter;

        LineRenderer lr = rb.GetComponent<LineRenderer>();
        for (int i = 0; i < originalPoints.Count; i++)
        {
            originalPoints[i] -= center;
            if (lr != null) lr.SetPosition(i, new Vector3(originalPoints[i].x, originalPoints[i].y, 0));
        }

        Vector2[] colPoints = collider.points;
        for (int i = 0; i < colPoints.Length; i++)
            colPoints[i] -= center;

        collider.points = colPoints;
        rb.centerOfMass = Vector2.zero;
    }
}