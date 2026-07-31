using UnityEngine;
using System.Collections.Generic;

public class PageVisibilityManager : MonoBehaviour
{
    [System.Serializable]
    public class PageObjectSet
    {
        public int pageIndex;
        public List<GameObject> objects;
    }

    // =========================================================
    // VISIBLE ON PAGE
    // Objects assigned here are active ONLY on their assigned
    // page(s) and inactive on every other page.
    // =========================================================

    [Header("Per-Page Visible Objects")]
    [Tooltip("Objects listed here are shown ONLY while their assigned page is active.")]
    public List<PageObjectSet> pageObjectSets;


    // =========================================================
    // DISABLED ON PAGE
    // Objects assigned here are forced inactive when that page
    // becomes current.
    //
    // Unlike pageObjectSets, these objects are NOT automatically
    // enabled on other pages. This list only forces SetActive(false)
    // on the specified page.
    // =========================================================

    [Header("Per-Page Disabled Objects")]
    [Tooltip("Objects listed here are forced inactive when their assigned page is active.")]
    public List<PageObjectSet> pageDisabledObjectSets;


    void Awake()
    {
        if (pageObjectSets != null)
        {
            foreach (var set in pageObjectSets)
            {
                if (set?.objects == null)
                    continue;

                foreach (var obj in set.objects)
                {
                    if (obj != null)
                        obj.SetActive(false);
                }
            }
        }
    }


    // Called by PageFlowManager.ShowPage(index)
    public void SetPageContext(int pageIndex)
    {
        Debug.Log("PageVisibilityManager -> Page " + pageIndex);

        // =====================================================
        // 1. NORMAL VISIBILITY
        // =====================================================

        Dictionary<GameObject, bool> objectStates = new();

        if (pageObjectSets != null)
        {
            foreach (var set in pageObjectSets)
            {
                if (set?.objects == null)
                    continue;

                bool activeForThisPage = set.pageIndex == pageIndex;

                foreach (var obj in set.objects)
                {
                    if (obj == null)
                        continue;

                    if (!objectStates.ContainsKey(obj))
                        objectStates[obj] = false;

                    if (activeForThisPage)
                        objectStates[obj] = true;
                }
            }
        }

        foreach (var pair in objectStates)
        {
            pair.Key.SetActive(pair.Value);
        }


        // =====================================================
        // 2. FORCE DISABLE
        // Run AFTER normal visibility so Disable always wins.
        // =====================================================

        if (pageDisabledObjectSets != null)
        {
            foreach (var set in pageDisabledObjectSets)
            {
                if (set == null || set.pageIndex != pageIndex || set.objects == null)
                    continue;

                foreach (var obj in set.objects)
                {
                    if (obj == null)
                        continue;

                    obj.SetActive(false);

                    Debug.Log(
                        $"[PageVisibilityManager] Forced OFF '{obj.name}' " +
                        $"on page {pageIndex}"
                    );
                }
            }
        }
    }
}