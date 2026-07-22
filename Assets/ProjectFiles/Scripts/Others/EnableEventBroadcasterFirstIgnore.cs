using UnityEngine;
using UnityEngine.Events;

public class EnableEventBroadcasterFirstIgnore : MonoBehaviour
{
    public UnityEvent OnEnabled;
    public UnityEvent OnDisabled;
    public UnityEvent OnStart;

    private bool hasIgnoredFirstEnable;

    private void OnEnable()
    {
        if (!hasIgnoredFirstEnable)
        {
            hasIgnoredFirstEnable = true;
            return; // Ignore only the first OnEnable
        }

        OnEnabled?.Invoke();
    }

    private void Start()
    {
        OnStart?.Invoke();
    }

    private void OnDisable()
    {
        OnDisabled?.Invoke();
    }
}