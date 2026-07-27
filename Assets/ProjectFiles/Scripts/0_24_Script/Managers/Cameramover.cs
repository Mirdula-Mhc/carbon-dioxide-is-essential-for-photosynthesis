using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

// -----------------------------------------------------------------
// PageFlowManager calls MoveNext() / MovePrevious() - this script
// has no idea what page triggered it, it just tracks its own index
// into "points" and moves the camera there. Optional: leave
// cameraMover unassigned on the page flow if a project doesn't need
// camera movement at all.
//
// FIX 1: if the camera GameObject also has an Animator that's
// actively driving position/rotation, it overwrites this script's
// Lerp/Slerp in LateUpdate every frame - symptom was "camera moves,
// then snaps back". camAnimator gets disabled for the duration of
// each move and only re-enabled after if the destination page is
// still under external animation control (see FIX 2/3).
//
// FIX 2: on pages where the camera's own Animator+AnimationClip (via
// PageEnterAnimManager) IS the camera movement - i.e. the Animator
// needs to stay ON and driving the camera as soon as that page is
// entered - this script must NOT move the camera or touch camAnimator
// at all, or it fights for control of the same Transform. List those
// page indexes in animatorDrivenPages; MoveNext()/MovePrevious()
// still advance the internal "points" index to stay in sync, they
// just skip the actual Lerp/Animator-disable on those specific pages.
//
// FIX 3: animatorDrivenPages alone only covers page-ENTER animations,
// because that's the only case where the animator starts driving the
// camera at the exact moment MoveTo() runs. Click-triggered camera
// animations (via ClickAnimManager -> AnimationSource.Play()) start
// LATER, while the user is already sitting on the page - CameraMover
// has no way to know about them from a static per-page list checked
// only at Next()/Previous() time.
//
// So ClickAnimManager (or anything else that plays a camera-driving
// AnimationSource mid-page) must explicitly call
// BeginExternalControl() right before playing, and EndExternalControl()
// in its onComplete callback. While externalControlActive is true,
// CameraMover will not start a new Lerp even if MoveTo() is called,
// and will not re-enable camAnimator out from under an animation
// that's still playing. See ClickAnimManager wiring notes below.
// -----------------------------------------------------------------
public class CameraMover : MonoBehaviour
{
    public Transform cam;
    public List<Transform> points;
    public float speed = 4f;

    [Tooltip("Assign if 'cam' also has an Animator that drives position/rotation - it will be disabled during the move and re-enabled after, so it can't overwrite this script's movement.")]
    public Animator camAnimator;

    [Header("Animator-Driven Camera Pages")]
    [Tooltip("Page indexes (matching PageFlowManager's page list / this script's points index) where the camera's own Animator+clip animation IS the camera movement on ENTER. On these pages, this script does nothing at all on Next()/Previous() - no Lerp, no touching camAnimator.")]
    public List<int> animatorDrivenPages;

    int index = 0;
    Coroutine moveRoutine;

    // True while a click-triggered (or any mid-page) camera animation
    // is actively playing. Set via BeginExternalControl()/EndExternalControl(),
    // called by ClickAnimManager around any AnimationSource.Play() that
    // targets the camera.
    bool externalControlActive = false;

