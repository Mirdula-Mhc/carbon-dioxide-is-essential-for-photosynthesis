using UnityEngine;

public class ExperimentManager : MonoBehaviour
{
    public static ExperimentManager Instance;

    [SerializeField] private int totalCorrectObjects = 2;

    private int currentCorrect = 0;

    private void Awake()
    {
        Instance = this;
    }

    public void CorrectSelected()
    {
        currentCorrect++;

        if (currentCorrect >= totalCorrectObjects)
        {
            Debug.Log("Experiment Complete");

            PageNavigationController.RequestNavigationUnlock();
        }
    }
}