using UnityEngine;
using UnityEngine.Playables;
using System;
using System.Collections;

// -----------------------------------------------------------------
// One clickable object. Deliberately owns NO animation data itself -
// it just detects the click and highlights (3D only). The animation
// to play is passed in at click-time by ClickAnimManager, because
// the SAME object can appear on multiple pages with a DIFFERENT
// animation each time (e.g. object X plays anim A on page 3, anim B
// on page 7). Baking one fixed Animator/Director into this component
// would break that case.
//
// Setup - UI object:
//   1. Put this on the same object as (or a child of) a Button.
//   2. isUIObject = true. No highlight material needed.
//   3. Wire the Button's onClick -> this component's OnClickedUI().
//
// Setup - 3D object:
//   1. Put this on the object itself (needs a Collider).
//   2. isUIObject = false.
//   3. Assign targetRenderer + highlightMaterial.
//   Click detection is handled by ClickAnimManager's raycast - no
//   extra wiring needed here.
//
// ClickAnimManager calls TriggerClick(...) with the specific
// animation source for whichever page is currently active - see
// ClickAnimManager.OnObjectClicked().
// -----------------------------------------------------------------
public class ClickAnimObject : MonoBehaviour
{
    [Header("Type")]
    public bool isUIObject = false;

    [Header("3D Highlight (ignored for UI objects)")]
    public Renderer targetRenderer;
    public Material highlightMaterial;
    Material originalMaterial;

    // Set by ClickAnimManager right before/while this page is active,
    // so OnClickedUI() (fired by Unity's Button component with no
    // args) knows which page's animation data AND completion callback
    // to use.
    [HideInInspector] public AnimationSource pendingSource;
    [HideInInspector] public Action pendingOnComplete;

    bool busy = false; // prevents double-clicks while an animation is playing

    public void Highlight()
    {
        busy = false;

        if (isUIObject || targetRenderer == null || highlightMaterial == null) return;

        if (originalMaterial == null)
            originalMaterial = targetRenderer.material;

        targetRenderer.material = highlightMaterial;
    }

    // Wired to a UI Button's onClick in the Inspector. Uses whatever
    // ClickAnimManager most recently set as pendingSource for THIS
    // page - see ClickAnimManager.SetPageContext().
    public void OnClickedUI()
    {
        TriggerClick(pendingSource, pendingOnComplete);
    }

    // Called directly by ClickAnimManager for 3D raycast clicks,
    // passing the correct source for the current page explicitly
    // (no reliance on pendingSource, since the manager already has
    // it in hand at the point of the raycast hit).
    public void TriggerClick(AnimationSource source, Action onComplete)
    {
        if (busy) return;
        busy = true;

        if (!isUIObject && targetRenderer != null && originalMaterial != null)
            targetRenderer.material = originalMaterial; // remove highlight

        StartCoroutine(PlayAndWait(source, onComplete));
    }

    IEnumerator PlayAndWait(AnimationSource source, Action onComplete)
    {
        if (source == null)
        {
            Debug.LogWarning($"[ClickAnimObject] {name} clicked with no AnimationSource assigned - completing immediately.");
            onComplete?.Invoke();
            yield break;
        }

        if (source.director != null)
        {
            bool done = false;
            void Handler(PlayableDirector d) { done = true; }
            source.director.stopped += Handler;
            source.director.Play();
            while (!done) yield return null;
            source.director.stopped -= Handler;
        }
        else if (source.legacyAnimation != null && source.clip != null)
        {
            // Legacy Animation component - the clip starts by itself
            // when Play() is called, no trigger/state machine involved.
            if (!source.legacyAnimation.GetClip(source.clip.name))
                source.legacyAnimation.AddClip(source.clip, source.clip.name);

            source.legacyAnimation.Play(source.clip.name);

            yield return null; // let isPlaying actually turn true before polling
            while (source.legacyAnimation.IsPlaying(source.clip.name))
                yield return null;
        }
        else
        {
            Debug.LogWarning($"[ClickAnimObject] {name} AnimationSource has neither a PlayableDirector nor a legacy Animation+clip assigned - completing immediately.");
        }

        onComplete?.Invoke();
    }

    // Call if a page can be revisited and should require the click again.
    public void ResetForRevisit()
    {
        busy = false;
        Highlight();
    }
}

// -----------------------------------------------------------------
// Which animation to play for a given (page, object) pairing. Lives
// on the manager's per-page entries, NOT on ClickAnimObject, so the
// same object can have a different one of these per page.
// -----------------------------------------------------------------
[System.Serializable]
public class AnimationSource
{
    [Tooltip("Use this OR the legacy Animation+clip below, not both.")]
    public PlayableDirector director;

    [Header("Legacy Animation component")]
    [Tooltip("The Animation component on the object (or wherever the clip lives).")]
    public Animation legacyAnimation;
    public AnimationClip clip;
}