using UnityEngine;
using System.Collections.Generic;

public class PageEnterAnimManager : MonoBehaviour
{
    [System.Serializable]
    public class PageAnimSet
    {
        public int pageIndex;

        public List<AnimationSource> animations =
            new List<AnimationSource>();

        [Tooltip(
            "If enabled, revisiting this page replays its animations."
        )]
        public bool replayOnRevisit = false;
    }

    [Header("Per-Page Auto-Play Animations")]
    public List<PageAnimSet> pageAnimSets =
        new List<PageAnimSet>();

    private class PageState
    {
        public bool playing;
        public bool completed;
        public int finishedCount;
        public int expectedCount;
    }

    private readonly Dictionary<int, PageState> pageStates =
        new Dictionary<int, PageState>();

    private int lastPageIndex = -1;

    // =========================================================
    // INITIALIZATION
    // =========================================================

    private void Awake()
    {
        BuildStates();
    }

    private void BuildStates()
    {
        pageStates.Clear();

        if (pageAnimSets == null)
            return;

        foreach (PageAnimSet set in pageAnimSets)
        {
            if (set == null)
                continue;

            if (!pageStates.ContainsKey(set.pageIndex))
                pageStates.Add(set.pageIndex, new PageState());
        }
    }

    // =========================================================
    // PAGE CONTEXT
    // =========================================================

    public void SetPageContext(int pageIndex)
    {
        bool actualPageChange =
            pageIndex != lastPageIndex;

        lastPageIndex = pageIndex;

        if (!pageStates.TryGetValue(
            pageIndex,
            out PageState state))
        {
            return;
        }

        if (!actualPageChange)
            return;

        PageAnimSet set =
            pageAnimSets.Find(x => x.pageIndex == pageIndex);

        if (set == null)
            return;

        int validAnimationCount = CountValidAnimations(set);

        // Nothing configured = automatically complete.
        if (validAnimationCount == 0)
        {
            state.completed = true;
            state.playing = false;
            state.finishedCount = 0;
            state.expectedCount = 0;

            return;
        }

        // Already completed and shouldn't replay.
        if (state.completed && !set.replayOnRevisit)
            return;

        StartPageAnimations(
            pageIndex,
            set,
            state,
            validAnimationCount
        );
    }

    // =========================================================
    // START
    // =========================================================

    private void StartPageAnimations(
        int pageIndex,
        PageAnimSet set,
        PageState state,
        int validAnimationCount)
    {
        state.playing = true;
        state.completed = false;
        state.finishedCount = 0;
        state.expectedCount = validAnimationCount;

        string lockID = GetLockID(pageIndex);

        // THIS is the missing part:
        // lock BOTH navigation buttons while page-enter animation runs.
        PageFlowManager.Instance?.LockInteraction(lockID);

        foreach (AnimationSource animation in set.animations)
        {
            if (!IsValid(animation))
                continue;

            AnimationSource capturedAnimation = animation;

            StartCoroutine(
                capturedAnimation.Play(
                    this,
                    () => OnOneFinished(pageIndex)
                )
            );
        }
    }

    // =========================================================
    // COMPLETION
    // =========================================================

    private void OnOneFinished(int pageIndex)
    {
        if (!pageStates.TryGetValue(
            pageIndex,
            out PageState state))
        {
            return;
        }

        if (!state.playing)
            return;

        state.finishedCount++;

        Debug.Log(
            $"[PageEnterAnimManager] Page {pageIndex}: " +
            $"{state.finishedCount}/{state.expectedCount} finished."
        );

        if (state.finishedCount < state.expectedCount)
            return;

        state.playing = false;
        state.completed = true;

        string lockID = GetLockID(pageIndex);

        // Release Previous + Next.
        PageFlowManager.Instance?.UnlockInteraction(lockID);

        // Tell PageFlowManager this gate is complete.
        PageFlowManager.Instance?.OnPageEnterAnimDone();

        Debug.Log(
            $"[PageEnterAnimManager] Page {pageIndex} complete."
        );
    }

    // =========================================================
    // PAGE FLOW QUERIES
    // =========================================================

    public bool OwnsPage(int pageIndex)
    {
        return pageStates.ContainsKey(pageIndex);
    }

    public bool IsPageDone(int pageIndex)
    {
        if (!pageStates.TryGetValue(
            pageIndex,
            out PageState state))
        {
            return true;
        }

        PageAnimSet set =
            pageAnimSets.Find(x => x.pageIndex == pageIndex);

        if (set == null)
            return true;

        if (CountValidAnimations(set) == 0)
            return true;

        return state.completed;
    }

    public bool IsPagePlaying(int pageIndex)
    {
        if (!pageStates.TryGetValue(
            pageIndex,
            out PageState state))
        {
            return false;
        }

        return state.playing;
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private int CountValidAnimations(PageAnimSet set)
    {
        if (set == null || set.animations == null)
            return 0;

        int count = 0;

        foreach (AnimationSource animation in set.animations)
        {
            if (IsValid(animation))
                count++;
        }

        return count;
    }

    private bool IsValid(AnimationSource source)
    {
        if (source == null)
            return false;

        if (source.director != null)
            return true;

        return source.animator != null &&
               source.clip != null;
    }

    private string GetLockID(int pageIndex)
    {
        return $"PageEnterAnim_{pageIndex}";
    }
}