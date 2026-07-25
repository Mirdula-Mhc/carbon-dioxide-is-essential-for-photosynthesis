using UnityEngine;
using System.Collections.Generic;

// -----------------------------------------------------------------
// A MANAGER, same relationship to PageFlowManager as
// ButtonGroupManager/ClickAnimManager: owns every page that needs an
// animation to auto-play on entry, tracks per-page completion, and
// calls back PageFlowManager.Instance.OnPageEnterAnimDone() once
// every animation on the current page has finished.
//
// Unlike ClickAnimManager, there's no click detection or highlight -
// the animation(s) start immediately when SetPageContext() is called
// for that page.
//
// Setup:
//   1. One PageEnterAnimManager in the scene.
//   2. In "pageAnimSets", add one entry per page that should
//      auto-play something on enter. Each page can have MULTIPLE
//      AnimationSources if more than one thing should play at once -
//      Next unlocks once ALL of them finish.
//   3. Drag this into PageFlowManager's "Page Enter Anim Manager"
//      field.
//   Revisiting an already-completed page does NOT replay the
//   animation by default (see ReplayOnRevisit below) - it just shows
//   Next as already unlocked, same as ButtonGroupManager restoring a
//   solved page's state.
// -----------------------------------------------------------------
public class PageEnterAnimManager : MonoBehaviour
{
    [System.Serializable]
    public class PageAnimSet
    {
        public int pageIndex;
        public List<AnimationSource> animations;
        [Tooltip("If true, revisiting this page (e.g. via Previous then Next again) replays the animation(s) and re-locks Next until they finish again. If false, an already-completed page stays unlocked on revisit.")]
        public bool replayOnRevisit = false;
    }

    [Header("Per-Page Auto-Play Animations")]
    public List<PageAnimSet> pageAnimSets;

    class PageState
    {
        public bool locked;
        public int finishedCount;
    }

    Dictionary<int, PageState> pageStates = new();

    void Start()
    {
        foreach (var set in pageAnimSets)
            pageStates[set.pageIndex] = new PageState();
    }

    int lastPageIndex = -1;

    // Called by PageFlowManager.ShowPage() every time the active
    // page changes.
    public void SetPageContext(int pageIndex)
    {
        // ShowPage() gets called again internally right after this
        // page's own OnPageEnterAnimDone() fires (to refresh Next's
        // interactable state) - without this guard, that re-call would
        // immediately restart the animation when replayOnRevisit is
        // true, looping forever. Only run the start logic below on an
        // ACTUAL change of page.
        bool isActualPageChange = pageIndex != lastPageIndex;
        lastPageIndex = pageIndex;

        if (!pageStates.TryGetValue(pageIndex, out var state)) return; // not a page-enter-anim page
        if (!isActualPageChange) return;

        var set = pageAnimSets.Find(s => s.pageIndex == pageIndex);
        if (set == null || set.animations == null || set.animations.Count == 0) return;

        bool alreadyDone = state.finishedCount >= set.animations.Count;

        if (alreadyDone && !set.replayOnRevisit)
            return; // stay unlocked, don't replay

        // (Re)start this page's animations from scratch.
        state.locked = true;
        state.finishedCount = 0;

        foreach (var anim in set.animations)
        {
            if (anim == null) continue;
            StartCoroutine(anim.Play(this, () => OnOneFinished(pageIndex, set)));
        }
    }

    // Lets PageFlowManager check "does this page belong to me".
    public bool OwnsPage(int pageIndex)
    {
        return pageStates.ContainsKey(pageIndex);
    }

    // "Complete" for the purposes of PageFlowManager's gate check.
    public bool IsPageDone(int pageIndex)
    {
        if (!pageStates.TryGetValue(pageIndex, out var state)) return true; // not owned = not a gate
        var set = pageAnimSets.Find(s => s.pageIndex == pageIndex);
        if (set == null || set.animations == null || set.animations.Count == 0) return true;
        return state.finishedCount >= set.animations.Count;
    }

    void OnOneFinished(int pageIndex, PageAnimSet set)
    {
        var state = pageStates[pageIndex];
        state.finishedCount++;

        if (state.finishedCount >= set.animations.Count)
        {
            state.locked = false;
            PageFlowManager.Instance.OnPageEnterAnimDone();
        }
    }
}