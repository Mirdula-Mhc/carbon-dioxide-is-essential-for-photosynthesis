using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

public class SlideCameraController : MonoBehaviour
{
    [System.Serializable]
    public class Step
    {
        [Header("Identification")]
        [Tooltip("Logical page number / ID (not list index)")]
        public int pageNumber;

        [Header("Camera")]
        public Transform cameraPoint;

        [Header("UI")]
        public GameObject slideUI;
        [Tooltip("CanvasGroup used for smooth fade (required)")]
        public CanvasGroup canvasGroup;

        [Header("Events")]
        public UnityEvent onNextClicked;
        public UnityEvent onBackClicked;
        public UnityEvent onRepeatedNextClicked;



        [Header("Special UI Controls")]
        public bool enableNextOnStart = false;
        [Header("Navigation Control")]
        public bool lockPreviousButton = false;

        public bool showNumpad = false;
        public GameObject numpadObject;

        public bool showCalculator = false;
        public GameObject calculatorObject;
    }

    [Header("Camera")]
    public Transform cameraTransform;

    [Header("Camera Movement")]
    [Min(0.1f)]
    public float moveDuration = 1.5f;

    [Header("Camera Motion Curves")]
    [SerializeField]
    private AnimationCurve positionCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [SerializeField]
    private AnimationCurve rotationCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Steps")]
    public List<Step> steps;

    [Header("Navigation Buttons")]
    public Button nextButton;
    public Button previousButton;


    public UnityEvent onFirstSlideBackClick;
    public UnityEvent onLastSlideNextClick;

    [Header("Slide Counter UI")]
    public TextMeshProUGUI slideCounterText;

    // 🔹 ONLY NEW FIELD ADDED
    [Header("Slide Counter")]
    public string totalSlideText;

    [Header("Debug")]
    public bool enableCheatKey = true;

    int currentIndex;
    bool isMoving;

    bool[] slideCompleted;
    bool[] slideLocked;
    bool[] eventUsed;
    bool[] backUnlocked;

    public int CurrentSlideIndex => currentIndex;

    void Start()
    {
        slideCompleted = new bool[steps.Count];
        slideLocked = new bool[steps.Count];
        eventUsed = new bool[steps.Count];
        backUnlocked = new bool[steps.Count];

        foreach (var step in steps)
        {
            if (step.slideUI != null)
                step.slideUI.SetActive(true);

            if (step.canvasGroup != null)
            {
                step.canvasGroup.alpha = 0f;
                step.canvasGroup.interactable = false;
                step.canvasGroup.blocksRaycasts = false;
            }
        }

        currentIndex = 0;

        cameraTransform.position = steps[0].cameraPoint.position;
        cameraTransform.rotation = steps[0].cameraPoint.rotation;

        ShowSlideInstant(steps[0]);
        ApplyStepSettings(0);

        nextButton.interactable = slideCompleted[0];
        UpdateBackButton();
        UpdateSlideCounterUI();
    }

    void Update()
    {
        if (!enableCheatKey) return;

        if (Input.GetKeyDown(KeyCode.N))
            ForceNextSlide_Cheat();
    }

    void ForceNextSlide_Cheat()
    {
        if (currentIndex >= steps.Count - 1) return;

        StopAllCoroutines();
        isMoving = false;

        slideCompleted[currentIndex] = true;
        slideLocked[currentIndex] = true;
        nextButton.interactable = true;

        StartCoroutine(MoveTo(currentIndex + 1));
    }

    public void EnableNextButton()
    {
        slideCompleted[currentIndex] = true;
        slideLocked[currentIndex] = true;

        // Unlock Back permanently for this step
        backUnlocked[currentIndex] = true;

        nextButton.interactable = true;
        previousButton.interactable = true;

        UpdateBackButton();
    }

    public void Next()
    {
        if (isMoving) return;
        if (!slideCompleted[currentIndex]) return;

        // Last slide
        if (currentIndex >= steps.Count - 1)
        {
            onLastSlideNextClick?.Invoke();
            return;
        }

        if (!eventUsed[currentIndex])
        {
            steps[currentIndex].onNextClicked?.Invoke();
            eventUsed[currentIndex] = true;
        }

        steps[currentIndex].onRepeatedNextClicked?.Invoke();

        StartCoroutine(MoveTo(currentIndex + 1));
    }

