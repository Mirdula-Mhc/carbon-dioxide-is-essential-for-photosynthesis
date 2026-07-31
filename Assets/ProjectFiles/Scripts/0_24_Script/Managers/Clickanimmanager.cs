using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class ClickAnimManager : MonoBehaviour
{
    [System.Serializable]
    public class ObjectEntry
    {
        public ClickAnimObject clickObject;
        public AnimationSource animation;

        [Tooltip(
            "Enable when this animation moves/rotates the camera."
        )]
        public bool drivesCamera = false;
    }

    [System.Serializable]
    public class PageObjectSet
    {
        public int pageIndex;
        public List<ObjectEntry> entries =
            new List<ObjectEntry>();
    }

    [Header("Per-Page Click-Anim Sets")]
    public List<PageObjectSet> pageObjectSets =
        new List<PageObjectSet>();

    [Header("3D Click Detection")]
    public Camera raycastCamera;
    public LayerMask clickableLayers = ~0;

    [Header("Camera Coordination")]
    public CameraMover cameraMover;

    private class PageState
    {
        public bool completed;

        public HashSet<ClickAnimObject> finished =
            new HashSet<ClickAnimObject>();
    }

    private readonly Dictionary<int, PageState> pageStates =
        new Dictionary<int, PageState>();

    private int currentPageIndex = -1;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (raycastCamera == null)
            raycastCamera = Camera.main;

        if (pageObjectSets == null)
            return;

        foreach (PageObjectSet set in pageObjectSets)
        {
            if (set == null)
                continue;

            if (!pageStates.ContainsKey(set.pageIndex))
            {
                pageStates.Add(
                    set.pageIndex,
                    new PageState()
                );
            }
        }
    }

    // =========================================================
    // 3D CLICK
    // =========================================================

    private void Update()
    {
        if (Pointer.current == null)
            return;

        if (!Pointer.current.press.wasPressedThisFrame)
            return;

        if (currentPageIndex < 0)
            return;

        if (!pageStates.TryGetValue(
            currentPageIndex,
            out PageState state))
        {
            return;
        }

        if (state.completed)
            return;

        if (raycastCamera == null)
            raycastCamera = Camera.main;

        if (raycastCamera == null)
            return;

        Ray ray = raycastCamera.ScreenPointToRay(
            Pointer.current.position.ReadValue()
        );

        if (!Physics.Raycast(
            ray,
            out RaycastHit hit,
            1000f,
            clickableLayers))
        {
            return;
        }

        ClickAnimObject obj =
            hit.collider.GetComponentInParent<ClickAnimObject>();

        if (obj == null || obj.isUIObject)
            return;

        ObjectEntry entry =
            FindEntry(currentPageIndex, obj);

        if (entry == null)
            return;

        OnObjectClicked(
            currentPageIndex,
            entry
        );
    }

    // =========================================================
    // PAGE CONTEXT
    // =========================================================

    public void SetPageContext(int pageIndex)
    {
        currentPageIndex = pageIndex;

        if (!pageStates.TryGetValue(
            pageIndex,
            out PageState state))
        {
            return;
        }

        PageObjectSet set =
            GetSet(pageIndex);

        if (set == null || set.entries == null)
            return;

        foreach (ObjectEntry entry in set.entries)
        {
            if (entry == null ||
                entry.clickObject == null)
            {
                continue;
            }

            ClickAnimObject obj = entry.clickObject;

            if (state.finished.Contains(obj))
                continue;

            obj.Highlight();

            if (!obj.isUIObject)
                continue;

            // Capture values locally.
            int capturedPage = pageIndex;
            ClickAnimObject capturedObject = obj;
            bool capturedDrivesCamera = entry.drivesCamera;

            obj.pendingSource = entry.animation;

            obj.pendingOnComplete = () =>
            {
                // UI ClickAnimObject itself handles
                // EndExternalControl when drivesCamera=true.
                OnObjectFinished(
                    capturedPage,
                    capturedObject
                );
            };

            obj.pendingDrivesCamera =
                capturedDrivesCamera;

            obj.pendingCameraMover =
                capturedDrivesCamera
                    ? cameraMover
                    : null;

            // Keep completion coroutine alive even if
            // Timeline deactivates the clicked object.
            obj.pendingCoroutineHost = this;
        }
    }

    public bool OwnsPage(int pageIndex)
    {
        return pageStates.ContainsKey(pageIndex);
    }

    // =========================================================
    // 3D CLICK EXECUTION
    // =========================================================

    private void OnObjectClicked(
        int pageIndex,
        ObjectEntry entry)
    {
        if (entry == null ||
            entry.clickObject == null)
        {
            return;
        }

        PageState state = pageStates[pageIndex];

        if (state.completed)
            return;

        if (state.finished.Contains(entry.clickObject))
            return;

        string lockID = GetLockID(
            pageIndex,
            entry.clickObject
        );

        PageFlowManager.Instance?.LockInteraction(lockID);

        if (entry.drivesCamera &&
            cameraMover != null)
        {
            cameraMover.BeginExternalControl();
        }

        ClickAnimObject obj = entry.clickObject;
        bool drivesCamera = entry.drivesCamera;

        obj.TriggerClick(
            entry.animation,
            () =>
            {
                // ALWAYS return camera ownership first.
                if (drivesCamera &&
                    cameraMover != null)
                {
                    cameraMover.EndExternalControl();
                }

                // THEN unlock page navigation.
                PageFlowManager.Instance?.UnlockInteraction(lockID);

                // THEN record interaction completion.
                OnObjectFinished(
                    pageIndex,
                    obj
                );
            },
            this
        );
    }

    // =========================================================
    // COMPLETION
    // =========================================================

    private void OnObjectFinished(
        int pageIndex,
        ClickAnimObject obj)
    {
        if (!pageStates.TryGetValue(
            pageIndex,
            out PageState state))
        {
            return;
        }

        if (state.completed)
            return;

        if (state.finished.Contains(obj))
            return;

        state.finished.Add(obj);

        PageObjectSet set =
            GetSet(pageIndex);

        if (set == null)
            return;

        int requiredCount = 0;

        foreach (ObjectEntry entry in set.entries)
        {
            if (entry != null &&
                entry.clickObject != null)
            {
                requiredCount++;
            }
        }

        Debug.Log(
            $"[ClickAnimManager] Page {pageIndex}: " +
            $"{state.finished.Count}/{requiredCount} complete."
        );

        if (requiredCount == 0 ||
            state.finished.Count >= requiredCount)
        {
            state.completed = true;

            Debug.Log(
                $"[ClickAnimManager] Page {pageIndex} complete."
            );

            PageFlowManager.Instance?.OnClickAnimDone();
        }
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private PageObjectSet GetSet(int pageIndex)
    {
        return pageObjectSets?.Find(
            set => set != null &&
                   set.pageIndex == pageIndex
        );
    }

    private ObjectEntry FindEntry(
        int pageIndex,
        ClickAnimObject obj)
    {
        PageObjectSet set = GetSet(pageIndex);

        if (set == null || set.entries == null)
            return null;

        return set.entries.Find(
            entry =>
                entry != null &&
                entry.clickObject == obj
        );
    }

    private string GetLockID(
        int pageIndex,
        ClickAnimObject obj)
    {
        return
            $"ClickAnim_{pageIndex}_{obj.GetInstanceID()}";
    }
}