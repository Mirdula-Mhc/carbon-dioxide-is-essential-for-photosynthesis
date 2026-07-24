using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AnimationSwitcher : MonoBehaviour
{
    [System.Serializable]
    public class AnimationElement
    {
        [Header("Page Index (0-based)")]
        [Tooltip("The page index this element corresponds to.")]
        public int pageIndex = 0;

        [Header("Animation Clips")]
        [Tooltip("List of animation clips assigned to this page/element.")]
        public AnimationClip[] animationClips;

        [Header("Navigation")]
        [Tooltip("If enabled, navigation will be unlocked when an animation finishes or starts.")]
        public bool unlockNavigationOnPlay = false;
    }

    [Header("Animator Reference")]
    public Animator animator;

    [Header("Animation Elements Config")]
    public List<AnimationElement> elements = new List<AnimationElement>();

    private int currentElementIndex = -1;
    private int currentClipIndex = -1;
    private Coroutine playRoutine;

    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        PageNavigationController.OnPageChanged += HandlePageChanged;
    }

    private void OnDisable()
    {
        PageNavigationController.OnPageChanged -= HandlePageChanged;
    }

    private void HandlePageChanged(int pageIndex)
    {
        // Stop active routine if page switches
        if (playRoutine != null)
            StopCoroutine(playRoutine);
    }

    // =========================================================================
    // UNITY EVENT HELPERS (Call directly from Inspector Events)
    // =========================================================================

    public void PlayElement0Clip0() => PlayClipInElement(0, 0);
    public void PlayElement0Clip1() => PlayClipInElement(0, 1);

    public void PlayElement1Clip0() => PlayClipInElement(1, 0);
    public void PlayElement1Clip1() => PlayClipInElement(1, 1);

    public void PlayElement2Clip0() => PlayClipInElement(2, 0);
    public void PlayElement2Clip1() => PlayClipInElement(2, 1);

    // =========================================================================
    // CORE ANIMATION PLAY LOGIC
    // =========================================================================

    /// <summary>
    /// Plays a specific clip index inside a specific element index.
    /// </summary>
    /// <param name="elementIndex">Index of the element in the list (0, 1, 2...)</param>
    /// <param name="clipIndex">Index of the clip inside that element's clips array (0, 1...)</param>
    public void PlayClipInElement(int elementIndex, int clipIndex)
    {
        if (animator == null) return;
        if (elementIndex < 0 || elementIndex >= elements.Count) return;

        AnimationElement element = elements[elementIndex];

        if (element.animationClips == null || clipIndex < 0 || clipIndex >= element.animationClips.Length) return;

        if (playRoutine != null)
            StopCoroutine(playRoutine);

        playRoutine = StartCoroutine(PlayAfterEnable(element, clipIndex));
    }

    /// <summary>
    /// Plays a clip inside the element matching a specific page index.
    /// </summary>
    public void PlayClipByPageIndex(int pageIndex, int clipIndex = 0)
    {
        int elementIndex = elements.FindIndex(e => e.pageIndex == pageIndex);
        if (elementIndex != -1)
        {
            PlayClipInElement(elementIndex, clipIndex);
        }
    }

    private IEnumerator PlayAfterEnable(AnimationElement element, int clipIndex)
    {
        animator.enabled = true;

        yield return null;

        animator.Rebind();
        animator.Update(0f);

        AnimationClip clipToPlay = element.animationClips[clipIndex];
        string stateName = "Base Layer." + clipToPlay.name;

        animator.Play(stateName, 0, 0f);
        animator.Update(0f);

        currentElementIndex = elements.IndexOf(element);
        currentClipIndex = clipIndex;

        if (element.unlockNavigationOnPlay)
        {
            PageNavigationController.RequestNavigationUnlock();
        }
    }

    public void PlayNextClipInCurrentElement()
    {
        if (currentElementIndex < 0 || currentElementIndex >= elements.Count) return;

        AnimationElement currentElement = elements[currentElementIndex];
        if (currentElement.animationClips == null || currentElement.animationClips.Length == 0) return;

        int nextClip = currentClipIndex + 1;
        if (nextClip >= currentElement.animationClips.Length)
            nextClip = 0;

        PlayClipInElement(currentElementIndex, nextClip);
    }
}