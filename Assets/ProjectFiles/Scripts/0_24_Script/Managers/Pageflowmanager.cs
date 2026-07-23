using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

// -----------------------------------------------------------------
// Handles page navigation AND whether Next is allowed.
//
// Rule per page:
//   - autoComplete = true  -> Next is interactable immediately.
//   - autoComplete = false -> Next stays non-interactable until this
//                             page has received enough calls to
//                             ReportProgress() (see requiredCount).
//
// PageButtonGroup (or any future interaction script) just calls
// PageFlowManager.Instance.ReportProgress() once per button pressed.
// That's the only connection between the two scripts.
// -----------------------------------------------------------------
public class PageFlowManager : MonoBehaviour
{
    public static PageFlowManager Instance { get; private set; }

    [System.Serializable]
    public class Page
    {
        [Tooltip("Label for your own reference only.")]
        public string pageName;

        public GameObject pageRoot;

        [Header("Camera (optional)")]
        public Transform cameraPoint;
        public float cameraSpeed = 4f;

        [Header("Completion")]
        [Tooltip("ON = Next is interactable right away, nothing to do on this page. OFF = Next stays non-interactable until requiredCount interactions are reported.")]
        public bool autoComplete = false;

        [Tooltip("Only used when autoComplete is OFF. How many ReportProgress() calls this page needs before Next unlocks (e.g. 5 if there are 5 buttons to press).")]
        public int requiredCount = 0;

        [Header("Events")]
        public UnityEvent onPageEnter;
        public UnityEvent onPageExit;

        [HideInInspector] public int currentCount = 0;

        public bool IsComplete()
        {
            if (autoComplete) return true;
            if (requiredCount <= 0) return false; // not configured yet - Next must NOT unlock by accident
            return currentCount >= requiredCount;
        }
    }

    [Header("Pages")]
    public List<Page> pages;

    [Header("Page Counter")]
    public TMP_Text pageCounterText;
    public int pageOffset = 1;

    [Header("Navigation")]
    public Button nextButton;
    public Button previousButton;

    [Header("Camera")]
    public Transform mainCamera;
    public float defaultSpeed = 4f;

    private int currentIndex = 0;
    private bool isMoving = false;
    private Coroutine camRoutine;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        nextButton.onClick.AddListener(NextPage);
        previousButton.onClick.AddListener(PreviousPage);

        if (mainCamera != null && pages.Count > 0 && pages[0].cameraPoint != null)
        {
            mainCamera.position = pages[0].cameraPoint.position;
            mainCamera.rotation = pages[0].cameraPoint.rotation;
        }

        ShowPage(0);
    }

    // Call this from any interaction script (e.g. once per button
    // pressed) to add progress toward unlocking the CURRENT page.
    public void ReportProgress()
    {
        pages[currentIndex].currentCount++;
        UpdateUI();
    }

    void ShowPage(int index)
    {
        for (int i = 0; i < pages.Count; i++)
            pages[i].pageRoot.SetActive(i == index);

        var page = pages[index];

        MoveCamera(page);
        page.onPageEnter?.Invoke();

        UpdateUI();
    }

    void NextPage()
    {
        if (isMoving || !pages[currentIndex].IsComplete()) return;

        pages[currentIndex].onPageExit?.Invoke();

        if (currentIndex < pages.Count - 1)
        {
            currentIndex++;
            ShowPage(currentIndex);
        }
    }

    void PreviousPage()
    {
        if (isMoving || currentIndex == 0) return;

        pages[currentIndex].onPageExit?.Invoke();

        currentIndex--;
        ShowPage(currentIndex);
    }

    void MoveCamera(Page page)
    {
        if (mainCamera == null || page.cameraPoint == null) return;

        if (camRoutine != null)
            StopCoroutine(camRoutine);

        float speed = page.cameraSpeed > 0 ? page.cameraSpeed : defaultSpeed;
        camRoutine = StartCoroutine(SmoothMove(page.cameraPoint, speed));
    }

    IEnumerator SmoothMove(Transform target, float speed)
    {
        isMoving = true;
        UpdateUI();

        while (Vector3.Distance(mainCamera.position, target.position) > 0.01f)
        {
            mainCamera.position = Vector3.Lerp(mainCamera.position, target.position, Time.deltaTime * speed);
            mainCamera.rotation = Quaternion.Slerp(mainCamera.rotation, target.rotation, Time.deltaTime * speed);
            yield return null;
        }

        mainCamera.position = target.position;
        mainCamera.rotation = target.rotation;

        isMoving = false;
        UpdateUI();
    }

    void UpdateUI()
    {
        nextButton.interactable = pages[currentIndex].IsComplete() && !isMoving;
        previousButton.interactable = currentIndex > 0 && !isMoving;

        if (pageCounterText != null)
            pageCounterText.text = (currentIndex + pageOffset) + " / " + pages.Count;
    }
}