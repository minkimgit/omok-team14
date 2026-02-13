using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ButtonInteraction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler,
    IPointerUpHandler
{
    private RectTransform _rectTransform;
    private AudioManager _audioManager;

    void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
        _audioManager = FindFirstObjectByType<AudioManager>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _rectTransform.DOScale(1.1f, .15f).SetEase(Ease.InOutQuad);
        _audioManager.PlayHoverButtonSfx();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _rectTransform.DOScale(1f, .15f).SetEase(Ease.InOutQuad);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _rectTransform.DOScale(.8f, .15f).SetEase(Ease.OutBack);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _rectTransform.DOScale(1f, .15f).SetEase(Ease.InOutBack);
        _audioManager.PlayButtonClickSfx();
    }
}