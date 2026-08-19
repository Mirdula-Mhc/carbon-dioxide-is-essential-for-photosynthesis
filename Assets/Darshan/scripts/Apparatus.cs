using UnityEngine;

public class Apparatus : MonoBehaviour
{
    [Header("Correct Apparatus")]
    [SerializeField] private bool isCorrect = true;

    [Header("Tick Above Object")]
    [SerializeField] private GameObject worldTick;

    [Header("Tick In UI")]
    [SerializeField] private GameObject uiTick;

    private bool isSelected = false;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.gameObject != gameObject)
                    return;

                if (isSelected)
                    return;

                isSelected = true;

                if (worldTick != null)
                    worldTick.SetActive(true);

                if (uiTick != null)
                    uiTick.SetActive(true);

                if (isCorrect)
                {
                    ExperimentManager.Instance.CorrectSelected();
                }

                Debug.Log(gameObject.name + " Selected");
            }
        }
    }
}