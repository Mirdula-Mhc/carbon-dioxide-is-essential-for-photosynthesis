using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class AnimationComplete : MonoBehaviour
{
    [Header("Animator Reference")]
    public Animator animator;

    [Header("Animation State Name")]
    public string stateName;

    [Header("Event When Animation Finishes")]
    public UnityEvent onAnimationComplete;

    [Header("Delayed Event Settings")]
    public float delayTime = 2f; // seconds delay
    public UnityEvent onDelayedEvent;

    private bool eventFired = false;

    //public bool istrue = true;
    //public GameObject activefalse;
    //public GameObject manage1;
    //public GameObject manage2;

    void Update()
    {
        if (animator == null || eventFired) return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.IsName(stateName) && stateInfo.normalizedTime >= 1f)
        {
            eventFired = true;

            // Immediate event
            onAnimationComplete?.Invoke();

            // Start delayed event
            StartCoroutine(DelayedEvent());
        }
    }

    //public void Whenenable()
    //{
    //    istrue = true;
    //    activefalse.SetActive(true);

    //    if (istrue = false)
    //    {
    //        activefalse.SetActive(false);
    //    }

    //    if(istrue= true)
    //    {
    //        manage1.SetActive(true);
    //    }

    //    if(istrue= false)
    //    {
    //        manage2.SetActive(false);
    //    }


    //}

    IEnumerator DelayedEvent()
    {
        yield return new WaitForSeconds(delayTime);
        onDelayedEvent?.Invoke();
    }

    // Optional reset
    public void ResetEvent()
    {
        eventFired = false;
    }
}
