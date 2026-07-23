using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PageEventManager : MonoBehaviour
{
    [System.Serializable]
    public class PageEventConfig
    {
        [Header("Page Assignment")]
        [Tooltip("The human-readable Page Number (1, 2, 3, etc.). Page 1 corresponds to index 0.")]
        [Min(1)]
        public int pageNumber = 1;

        [Header("Activation Rules")]
        [Tooltip("If TRUE: Fires ONLY ONCE the first time you visit this page. If FALSE: Fires every time you land on this page.")]
        public bool triggerOnlyOnceOnThisPage = true;

        [Tooltip("If TRUE: Keeps firing when you move forward to higher/upcoming page numbers.")]
        public bool keepTriggeringInUpcomingPages = false;

        [Header("Event")]
        public UnityEvent onPageReached;

        // Tracks whether the event has already been executed
        [HideInInspector] public bool hasTriggered = false;

        /// <summary>
        /// Converts 1-based Page Number to 0-based Page Index.
        /// </summary>
        public int TargetPageIndex => Mathf.Max(0, pageNumber - 1);
    }

    [Header("Page Event Configurations")]
    [SerializeField] private List<PageEventConfig> pageEventConfigs = new List<PageEventConfig>();

    private void OnEnable()
    {
        PageNavigationController.OnPageChanged += HandlePageChanged;
    }

    private void OnDisable()
    {
        PageNavigationController.OnPageChanged -= HandlePageChanged;
    }

    private void Start()
    {
        // Fire event for the starting page
        HandlePageChanged(PageNavigationController.CurrentIndex);
    }

    private void HandlePageChanged(int currentPageIndex)
    {
        foreach (PageEventConfig config in pageEventConfigs)
        {
            if (ShouldTriggerEvent(currentPageIndex, config))
            {
                config.hasTriggered = true;
                config.onPageReached?.Invoke();
            }
        }
    }

    private bool ShouldTriggerEvent(int currentPageIndex, PageEventConfig config)
    {
        int targetIndex = config.TargetPageIndex;

        bool isExactPage = (currentPageIndex == targetIndex);
        bool isUpcomingPage = (currentPageIndex > targetIndex);

        // --- RULE 1: Exact Page Match ---
        if (isExactPage)
        {
            // If "Trigger Only Once" is checked, ensure it hasn't fired yet
            if (config.triggerOnlyOnceOnThisPage)
            {
                return !config.hasTriggered;
            }

            // Otherwise, fire every time you reach this page
            return true;
        }

        // --- RULE 2: Upcoming Pages Match ---
        if (isUpcomingPage && config.keepTriggeringInUpcomingPages)
        {
            if (config.triggerOnlyOnceOnThisPage)
            {
                return !config.hasTriggered;
            }

            return true;
        }

        return false;
    }
}