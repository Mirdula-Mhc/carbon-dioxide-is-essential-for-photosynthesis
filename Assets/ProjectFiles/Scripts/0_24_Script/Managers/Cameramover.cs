using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// -----------------------------------------------------------------
// PageFlowManager calls MoveNext() / MovePrevious() - this script
// has no idea what page triggered it, it just tracks its own index
// into "points" and moves the camera there. Optional: leave
// cameraMover unassigned on the page flow if a project doesn't need
// camera movement at all.
// -----------------------------------------------------------------
public class CameraMover : MonoBehaviour
{
    public Transform cam;
    public List<Transform> points;
    public float speed = 4f;

    int index = 0;
    Coroutine moveRoutine;

    public void MoveNext()
    {
        if (index < points.Count - 1)
        {
            index++;
            MoveTo(points[index]);
        }
    }

    public void MovePrevious()
    {
        if (index > 0)
        {
            index--;
            MoveTo(points[index]);
        }
    }

    void MoveTo(Transform target)
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(SmoothMove(target));
    }

    IEnumerator SmoothMove(Transform target)
    {
        while (Vector3.Distance(cam.position, target.position) > 0.01f)
        {
            cam.position = Vector3.Lerp(cam.position, target.position, Time.deltaTime * speed);
            cam.rotation = Quaternion.Slerp(cam.rotation, target.rotation, Time.deltaTime * speed);
            yield return null;
        }
        cam.position = target.position;
        cam.rotation = target.rotation;
    }
}