    public void MoveNext(Action onComplete = null)
    {
        if (index < points.Count - 1)
        {
            index++;
            MoveTo(index, onComplete);
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    public void MovePrevious(Action onComplete = null)
    {
        if (index > 0)
        {
            index--;
            MoveTo(index, onComplete);
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    // Call right before playing any AnimationSource that drives the
    // camera transform from a click (ClickAnimManager) or any other
    // mid-page trigger. Stops CameraMover's own Lerp and ENABLES
    // camAnimator so the click-triggered clip/timeline can actually
    // drive the camera - it was likely left disabled after the last
    // page-entry move.
    public void BeginExternalControl()
    {
        externalControlActive = true;
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }
        if (camAnimator != null)
            camAnimator.enabled = true;
        Debug.Log("[CameraMover] BeginExternalControl - camAnimator enabled, Lerp suspended.");
    }

    // Call in that AnimationSource's onComplete. Deliberately does NOT
    // touch camAnimator.enabled here - forcing it off (or on) right as
    // the click-anim finishes is what caused the snap-back before.
    // camAnimator is left exactly as the click-anim left it; the next
    // Next()/Previous() page move will disable it again itself if that
    // next page is a plain (non-animator) page.
    public void EndExternalControl()
    {
        externalControlActive = false;
        Debug.Log("[CameraMover] EndExternalControl - Lerp/Animator toggling resumed for next page move.");
    }

    // Call once, at end-of-flow handoff (PageFlowManager.HandOffToNextModule()).
    // Stops any move currently in progress and permanently blocks every
    // future MoveNext()/MovePrevious()/MoveTo() call from doing anything -
    // this simulation's root is about to be disabled, so there's no page
    // left to move the camera FOR, and the next module may want to own
    // the camera itself without this script fighting it.
    //
    // Deliberately does NOT touch camAnimator.enabled - whatever state
    // it's in when handoff happens is left alone, since the next module
    // (or the outgoing page's own animation) may still care about it.
    public void DisableMover()
    {
        externalControlActive = true; // blocks any MoveTo() called after this point

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        enabled = false; // stops Update/LateUpdate if this script ever gains one
        Debug.Log("[CameraMover] DisableMover - handoff complete, this script will no longer move the camera.");
    }

    // pointIndex doubles as the page index, assuming "points" is
    // built 1:1 with PageFlowManager's "pages" list (same order, one
    // entry each). If that's not true for your project, this check
    // will look at the wrong index - confirm before trusting this.
    void MoveTo(int pointIndex, Action onComplete)
    {
        Debug.Log($"[CameraMover] MoveTo pointIndex={pointIndex}, in animatorDrivenPages: {animatorDrivenPages.Contains(pointIndex)}, externalControlActive: {externalControlActive}");

        if (externalControlActive)
        {
            Debug.Log("[CameraMover] Skipping - external (click) animation currently controls the camera.");
            onComplete?.Invoke();
            return;
        }

        if (animatorDrivenPages.Contains(pointIndex))
        {
            // Handing off control to PageEnterAnimManager for this page -
            // the Animator component must actually be enabled or its clip
            // won't evaluate/advance even though Play()/state-info calls
            // still "succeed" and report completion.
            if (camAnimator != null && !camAnimator.enabled)
                camAnimator.enabled = true;
            Debug.Log($"[CameraMover] Skipping - page {pointIndex} is Animator-driven on enter, camAnimator enabled and handed off.");
            // Camera-move part is done as far as PageFlowManager cares -
            // PageEnterAnimManager's own completion gates Next separately.
            onComplete?.Invoke();
            return;
        }

        if (moveRoutine != null) StopCoroutine(moveRoutine);
        // Destination page is a plain (non-animator) page, so camAnimator
        // must stay OFF once the move finishes - otherwise whatever pose
        // its currently-active/default state holds gets written back onto
        // the camera the very next LateUpdate, undoing the Lerp we just did.
        moveRoutine = StartCoroutine(SmoothMove(points[pointIndex], keepAnimatorOffAfter: true, onComplete));
    }

    IEnumerator SmoothMove(Transform target, bool keepAnimatorOffAfter, Action onComplete)
    {
        bool wasEnabled = camAnimator != null && camAnimator.enabled;
        Debug.Log($"[CameraMover] SmoothMove starting, disabling camAnimator: {wasEnabled}");
        if (wasEnabled)
            camAnimator.enabled = false; // stop it overwriting position/rotation in LateUpdate

        while (Vector3.Distance(cam.position, target.position) > 0.01f)
        {
            cam.position = Vector3.Lerp(cam.position, target.position, Time.deltaTime * speed);
            cam.rotation = Quaternion.Slerp(cam.rotation, target.rotation, Time.deltaTime * speed);
            yield return null;
        }
        cam.position = target.position;
        cam.rotation = target.rotation;

        bool reEnable = wasEnabled && !keepAnimatorOffAfter;
        Debug.Log($"[CameraMover] SmoothMove finished, re-enabling camAnimator: {reEnable}");
        if (reEnable)
            camAnimator.enabled = true;

        onComplete?.Invoke();
    }

    public void PrepareForModuleHandoff()
    {
        Debug.Log("[CameraMover] Preparing camera for module handoff.");

        externalControlActive = true;

        // Stop our movement.
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        // Stop Module 1's camera Animator from continuing to drive Transform.
        if (camAnimator != null)
        {
            camAnimator.enabled = false;
        }

        // Stop Module 1's Camera from rendering.
        if (cam != null)
        {
            Camera cameraComponent = cam.GetComponent<Camera>();

            if (cameraComponent != null)
            {
                cameraComponent.enabled = false;
                Debug.Log($"[CameraMover] Disabled outgoing camera: {cam.name}");
            }
        }

        enabled = false;
    }
}