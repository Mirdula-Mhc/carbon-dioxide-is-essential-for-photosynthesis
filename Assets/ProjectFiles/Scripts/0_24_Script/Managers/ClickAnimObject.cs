using UnityEngine;
using UnityEngine.Playables;
using System;
using System.Collections;

public class ClickAnimObject : MonoBehaviour
{
    [Header("Type")]
    public bool isUIObject = false;

    [Header("3D Highlight (ignored for UI objects)")]
    public Renderer targetRenderer;
    public Material highlightMaterial;
    Material originalMaterial;

    [HideInInspector] public AnimationSource pendingSource;
    [HideInInspector] public Action pendingOnComplete;

    bool busy = false;

    public void Highlight()
    {
        busy = false;

        if (isUIObject || targetRenderer == null || highlightMaterial == null) return;

        if (originalMaterial == null)
            originalMaterial = targetRenderer.material;

        targetRenderer.material = highlightMaterial;
    }

    public void OnClickedUI()
    {
        TriggerClick(pendingSource, pendingOnComplete);
    }

    public void TriggerClick(AnimationSource source, Action onComplete)
    {
        if (busy) return;
        busy = true;

        if (!isUIObject && targetRenderer != null && originalMaterial != null)
            targetRenderer.material = originalMaterial;

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
            if (!source.legacyAnimation.GetClip(source.clip.name))
                source.legacyAnimation.AddClip(source.clip, source.clip.name);

            source.legacyAnimation.Play(source.clip.name);

            yield return null;
            while (source.legacyAnimation.IsPlaying(source.clip.name))
                yield return null;
        }
        else
        {
            Debug.LogWarning($"[ClickAnimObject] {name} AnimationSource has neither a PlayableDirector nor a legacy Animation+clip assigned - completing immediately.");
        }

        onComplete?.Invoke();
    }

    public void ResetForRevisit()
    {
        busy = false;
        Highlight();
    }
}

[System.Serializable]
public class AnimationSource
{
    [Tooltip("Use this OR the legacy Animation+clip below, not both.")]
    public PlayableDirector director;

    [Header("Legacy Animation component")]
    [Tooltip("The Animation component on the object (or wherever the clip lives).")]
    public Animation legacyAnimation;
    public AnimationClip clip;

    // Added for PageEnterAnimManager - plays this animation and
    // invokes onComplete when done. "runner" just needs to be any
    // active MonoBehaviour to host the coroutine on.
    public IEnumerator Play(MonoBehaviour runner, Action onComplete)
    {
        if (director != null)
        {
            bool done = false;
            void Handler(PlayableDirector d) { done = true; }
            director.stopped += Handler;
            director.Play();
            while (!done) yield return null;
            director.stopped -= Handler;
        }
        else if (legacyAnimation != null && clip != null)
        {
            if (!legacyAnimation.GetClip(clip.name))
                legacyAnimation.AddClip(clip, clip.name);

            legacyAnimation.Play(clip.name);

            yield return null;
            while (legacyAnimation.IsPlaying(clip.name))
                yield return null;
        }
        else
        {
            Debug.LogWarning("[AnimationSource] Play() called with neither a PlayableDirector nor a legacy Animation+clip assigned - completing immediately.");
        }

        onComplete?.Invoke();
    }
}