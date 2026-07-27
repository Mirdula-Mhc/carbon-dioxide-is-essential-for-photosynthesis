using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EventTrigger : MonoBehaviour
{
    [System.Serializable]
    public class EventElement
    {
        [Header("Target Page Index (0-based)")]
        [Tooltip("The page index this element corresponds to.")]
        public int pageIndex = 0;

        [Header("Event To Trigger")]
        public UnityEvent onTriggered;

        [Header("Navigation")]
        [Tooltip("If enabled, navigation will be unlocked when this event is triggered.")]
        public bool enableNavigation = true;
    }

    [Header("Event Elements List")]
    [SerializeField] private List<EventElement> elements = new List<EventElement>();

    // =========================================================================
    // UNITY EVENT HELPER FUNCTIONS (Wire directly to Inspector Buttons/Events)
    // =========================================================================

    /// <summary>
    /// Triggers Element at Index 0 in the list.
    /// </summary>
    public void TriggerElement0() => TriggerElementIndex(0);

    /// <summary>
    /// Triggers Element at Index 1 in the list.
    /// </summary>
    public void TriggerElement1() => TriggerElementIndex(1);

    /// <summary>
    /// Triggers Element at Index 2 in the list.
    /// </summary>
    public void TriggerElement2() => TriggerElementIndex(2);

    // =========================================================================
    // CORE TRIGGER FUNCTIONS
    // =========================================================================

    /// <summary>
    /// Triggers element by its array list index (0, 1, 2...).
    /// </summary>
    public void TriggerElementIndex(int elementIndex)
    {
        if (elementIndex >= 0 && elementIndex < elements.Count)
        {
            ExecuteTrigger(elements[elementIndex]);
        }
        else
        {
            Debug.LogWarning($"[EventTrigger] Element Index {elementIndex} is out of bounds!", this);
        }
    }

    /// <summary>
    /// Triggers element matching a specific page index.
    /// </summary>
    public void TriggerElementByPageIndex(int pageIndex)
    {
        EventElement element = elements.Find(e => e.pageIndex == pageIndex);
        if (element != null)
        {
            ExecuteTrigger(element);
        }
        else
        {
            Debug.LogWarning($"[EventTrigger] No element found for Page Index {pageIndex}!", this);
        }
    }

    /// <summary>
    /// Triggers a specific element matching both page index and element list index.
    /// </summary>
    public void TriggerElementByPageIndex(int pageIndex, int elementIndex)
    {
        List<EventElement> pageMatches = elements.FindAll(e => e.pageIndex == pageIndex);
        if (elementIndex >= 0 && elementIndex < pageMatches.Count)
        {
            ExecuteTrigger(pageMatches[elementIndex]);
        }
    }

    private void ExecuteTrigger(EventElement element)
    {

        element.onTriggered?.Invoke();

        if (element.enableNavigation)
        {
            PageNavigationController.RequestNavigationUnlock();
        }
    }
}