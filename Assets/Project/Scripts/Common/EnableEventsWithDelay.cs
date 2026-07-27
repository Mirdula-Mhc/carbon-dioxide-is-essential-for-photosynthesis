using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class EnableEventsWithDelay : MonoBehaviour
{
    [Header("Events")]
    public UnityEvent onEnableEvent;
    public UnityEvent onClickedDelayed1;
    public UnityEvent onClickedDelayed2;
    public UnityEvent onClickedDelayed3;

    public float delay1 = 2f;
    public float delay2 = 2f;
    public float delay3 = 2f;


    private void OnEnable()
    {
        // Immediate event
        onEnableEvent?.Invoke();
        Debug.Log("Its working");

        // Start delayed events
        StartCoroutine(EnableDelayedEvents());
    }

    private IEnumerator EnableDelayedEvents()
    {
        // Wait 2 seconds
        yield return new WaitForSeconds(delay1);
        onClickedDelayed1?.Invoke();

        // Wait another 3 seconds (total 5 seconds)
        yield return new WaitForSeconds(delay2);
        onClickedDelayed2?.Invoke();

        yield return new WaitForSeconds(delay3);
        onClickedDelayed3?.Invoke();
    }
}
