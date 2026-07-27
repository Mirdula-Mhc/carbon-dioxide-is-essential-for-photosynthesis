using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class PageNavigationController : MonoBehaviour
{
    [System.Serializable]
    public class PageNavigationRule
    {
        [Tooltip("If checked, locks NEXT button until page completion.")]
        public bool requiresInteraction = false;

        [Tooltip("If checked, locks PREVIOUS button until page completion.")]
        public bool lockPreviousUntilCompleted = false;
    }

    [Header("Navigation Buttons")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;

    [Header("Page Display")]
    [SerializeField] private TMP_Text pageNumberText;

    [Header("Page Bounds Configurations")]
    [Tooltip("The starting page number (1-based user facing index, e.g., 25).")]
    [SerializeField] private int startPageNumber = 25;

    [Tooltip("The page that triggers the next set event (1-based, e.g., 34).")]
    [SerializeField] private int eventTriggerPageNumber = 34;

    [Tooltip("The total displayed end page number (1-based user facing index, e.g., 43).")]
    [SerializeField] private int endPageNumber = 43;

    [Header("Developer Settings")]
    [Tooltip("Displays the current page using its actual index (0-based). Disable this before making a build.")]
    [SerializeField] private bool developerIndexMode = false;

    [Header("Testing Mode (Ignore Locks)")]
    [SerializeField] private bool testing = false;

    [Header("Direct Set Swapping (Instant / No Delay)")]
    [Tooltip("The active set GameObject to disable when leaving page 34.")]
    [SerializeField] private GameObject currentSetObject;

    [Tooltip("The next set GameObject to enable when leaving page 34.")]
    [SerializeField] private GameObject nextSetObject;

    [Tooltip("The previous set GameObject to enable when going back past page 25.")]
    [SerializeField] private GameObject previousSetObject;

    [Tooltip("Target GameObject to destroy automatically when moving from Page 25 to 26.")]
    [SerializeField] private GameObject destroyObjectOnPage26;

    [Header("Page Navigation Rules Per Index")]
    [SerializeField] private List<PageNavigationRule> pageRules = new();

    [Header("Set Swap Events (Optional / SFX / Secondary Logic)")]
    [Tooltip("Triggered when clicking Back/Previous while on Start Page (Page 25).")]
    [SerializeField] private UnityEvent OnPage25PreviousClicked;

    [Tooltip("Triggered when clicking Next while on Event Trigger Page (Page 34).")]
    [SerializeField] private UnityEvent OnPage34NextClicked;

    // Deprecated list retained internally to prevent editor serialized data loss during migration
    [HideInInspector]
    [SerializeField] private List<bool> requiresInteraction = new();

    // Events
    public static event Action<int> OnPageChanged;
    public static event Action OnNavigationUnlockRequested;

    // State
    public static int CurrentIndex { get; private set; }
    public static PageNavigationController Instance { get; private set; }

    [SerializeField] private int currentIndex = 24; // 0-based for Page 25

    // Runtime State
    private readonly HashSet<int> visitedPages = new();
    private readonly HashSet<int> completedPages = new();

    // Calculated Bounds Indices (0-based)
    private int StartIndex => Mathf.Max(0, startPageNumber - 1);
    private int TriggerIndex => Mathf.Max(StartIndex, eventTriggerPageNumber - 1);

    private void OnValidate()
    {
        // Safe migration of legacy list
        if (requiresInteraction != null && requiresInteraction.Count > 0 && pageRules.Count == 0)
        {
            for (int i = 0; i < requiresInteraction.Count; i++)
            {
                pageRules.Add(new PageNavigationRule
                {
                    requiresInteraction = requiresInteraction[i],
                    lockPreviousUntilCompleted = false
                });
            }
        }
    }

    private void Awake()
    {
        Instance = this;

        // Ensure current index initializes safely within configured bounds
        currentIndex = Mathf.Clamp(currentIndex, StartIndex, TriggerIndex);
    }

    private void OnEnable()
    {
        OnNavigationUnlockRequested += EnableNavigationButtons;
    }

    private void Start()
    {
        if (nextButton)
            nextButton.onClick.AddListener(NextPage);

        if (previousButton)
            previousButton.onClick.AddListener(PreviousPage);

        visitedPages.Add(currentIndex);

        UpdateButtons();
        UpdateDisplay();
        RaisePageChanged();
    }

    private void OnDisable()
    {
        OnNavigationUnlockRequested -= EnableNavigationButtons;
    }

    private void OnDestroy()
    {
        if (nextButton)
            nextButton.onClick.RemoveListener(NextPage);

        if (previousButton)
            previousButton.onClick.RemoveListener(PreviousPage);

        if (Instance == this)
            Instance = null;
    }

    public void NextPage()
    {
        // Reached Page 34 - Instant Swap
        if (currentIndex >= TriggerIndex)
        {
            if (nextSetObject) nextSetObject.SetActive(true);
            if (currentSetObject) currentSetObject.SetActive(false);

            OnPage34NextClicked?.Invoke();
            return;
        }

        // Check if moving from Page 25 (0-based index 24) to Page 26 (0-based index 25)
        if (currentIndex == StartIndex && destroyObjectOnPage26 != null)
        {
            Destroy(destroyObjectOnPage26);
            destroyObjectOnPage26 = null; // Clear reference after destroying
        }

        currentIndex++;

        visitedPages.Add(currentIndex);

        UpdateButtons();
        UpdateDisplay();
        RaisePageChanged();
    }

    public void PreviousPage()
    {
        // Reached Page 25 - Instant Swap Back
        if (currentIndex <= StartIndex)
        {
            if (previousSetObject) previousSetObject.SetActive(true);
            if (currentSetObject) currentSetObject.SetActive(false);

            OnPage25PreviousClicked?.Invoke();
            return;
        }

        currentIndex--;

        visitedPages.Add(currentIndex);

        UpdateButtons();
        UpdateDisplay();
        RaisePageChanged();
    }

    private void RaisePageChanged()
    {
        CurrentIndex = currentIndex;
        OnPageChanged?.Invoke(currentIndex);
    }

    private void UpdateButtons()
    {
        if (testing)
        {
            SetNormalButtonState();
            return;
        }

        bool isCompleted = completedPages.Contains(currentIndex);

        bool needsNextInteraction = false;
        bool lockPrevious = false;

        if (currentIndex < pageRules.Count)
        {
            needsNextInteraction = pageRules[currentIndex].requiresInteraction;
            lockPrevious = pageRules[currentIndex].lockPreviousUntilCompleted;
        }

        // --- PREVIOUS BUTTON LOCK LOGIC ---
        if (previousButton)
        {
            if (currentIndex <= StartIndex)
            {
                previousButton.interactable = true;
            }
            else if (lockPrevious)
            {
                previousButton.interactable = isCompleted;
            }
            else
            {
                previousButton.interactable = true;
            }
        }

        // --- NEXT BUTTON LOCK LOGIC ---
        if (nextButton)
        {
            // Even on the trigger page (Page 34), require completion if defined by rules
            if (needsNextInteraction)
            {
                nextButton.interactable = isCompleted;
            }
            else
            {
                nextButton.interactable = true;
            }
        }
    }

    private void SetNormalButtonState()
    {
        if (previousButton)
            previousButton.interactable = true;

        if (nextButton)
            nextButton.interactable = true;
    }

    public void EnableNavigationButtons()
    {
        completedPages.Add(currentIndex);
        UpdateButtons();
    }

    public static void RequestNavigationUnlock()
    {
        OnNavigationUnlockRequested?.Invoke();
    }

    private void UpdateDisplay()
    {
        if (!pageNumberText)
            return;

        int displayedPage = developerIndexMode
            ? currentIndex
            : currentIndex + 1;

        // Displays format: 25/43 ... 34/43
        pageNumberText.text = $"{displayedPage}/{endPageNumber}";
    }

    public bool IsPageVisited(int pageIndex)
    {
        return visitedPages.Contains(pageIndex);
    }

    public bool IsPageCompleted(int pageIndex)
    {
        return completedPages.Contains(pageIndex);
    }
}