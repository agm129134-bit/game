using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class DraggableTrash : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public TrashMinigame manager;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 startPos;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        startPos = rectTransform.anchoredPosition; // 記住拖曳前的位置
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.8f;
        //manager.PlayClickSound();
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / manager.mainCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;
        // 告訴大總管我放開了，請檢查座標有沒有對準垃圾桶！
        manager.CheckDrop(this, rectTransform.anchoredPosition, startPos);
    }

    public void ResetPosition(Vector2 pos)
    {
        rectTransform.anchoredPosition = pos;
    }
}