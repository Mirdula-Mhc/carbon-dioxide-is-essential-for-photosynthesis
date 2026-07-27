using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

[RequireComponent(typeof(CanvasGroup))]
public class UIDragDrop : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Canvas")]
    public Canvas canvas;

    [Header("Correct Target")]
    public RectTransform target;

    [Header("Snap Settings")]
    public float snapDistance = 100f;
    public bool disableAfterDrop = true;

    [Header("All Drag Items")]
    public List<UIDragDrop> allItems = new List<UIDragDrop>();

    [Header("Events")]
    public UnityEvent onCorrectDrop;
    public UnityEvent onWrongDrop;
    public UnityEvent onAllPlaced;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private Vector2 startAnchoredPosition;
    private Transform originalParent;
    private int originalSiblingIndex;

    private bool isPlaced = false;

    public bool IsPlaced()
    {
        return isPlaced;
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isPlaced)
            return;

        startAnchoredPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();

        // Bring to front while dragging
        transform.SetParent(canvas.transform);
        transform.SetAsLastSibling();

        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isPlaced)
            return;

        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isPlaced)
            return;

        canvasGroup.blocksRaycasts = true;

        float distance = Vector2.Distance(rectTransform.position, target.position);

        if (distance <= snapDistance)
        {
            // Correct Drop
            transform.SetParent(target);

            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;

            isPlaced = true;

            onCorrectDrop?.Invoke();

            if (disableAfterDrop)
            {
                canvasGroup.blocksRaycasts = false;
                enabled = false;
            }

            bool completed = true;

            foreach (UIDragDrop item in allItems)
            {
                if (!item.isPlaced)
                {
                    completed = false;
                    break;
                }
            }

            if (completed)
                onAllPlaced?.Invoke();
        }
        else
        {
            // Wrong Drop - Return to Original Position
            transform.SetParent(originalParent);
            transform.SetSiblingIndex(originalSiblingIndex);

            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.anchoredPosition = startAnchoredPosition;

            onWrongDrop?.Invoke();
        }
    }
}