    public void Previous()
    {
        if (isMoving) return;

        // Back still locked on this step
        if (steps[currentIndex].lockPreviousButton &&
            !backUnlocked[currentIndex])
            return;

        // First slide
        if (currentIndex <= 0)
        {
            onFirstSlideBackClick?.Invoke();
            return;
        }

        steps[currentIndex].onBackClicked?.Invoke();

        StartCoroutine(MoveTo(currentIndex - 1));
    }
    IEnumerator MoveTo(int targetIndex)
    {
        isMoving = true;

        Step from = steps[currentIndex];
        Step to = steps[targetIndex];

        Vector3 startPos = cameraTransform.position;
        Quaternion startRot = cameraTransform.rotation;

        Vector3 endPos = to.cameraPoint.position;
        Quaternion endRot = to.cameraPoint.rotation;

        float time = 0f;

        HideSlide(from);

        while (time < moveDuration)
        {
            float t = time / moveDuration;

            float posT = positionCurve.Evaluate(t);
            float rotT = rotationCurve.Evaluate(t);

            cameraTransform.position = Vector3.Lerp(startPos, endPos, posT);
            cameraTransform.rotation = Quaternion.Slerp(startRot, endRot, rotT);

            time += Time.deltaTime;
            yield return null;
        }

        cameraTransform.position = endPos;
        cameraTransform.rotation = endRot;

        ShowSlide(to);

        currentIndex = targetIndex;
        ApplyStepSettings(currentIndex);

        nextButton.interactable = slideCompleted[currentIndex];
        UpdateBackButton();
        UpdateSlideCounterUI();

        isMoving = false;
    }

    void ShowSlide(Step step)
    {
        if (step.slideUI != null)
            step.slideUI.SetActive(true);

        if (step.canvasGroup != null)
        {
            step.canvasGroup.alpha = 1f;
            step.canvasGroup.interactable = true;
            step.canvasGroup.blocksRaycasts = true;
        }
    }

    void HideSlide(Step step)
    {
        if (step.canvasGroup != null)
        {
            step.canvasGroup.alpha = 0f;
            step.canvasGroup.interactable = false;
            step.canvasGroup.blocksRaycasts = false;
        }
    }

    void ShowSlideInstant(Step step)
    {
        if (step.slideUI != null)
            step.slideUI.SetActive(true);

        if (step.canvasGroup != null)
        {
            step.canvasGroup.alpha = 1f;
            step.canvasGroup.interactable = true;
            step.canvasGroup.blocksRaycasts = true;
        }
    }

    void ApplyStepSettings(int index)
    {
        Step s = steps[index];

        if (slideLocked[index])
        {
            slideCompleted[index] = true;
            DisableSpecialUI(s);
            return;
        }

        if (s.enableNextOnStart)
            slideCompleted[index] = true;

        if (s.numpadObject != null)
            s.numpadObject.SetActive(s.showNumpad);

        if (s.calculatorObject != null)
            s.calculatorObject.SetActive(s.showCalculator);
    }

    void DisableSpecialUI(Step s)
    {
        if (s.numpadObject != null)
            s.numpadObject.SetActive(false);

        if (s.calculatorObject != null)
            s.calculatorObject.SetActive(false);
    }

    void UpdateBackButton()
    {
        // If unlocked once, always allow Back
        if (backUnlocked[currentIndex])
        {
            previousButton.interactable = true;
            return;
        }

        // First visit: obey lock setting
        if (steps[currentIndex].lockPreviousButton)
        {
            previousButton.interactable = false;
        }
        else
        {
            previousButton.interactable = true;
        }
    }

    public void OnClear()
    {
        if (nextButton = null)
        { previousButton.interactable = true; }

    }

    // 🔹 ONLY LOGIC CHANGE USED HERE
    void UpdateSlideCounterUI()
    {
        if (slideCounterText == null) return;

        slideCounterText.text =
            $"{steps[currentIndex].pageNumber} / {totalSlideText}";
    }
}
