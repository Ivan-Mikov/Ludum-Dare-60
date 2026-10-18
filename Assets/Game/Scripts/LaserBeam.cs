using UnityEngine;
using DG.Tweening;

public class LaserBeam : MonoBehaviour
{
    [Header("Настройки анимации DOTween")] [SerializeField]
    private float appearanceDuration = 0.15f;

    [SerializeField] private float disappearanceDuration = 0.1f;

    // Сделаем размеры явными, чтобы они не зависели от родительских скейлов
    private Vector3 targetScale = new Vector3(1f, 1f, 1f);
    private bool isActive = false;

    private const float MinX = -5.32f;
    private const float MaxX = 1.44f;
    private const float MinY = -2.02f;
    private const float MaxY = 2.3f;

    void Awake()
    {
        // Принудительно ставим лазер в ноль на старте сцены
        transform.localScale = Vector3.zero;
    }

    // Классическая анимация включения: сначала вдоль оси (X), затем перпендикулярно (Y, Z)
    public void ActivateBeam()
    {
        isActive = true;
        gameObject.SetActive(true);

        // Обязательно сбрасываем предыдущие анимации, чтобы они не конфликтовали
        transform.DOKill();

        // Начинаем строго из нулевого масштаба
        transform.localScale = Vector3.zero;

        // ШАГ 1: Вытягиваем по длине (ось X) до единицы
        transform.DOScaleX(targetScale.x, appearanceDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                // ШАГ 2: Расширяем по толщине (оси Y и Z) до единицы
                transform.DOScaleY(targetScale.y, appearanceDuration).SetEase(Ease.OutQuad);
                transform.DOScaleZ(targetScale.z, appearanceDuration).SetEase(Ease.OutQuad);
            });
    }

    // Классическая анимация выключения: сначала сужается по толщине, затем сжимается по длине
    public void DeactivateBeam()
    {
        if (!isActive) return;
        isActive = false;

        transform.DOKill();

        // ШАГ 1: Схлопываем по толщине (Y и Z) в ноль
        transform.DOScaleY(0f, disappearanceDuration).SetEase(Ease.InQuad);
        transform.DOScaleZ(0f, disappearanceDuration).SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                // ШАГ 2: Сжимаем по длине (X) в ноль
                transform.DOScaleX(0f, disappearanceDuration)
                    .SetEase(Ease.InQuad)
                    .OnComplete(() => gameObject.SetActive(false));
            });
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!isActive) return;

        // 1. ПРОВЕРКА НА БОССА
        if (collision.TryGetComponent<FinalBoss>(out var boss))
        {
            if (!boss.IsInvulnerable)
            {
                // Наносим 1 единицу урона в секунду
                boss.TakeLaserDamage(Time.deltaTime);
                SoundManager.Instance.PlaySFX(SoundManager.SoundEffect.BossDamage);
            }
            return; // Выходим в любом случае, если это босс (независимо от щита)
        }

        // 2. ОБЫЧНЫЕ ВРАГИ (Вынесены за пределы проверки босса)
        if (collision.CompareTag("Enemy") || collision.gameObject.layer == LayerMask.NameToLayer("Enemy") ||
            collision.GetComponent<PhantomEnemy>() != null)
        {
            Vector3 enemyPos = collision.transform.position;

            bool isXOnScreen = enemyPos.x >= (MinX - 0.2f) && enemyPos.x <= (MaxX + 0.2f);
            bool isYOnScreen = enemyPos.y >= (MinY - 0.2f) && enemyPos.y <= (MaxY + 0.2f);

            if (isXOnScreen && isYOnScreen)
            {
                if (GameManager.Instance != null) GameManager.Instance.RegisterEnemyKilled();
                Destroy(collision.gameObject);
            }
        }
    }
}
