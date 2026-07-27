using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DragCompleteChecker : MonoBehaviour
{
    [Header("Drag Objects")]
    public List<UIDragDrop> dragItems = new List<UIDragDrop>();

    [Header("Events")]
    public UnityEvent onAllCompleted;

    private bool eventInvoked = false;

    void Update()
    {
        if (eventInvoked)
            return;

        foreach (UIDragDrop item in dragItems)
        {
            if (!item.IsPlaced())
                return;
        }

        eventInvoked = true;
        onAllCompleted?.Invoke();
    }
}