using UnityEngine;
using UnityEngine.Playables;
using System;
using System.Collections;
using System.Collections.Generic;

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
//   3. Assign targetRenderers (all meshes to highlight together, e.g.
//      a multi-mesh object) + highlightMaterial.
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
    [Tooltip("All renderers to highlight together (e.g. a multi-mesh object). Every one of these gets the highlight material while waiting to be clicked.")]
    public List<Renderer> targetRenderers = new List<Renderer>();
    public Material highlightMaterial;
    List<Material> originalMaterials;

    // Set by ClickAnimManager right before/while this page is active,
    // so OnClickedUI() (fired by Unity's Button component with no
    // args) knows which page's animation data AND completion callback
    // to use.
    [HideInInspector] public AnimationSource pendingSource;
    [HideInInspector] public Action pendingOnComplete;

    // Also set by ClickAnimManager alongside pendingSource - lets
    // OnClickedUI() tell CameraMover to stand down right at the actual
    // moment of the click (not earlier, at page-entry time, since the
    // user may sit on the page a while before clicking). Null/false
    // means this click doesn't touch the camera at all.
    [HideInInspector] public bool pendingDrivesCamera;
    [HideInInspector] public CameraMover pendingCameraMover;

    // Also set by ClickAnimManager alongside pendingSource - the
    // coroutine host to run the wait-for-completion coroutine on
    // (see TriggerClick's coroutineHost parameter for why this
    // matters). Null falls back to running it on this object.
    [HideInInspector] public MonoBehaviour pendingCoroutineHost;

    bool busy = false; // prevents double-clicks while an animation is playing

    public void Highlight()
    {
        busy = false;

        if (isUIObject || targetRenderers == null || targetRenderers.Count == 0 || highlightMaterial == null) return;

        if (originalMaterials == null)
        {
            originalMaterials = new List<Material>();
            foreach (var r in targetRenderers)
                originalMaterials.Add(r != null ? r.material : null);
        }

        foreach (var r in targetRenderers)
            if (r != null) r.material = highlightMaterial;
    }

    // Wired to a UI Button's onClick in the Inspector. Uses whatever
    // ClickAnimManager most recently set as pendingSource for THIS
    // page - see ClickAnimManager.SetPageContext().
    public void OnClickedUI()
    {
        if (pendingDrivesCamera && pendingCameraMover != null)
        {
            pendingCameraMover.BeginExternalControl();
            var originalComplete = pendingOnComplete;
            TriggerClick(pendingSource, () =>
            {
                pendingCameraMover.EndExternalControl();
                originalComplete?.Invoke();
            }, pendingCoroutineHost);
        }
        else
        {
            TriggerClick(pendingSource, pendingOnComplete, pendingCoroutineHost);
        }
    }

    // Called directly by ClickAnimManager for 3D raycast clicks,
    // passing the correct source for the current page explicitly
    // (no reliance on pendingSource, since the manager already has
    // it in hand at the point of the raycast hit).
    //
    // "coroutineHost" lets the caller run the wait-for-completion
    // coroutine on a MonoBehaviour that's guaranteed to stay active
    // for the whole page (e.g. ClickAnimManager) instead of on this
    // object. This matters because some Timelines deactivate the
    // clicked object itself partway through (via an Activation
    // Track) - if the coroutine were running on this object, Unity
    // silently kills it the instant the GameObject deactivates,
    // so onComplete never fires and the page never unlocks. Pass
    // null to keep the old behaviour (coroutine runs on this object).
    public void TriggerClick(AnimationSource source, Action onComplete, MonoBehaviour coroutineHost = null)
    {
        if (busy) return;
        busy = true;

        if (!isUIObject && targetRenderers != null && originalMaterials != null)
        {
            for (int i = 0; i < targetRenderers.Count; i++)
                if (targetRenderers[i] != null && originalMaterials[i] != null)
                    targetRenderers[i].material = originalMaterials[i]; // remove highlight
        }

        var host = coroutineHost != null ? coroutineHost : this;
        host.StartCoroutine(source != null ? source.Play(this, onComplete) : NullSourceFallback(onComplete));
    }

    IEnumerator NullSourceFallback(Action onComplete)
    {
        Debug.LogWarning($"[ClickAnimObject] {name} clicked with no AnimationSource assigned - completing immediately.");
        onComplete?.Invoke();
        yield break;
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
//
// Play() is the single shared playback+completion-wait routine, used
// by both ClickAnimObject (click-triggered) and PageEnterAnimManager
// (auto-triggered on page enter) - one implementation, no duplicated
// logic to keep in sync between the two.
// -----------------------------------------------------------------
[System.Serializable]
public class AnimationSource
{
    [Tooltip("Use this OR the Animator below, not both.")]
    public PlayableDirector director;

    [Header("Animator (clip-driven)")]
    public Animator animator;
    [Tooltip("Drag the AnimationClip to play. Its name is used to call animator.Play(clip.name) - so a state with a matching name must exist in the Animator Controller.")]
    public AnimationClip clip;

    // "runner" is whatever MonoBehaviour should own the coroutine
    // (needs to be something active in the scene - pass "this" from
    // the calling script).
    public IEnumerator Play(MonoBehaviour runner, Action onComplete)
    {
        if (director != null)
        {
            bool done = false;
            void Handler(PlayableDirector d) { done = true; Debug.Log($"[AnimationSource] director.stopped fired for '{director.gameObject.name}'."); }
            director.stopped += Handler;

            // TEMP DIAGNOSTIC - remove once the stuck-completion issue is found.
            Debug.Log($"[AnimationSource] Playing director '{director.gameObject.name}': state={director.state}, duration={director.duration}, time={director.time}, playableAsset={(director.playableAsset != null ? director.playableAsset.name : "NULL")}");

            director.Play();

            Debug.Log($"[AnimationSource] After Play() call: state={director.state}, time={director.time}");

            while (!done) yield return null;
            director.stopped -= Handler;
        }
        else if (animator != null && clip != null)
        {
            animator.Play(clip.name, 0, 0f);

            // Wait one frame so the new state actually takes effect
            // before we start checking progress against it.
            yield return null;

            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (!stateInfo.IsName(clip.name))
            {
                Debug.LogWarning($"[AnimationSource] {runner.name}: Animator has no state named '{clip.name}' (must match the clip name exactly). Falling back to a fixed wait of {clip.length}s based on the clip's length.");
                yield return new WaitForSeconds(clip.length);
            }
            else
            {
                while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
                    yield return null;
            }
        }
        else
        {
            Debug.LogWarning($"[AnimationSource] {runner.name}: AnimationSource has neither a PlayableDirector nor an Animator+clip assigned - completing immediately.");
        }

        onComplete?.Invoke();
    }
}