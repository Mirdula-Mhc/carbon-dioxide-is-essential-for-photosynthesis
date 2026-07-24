using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

// -----------------------------------------------------------------
// A MANAGER, same relationship to PageFlowManager as
// ButtonGroupManager. Owns every click-anim page's objects AND,
// critically, the SPECIFIC animation each object should play on
// THAT page - so the same physical object can appear on multiple
// pages with a different animation each time.
//
// Setup:
//   1. One ClickAnimManager in the scene.
//   2. In "pageObjectSets", add one entry per page. Each page has a
//      list of ObjectEntry - one per clickable object on that page,
//      each with its OWN AnimationSource (Animator+trigger/state, or
//      a PlayableDirector). The same ClickAnimObject can be dragged
//      into entries on more than one page, each with different
//      animation data - that's the whole point.
//   3. Drag this ClickAnimManager into PageFlowManager's
//      "Click Anim Manager" field.
//   4. UI objects: wire their Button's onClick to that
//      ClickAnimObject's OnClickedUI() (once - it reads whichever
//      page is currently active via pendingSource, set by
//      SetPageContext below).
//   5. 3D objects: no extra wiring - raycast click detection below
//      calls TriggerClick() directly with the correct source for
//      the active page.
// -----------------------------------------------------------------
public class ClickAnimManager : MonoBehaviour
{
    [System.Serializable]
    public class ObjectEntry
    {
        public ClickAnimObject clickObject;
        public AnimationSource animation;
    }

    [System.Serializable]
    public class PageObjectSet
    {
        public int pageIndex;
        public List<ObjectEntry> entries;
    }

    [Header("Per-Page Click-Anim Sets")]
    public List<PageObjectSet> pageObjectSets;

    [Header("3D Click Detection")]
    [Tooltip("Camera used to raycast for 3D object clicks. Leave empty to use Camera.main.")]
    public Camera raycastCamera;
    public LayerMask clickableLayers = ~0;

    class PageState
    {
        public bool locked;
        public HashSet<ClickAnimObject> finished = new HashSet<ClickAnimObject>();
    }

    Dictionary<int, PageState> pageStates = new();
    int currentPageIndex = -1;

    void Start()
    {
        if (raycastCamera == null) raycastCamera = Camera.main;

        foreach (var set in pageObjectSets)
            pageStates[set.pageIndex] = new PageState();
        // NOTE: unlike ButtonGroupManager, we do NOT subscribe to any
        // per-object event here. Completion is driven directly by the
        // callback passed into TriggerClick() per click, per page -
        // see OnObjectClicked() below. This is what avoids the
        // duplicate-subscription bug when the same object appears on
        // more than one page.
    }

    void Update()
    {
        if (Pointer.current == null) return;
        if (!Pointer.current.press.wasPressedThisFrame) return;
        if (currentPageIndex < 0 || !pageStates.TryGetValue(currentPageIndex, out var state) || state.locked) return;

        Ray ray = raycastCamera.ScreenPointToRay(Pointer.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, clickableLayers))
        {
            var obj = hit.collider.GetComponentInParent<ClickAnimObject>();
            if (obj == null || obj.isUIObject) return;

            var entry = FindEntry(currentPageIndex, obj);
            if (entry != null)
                OnObjectClicked(currentPageIndex, entry);
        }
    }

    // Called by PageFlowManager.ShowPage() every time the active
    // page changes.
    public void SetPageContext(int pageIndex)
    {
        currentPageIndex = pageIndex;

        if (!pageStates.TryGetValue(pageIndex, out var state)) return; // not a click-anim page

        var set = pageObjectSets.Find(s => s.pageIndex == pageIndex);
        if (set == null) return;

        foreach (var entry in set.entries)
        {
            if (state.finished.Contains(entry.clickObject)) continue;

            entry.clickObject.Highlight();

            // For UI objects: point their pendingSource/pendingOnComplete
            // at THIS page's data, so their already-wired onClick uses
            // the right animation and reports back to the right page.
            // Re-set every time the page becomes active, since the same
            // object needs this to change if revisited on a different page.
            if (entry.clickObject.isUIObject)
            {
                entry.clickObject.pendingSource = entry.animation;
                entry.clickObject.pendingOnComplete = () => OnObjectFinished(pageIndex, entry.clickObject);
            }
        }
    }

    // Lets PageFlowManager check "does this page belong to me".
    public bool OwnsPage(int pageIndex)
    {
        return pageStates.ContainsKey(pageIndex);
    }

    ObjectEntry FindEntry(int pageIndex, ClickAnimObject obj)
    {
        var set = pageObjectSets.Find(s => s.pageIndex == pageIndex);
        return set?.entries.Find(e => e.clickObject == obj);
    }

    // Shared by both UI (via OnClickedUI -> pendingSource, handled
    // inside ClickAnimObject itself) and 3D (called directly here).
    // For 3D we go through this method explicitly so the completion
    // callback captures the correct pageIndex/entry pairing.
    void OnObjectClicked(int pageIndex, ObjectEntry entry)
    {
        entry.clickObject.TriggerClick(entry.animation, () => OnObjectFinished(pageIndex, entry.clickObject));
    }

    void OnObjectFinished(int pageIndex, ClickAnimObject obj)
    {
        var state = pageStates[pageIndex];
        if (state.locked) return;
        if (state.finished.Contains(obj)) return;

        state.finished.Add(obj);

        var set = pageObjectSets.Find(s => s.pageIndex == pageIndex);
        if (state.finished.Count >= set.entries.Count)
        {
            state.locked = true;
            PageFlowManager.Instance.OnClickAnimDone();
        }
    }
}