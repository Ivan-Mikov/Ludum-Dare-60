using UnityEngine;
using UnityEngine.EventSystems; // Обязательно для отслеживания мыши
using DG.Tweening;             // Используем ваш DOTween

public class MenuButtonEffects : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Настройки анимации")]
    [SerializeField] private float hoverScale = 1.15f;    // На сколько увеличится кнопка (1.15 = +15%)
    [SerializeField] private float animationDuration = 0.2f; // Время анимации в секундах

    private Vector3 originalScale;

    void Awake()
    {
        // Запоминаем стартовый размер кнопки
        originalScale = transform.localScale;
    }

    // Срабатывает автоматически, когда мышка заходит на область кнопки
    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.DOKill(); // Сбрасываем прошлую анимацию, чтобы не было багов при спаме мыши
        transform.DOScale(originalScale * hoverScale, animationDuration).SetEase(Ease.OutBack);
    }

    // Срабатывает автоматически, когда мышка уходит с кнопки
    public void OnPointerExit(PointerEventData eventData)
    {
        transform.DOKill();
        transform.DOScale(originalScale, animationDuration).SetEase(Ease.OutQuad);
    }

    void OnDisable()
    {
        // На всякий случай сбрасываем размер в исходный, если кнопка скрывается
        transform.DOKill();
        transform.localScale = originalScale;
    }
}