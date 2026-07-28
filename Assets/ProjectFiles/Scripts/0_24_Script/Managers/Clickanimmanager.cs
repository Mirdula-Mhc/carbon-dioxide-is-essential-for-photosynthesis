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

        [Tooltip("Check this if 'animation' also moves/rotates the camera (e.g. its Animator/PlayableDirector has a track on the camera transform). While this entry's animation plays, CameraMover is told to hand off control so it doesn't fight the click-anim or snap the camera back afterward.")]
        public bool drivesCamera = false;
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

    [Header("Camera Coordination (optional)")]
    [Tooltip("Assign if any click-anim entries have drivesCamera checked. Told to stand down while those play, and handed back control once they finish.")]
    public CameraMover cameraMover;

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
            Debug.Log($"[ClickAnimManager] Raycast hit '{hit.collider.name}', ClickAnimObject found: {obj != null}, isUIObject: {obj?.isUIObject}");
            if (obj == null || obj.isUIObject) return;

            var entry = FindEntry(currentPageIndex, obj);
            Debug.Log($"[ClickAnimManager] currentPageIndex={currentPageIndex}, entry found for this object: {entry != null}");
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

                // UI clicks go straight from Button.onClick -> OnClickedUI()
                // -> TriggerClick(), bypassing OnObjectClicked() below (that
                // path is 3D-raycast only). ClickAnimObject exposes
                // pendingDrivesCamera/pendingCameraMover so OnClickedUI()
                // can call BeginExternalControl() itself at the actual
                // moment of the click, not here at page-entry time (the
                // user may sit on this page for a while before clicking).
                entry.clickObject.pendingDrivesCamera = entry.drivesCamera;
                entry.clickObject.pendingCameraMover = entry.drivesCamera ? cameraMover : null;

                // Same reasoning as the 3D path: run the wait-for-
                // completion coroutine on this manager (never gets
                // deactivated) rather than on the clicked object
                // itself (which some Timelines deactivate mid-play
                // via an Activation Track, silently killing the
                // coroutine and leaving the page permanently locked).
                entry.clickObject.pendingCoroutineHost = this;
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
        // Lock BOTH Next and Previous for this animation.
        PageFlowManager.Instance?.LockInteraction();

        if (entry.drivesCamera && cameraMover != null)
            cameraMover.BeginExternalControl();

        entry.clickObject.TriggerClick(
            entry.animation,
            () => OnObjectFinished(pageIndex, entry.clickObject),
            this
        );
    }

    void OnObjectFinished(int pageIndex, ClickAnimObject obj)
    {
        // This specific click animation has finished.
        PageFlowManager.Instance?.UnlockInteraction();

        Debug.Log(
            $"[ClickAnimManager] OnObjectFinished called for page {pageIndex}, " +
            $"object '{obj.name}'"
        );
        Debug.Log($"[ClickAnimManager] OnObjectFinished called for page {pageIndex}, object '{obj.name}'");

        // 3D path only: OnObjectClicked() below called BeginExternalControl()
        // directly (it has the ObjectEntry in hand), so end it here to match.
        // UI path handles its own Begin/End inside ClickAnimObject.OnClickedUI(),
        // so this would double-call End for UI objects if not guarded - but
        // EndExternalControl() is idempotent (just sets a bool), so it's safe
        // either way.
        var set = pageObjectSets.Find(s => s.pageIndex == pageIndex);
        var finishedEntry = set?.entries.Find(e => e.clickObject == obj);
        if (finishedEntry != null && finishedEntry.drivesCamera && cameraMover != null && !obj.isUIObject)
            cameraMover.EndExternalControl();

        var state = pageStates[pageIndex];
        if (state.locked) return;
        if (state.finished.Contains(obj)) return;

        state.finished.Add(obj);

        Debug.Log($"[ClickAnimManager] Page {pageIndex}: {state.finished.Count}/{set.entries.Count} finished");
        if (state.finished.Count >= set.entries.Count)
        {
            state.locked = true;
            Debug.Log($"[ClickAnimManager] Page {pageIndex} complete - calling OnClickAnimDone()");
            PageFlowManager.Instance.OnClickAnimDone();
        }
    }
}