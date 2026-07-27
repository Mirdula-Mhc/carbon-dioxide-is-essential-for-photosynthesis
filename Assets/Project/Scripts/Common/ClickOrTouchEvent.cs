using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class ClickOrTouchEvent : MonoBehaviour
{
    [Header("Immediate Event")]
    public UnityEvent onClicked;

    [Header("Delayed Event 1")]
    public UnityEvent onClickedDelayed1;
    public float delay1 = 2f;

    [Header("Delayed Event 2")]
    public UnityEvent onClickedDelayed2;
    public float delay2 = 5f;

    public GameObject objectToEnable;
    public GameObject objectToDisable;

    private bool clickTriggered = false;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform == transform && !clickTriggered)
                {
                    clickTriggered = true;

                    // Immediate event
                    onClicked?.Invoke();

                    // Delayed events
                    StartCoroutine(DelayedEvent1());
                    StartCoroutine(DelayedEvent2());

                    if (objectToDisable != null)
                        Destroy(objectToDisable);
                }
            }
        }
    }

    IEnumerator DelayedEvent1()
    {
        yield return new WaitForSeconds(delay1);
        onClickedDelayed1?.Invoke();
    }

    IEnumerator DelayedEvent2()
    {
        yield return new WaitForSeconds(delay2);
        onClickedDelayed2?.Invoke();
    }

    // ---- Existing Methods ----

    public void IsPlugPlaced()
    {
        if (objectToEnable != null)
        {
            objectToEnable.SetActive(true);
            Debug.Log("Electrons Start flow");
        }
    }

    public void IsPlugRemove()
    {
        if (objectToEnable != null)
        {
            objectToEnable.SetActive(false);
            Debug.Log("Electron stop flow");
        }
    }
}
