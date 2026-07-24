using UnityEngine;
using System.Collections.Generic;

// -----------------------------------------------------------------
// Controls which objects are visible on which page. Purely a
// visibility switch - NOT a completion gate, so unlike
// ButtonGroupManager/ClickAnimManager it never calls back into
// PageFlowManager and PageFlowManager never needs a reference to
// this at all. It only needs to be told when the page changes.
//
// Rule: an object assigned to page N is active ONLY while page N is
// current, and hidden on every other page. Objects NOT listed here
// are never touched - this script only affects objects you've
// explicitly assigned to a page.
//
// Setup:
//   1. One PageVisibilityManager in the scene.
//   2. In "pageObjectSets", add one entry per page that has objects
//      which should only be visible on that page, and drag those
//      objects into its list.
//   3. Wire this into PageFlowManager.ShowPage() by calling
//      SetPageContext(index) the same way buttonGroupManager and
//      clickAnimManager are called (see PageFlowManager's
//      "Interaction Managers" section - add a field there for this).
//   The same object can be listed under more than one page if it
//   should be visible on several (e.g. pages 3 AND 5) - it'll be
//   shown whenever the current page matches any entry it's in.
// -----------------------------------------------------------------
public class PageVisibilityManager : MonoBehaviour
{
    [System.Serializable]
    public class PageObjectSet
    {
        public int pageIndex;
        public List<GameObject> objects;
    }

    [Header("Per-Page Visible Objects")]
    [Tooltip("One entry per page. Objects listed here are shown ONLY while that page is active, hidden otherwise.")]
    public List<PageObjectSet> pageObjectSets;

    void Start()
    {
        // Hide everything managed by this script up front, so nothing
        // assigned to a later page is visible before its page is reached.
        foreach (var set in pageObjectSets)
            foreach (var obj in set.objects)
                if (obj != null) obj.SetActive(false);
    }

    // Called by PageFlowManager.ShowPage() every time the active
    // page changes.
    public void SetPageContext(int pageIndex)
    {
        foreach (var set in pageObjectSets)
        {
            bool shouldBeActive = set.pageIndex == pageIndex;
            foreach (var obj in set.objects)
                if (obj != null) obj.SetActive(shouldBeActive);
        }
    }
}