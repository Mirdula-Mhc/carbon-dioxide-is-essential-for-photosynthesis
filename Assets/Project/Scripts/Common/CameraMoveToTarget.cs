using UnityEngine;
using System.Collections;

public class CameraMoveToTarget : MonoBehaviour
{
    [Header("Camera")]
    public Transform cameraTransform;

    [Header("Movement Settings")]
    public float moveDuration = 1.5f;
    public AnimationCurve positionCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public AnimationCurve rotationCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Camera Move Audio")]
    public AudioSource cameraMoveAudio;

    private Coroutine moveRoutine;

    public void MoveToTarget(Transform targetPoint)
    {
        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(MoveCamera(targetPoint));
    }

    IEnumerator MoveCamera(Transform target)
    {
        Vector3 startPos = cameraTransform.position;
        Quaternion startRot = cameraTransform.rotation;

        Vector3 endPos = target.position;
        Quaternion endRot = target.rotation;

        float time = 0f;

        // 🔊 Play movement sound
        if (cameraMoveAudio != null)
        {
            cameraMoveAudio.Play();
        }

        while (time < moveDuration)
        {
            float t = time / moveDuration;

            float posT = positionCurve.Evaluate(t);
            float rotT = rotationCurve.Evaluate(t);

            cameraTransform.position =
                Vector3.Lerp(startPos, endPos, posT);

            cameraTransform.rotation =
                Quaternion.Slerp(startRot, endRot, rotT);

            time += Time.deltaTime;
            yield return null;
        }

        cameraTransform.position = endPos;
        cameraTransform.rotation = endRot;

        // 🔇 Stop movement sound
        if (cameraMoveAudio != null)
        {
            cameraMoveAudio.Stop();
        }
    }
}
