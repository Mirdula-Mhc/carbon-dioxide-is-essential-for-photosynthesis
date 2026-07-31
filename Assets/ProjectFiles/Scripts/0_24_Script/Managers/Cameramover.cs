using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class CameraMover : MonoBehaviour
{
    [Header("Camera")]
    public Transform cam;

    [Header("Page Camera Points")]
    public List<Transform> points = new List<Transform>();

    [Header("Movement")]
    public float speed = 4f;

    [Header("Camera Animator")]
    [Tooltip("Animator that can drive the same camera transform.")]
    public Animator camAnimator;

    [Header("Animator-Driven Camera Pages")]
    [Tooltip(
        "Page indexes where PageEnterAnimManager drives the camera. " +
        "CameraMover will not Lerp on entry to these pages."
    )]
    public List<int> animatorDrivenPages = new List<int>();

    private int index = 0;
    private Coroutine moveRoutine;

    private bool externalControlActive = false;
    private bool handedOff = false;

    // =========================================================
    // NORMAL PAGE MOVEMENT
    // =========================================================

    public void MoveNext(Action onComplete = null)
    {
        EnsureReady();

        if (points == null || points.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

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
        EnsureReady();

        if (points == null || points.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

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

    // =========================================================
    // DIRECT SYNCHRONIZATION
    // =========================================================

    public void SetPageIndex(int pageIndex, bool moveImmediately = false)
    {
        EnsureReady();

        if (points == null || points.Count == 0)
        {
            index = 0;
            return;
        }

        index = Mathf.Clamp(pageIndex, 0, points.Count - 1);

        if (moveImmediately)
            MoveTo(index, null);
    }

    public int CurrentIndex => index;

    // =========================================================
    // CLICK / EXTERNAL CAMERA CONTROL
    // =========================================================

    public void BeginExternalControl()
    {
        EnsureReady();

        externalControlActive = true;

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        if (camAnimator != null)
            camAnimator.enabled = true;

        Debug.Log(
            "[CameraMover] BeginExternalControl - " +
            "camera Animator enabled, normal movement suspended."
        );
    }

    public void EndExternalControl()
    {
        externalControlActive = false;

        Debug.Log(
            "[CameraMover] EndExternalControl - " +
            "normal camera movement restored."
        );
    }

    // =========================================================
    // MODULE HANDOFF
    // =========================================================

    public void PrepareForModuleHandoff()
    {
        Debug.Log("[CameraMover] Preparing for module handoff.");

        handedOff = true;
        externalControlActive = false;

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        if (camAnimator != null)
            camAnimator.enabled = false;

        Camera cameraComponent = GetCameraComponent();

        if (cameraComponent != null)
        {
            cameraComponent.enabled = false;

            Debug.Log(
                $"[CameraMover] Disabled outgoing camera '{cameraComponent.name}'."
            );
        }

        // We can disable this component during handoff,
        // but RestoreFromModuleHandoff() can bring it back.
        enabled = false;
    }

    public void RestoreFromModuleHandoff(int pageIndex)
    {
        Debug.Log(
            $"[CameraMover] Restoring module camera at page {pageIndex}."
        );

        handedOff = false;
        externalControlActive = false;

        enabled = true;

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        Camera cameraComponent = GetCameraComponent();

        if (cameraComponent != null)
            cameraComponent.enabled = true;

        if (points != null && points.Count > 0)
            index = Mathf.Clamp(pageIndex, 0, points.Count - 1);
        else
            index = 0;

        // Do NOT leave the Animator driving the camera just because
        // it happened to be enabled before the handoff.
        bool animatorPage =
            animatorDrivenPages != null &&
            animatorDrivenPages.Contains(index);

        if (camAnimator != null)
            camAnimator.enabled = animatorPage;

        // When returning from another module, put the camera at
        // this module's current page immediately.
        if (!animatorPage &&
            points != null &&
            index >= 0 &&
            index < points.Count &&
            points[index] != null &&
            cam != null)
        {
            cam.position = points[index].position;
            cam.rotation = points[index].rotation;
        }

        Debug.Log(
            $"[CameraMover] Restored. Index={index}, " +
            $"AnimatorPage={animatorPage}"
        );
    }

    // Keep this for compatibility if anything still calls it.
    public void DisableMover()
    {
        PrepareForModuleHandoff();
    }

    // =========================================================
    // INTERNAL MOVEMENT
    // =========================================================

    private void MoveTo(int pointIndex, Action onComplete)
    {
        EnsureReady();

        if (cam == null)
        {
            Debug.LogWarning("[CameraMover] Camera Transform is not assigned.");
            onComplete?.Invoke();
            return;
        }

        if (points == null ||
            pointIndex < 0 ||
            pointIndex >= points.Count ||
            points[pointIndex] == null)
        {
            Debug.LogWarning(
                $"[CameraMover] Invalid camera point {pointIndex}."
            );

            onComplete?.Invoke();
            return;
        }

        bool animatorDriven =
            animatorDrivenPages != null &&
            animatorDrivenPages.Contains(pointIndex);

        Debug.Log(
            $"[CameraMover] MoveTo {pointIndex} | " +
            $"AnimatorDriven={animatorDriven} | " +
            $"ExternalControl={externalControlActive}"
        );

        if (externalControlActive)
        {
            Debug.LogWarning(
                "[CameraMover] Move requested during external control. " +
                "Completing without starting a competing camera move."
            );

            onComplete?.Invoke();
            return;
        }

        if (animatorDriven)
        {
            if (camAnimator != null)
                camAnimator.enabled = true;

            // PageEnterAnimManager owns the actual movement.
            onComplete?.Invoke();
            return;
        }

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        moveRoutine = StartCoroutine(
            SmoothMove(
                points[pointIndex],
                () =>
                {
                    moveRoutine = null;
                    onComplete?.Invoke();
                }
            )
        );
    }

    private IEnumerator SmoothMove(
        Transform target,
        Action onComplete)
    {
        if (camAnimator != null)
            camAnimator.enabled = false;

        while (
            Vector3.Distance(cam.position, target.position) > 0.01f ||
            Quaternion.Angle(cam.rotation, target.rotation) > 0.1f
        )
        {
            cam.position = Vector3.Lerp(
                cam.position,
                target.position,
                Time.deltaTime * speed
            );

            cam.rotation = Quaternion.Slerp(
                cam.rotation,
                target.rotation,
                Time.deltaTime * speed
            );

            yield return null;
        }

        cam.position = target.position;
        cam.rotation = target.rotation;

        onComplete?.Invoke();
    }

    // =========================================================
    // SAFETY
    // =========================================================

    private void EnsureReady()
    {
        // This protects us if the module root gets re-enabled but
        // nobody explicitly called RestoreFromModuleHandoff().
        if (!handedOff)
            return;

        Debug.LogWarning(
            "[CameraMover] Movement requested after handoff. " +
            "Automatically restoring CameraMover."
        );

        handedOff = false;
        externalControlActive = false;
        enabled = true;

        Camera cameraComponent = GetCameraComponent();

        if (cameraComponent != null)
            cameraComponent.enabled = true;
    }

    private Camera GetCameraComponent()
    {
        if (cam == null)
            return null;

        return cam.GetComponent<Camera>();
    }
}