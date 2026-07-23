using System;
using System.Collections.Generic;
using UnityEngine;

public class PageContentManager : MonoBehaviour
{
    [System.Serializable]
    public class PageContentConfig
    {
        [Header("Page Assignment")]
        [Tooltip("The human-readable Page Number (1, 2, 3, etc.). Page 1 corresponds to index 0.")]
        [Min(1)]
        public int pageNumber = 1;

        [Header("Target Components")]
        [Tooltip("Colliders to control for this page.")]
        public List<Collider> colliders = new List<Collider>();

        [Tooltip("Scripts/MonoBehaviours to control for this page.")]
        public List<MonoBehaviour> scripts = new List<MonoBehaviour>();

        [Header("Activation Rules")]
        [Tooltip("If TRUE, components will ONLY be enabled while on this exact page number.")]
        public bool onlyActiveOnThisPage = false;

        [Tooltip("If TRUE, components turn ON when reaching this page number and STAY ON for all higher/upcoming page numbers.")]
        public bool keepActiveInUpcomingPages = false;

        /// <summary>
        /// Converts 1-based Page Number to 0-based Page Index.
        /// </summary>
        public int TargetPageIndex => Mathf.Max(0, pageNumber - 1);
    }

    [Header("Page Configurations")]
    [SerializeField] private List<PageContentConfig> pageConfigs = new List<PageContentConfig>();

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
        // Apply state for the initial starting page
        HandlePageChanged(PageNavigationController.CurrentIndex);
    }

    private void HandlePageChanged(int currentPageIndex)
    {
        foreach (PageContentConfig config in pageConfigs)
        {
            bool shouldBeActive = DetermineActiveState(currentPageIndex, config);

            // Toggle Colliders
            foreach (var col in config.colliders)
            {
                if (col != null)
                {
                    col.enabled = shouldBeActive;
                }
            }

            // Toggle Scripts/MonoBehaviours
            foreach (var script in config.scripts)
            {
                if (script != null)
                {
                    script.enabled = shouldBeActive;
                }
            }
        }
    }

    private bool DetermineActiveState(int currentPageIndex, PageContentConfig config)
    {
        int targetIndex = config.TargetPageIndex;

        // 1. Current page matches the target page index
        if (currentPageIndex == targetIndex)
        {
            return true;
        }

        // 2. "Only Active On This Page" rule: disable if not on target page
        if (config.onlyActiveOnThisPage)
        {
            return false;
        }

        // 3. "Keep Active In Upcoming Pages" rule: stay on if current page index is higher
        if (config.keepActiveInUpcomingPages && currentPageIndex > targetIndex)
        {
            return true;
        }

        // Default: turn off if on a different page or haven't reached target page
        return false;
    }
}