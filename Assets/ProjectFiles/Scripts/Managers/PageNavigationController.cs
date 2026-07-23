using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class PageNavigationController : MonoBehaviour
{
    [Header("Navigation Buttons")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;

    [Header("Page Display")]
    [SerializeField] private TMP_Text pageNumberText;

    [Header("Page Bounds Configurations")]
    [Tooltip("The starting page number (1-based user facing index).")]
    [SerializeField] private int startPageNumber = 1;

    [Tooltip("The ending page number (1-based user facing index).")]
    [SerializeField] private int endPageNumber = 17;

    [Header("Developer Settings")]
    [Tooltip("Displays the current page using its actual index (0-based). Disable this before making a build.")]
    [SerializeField] private bool developerIndexMode = false;

    [Header("Testing Mode (Ignore Locks)")]
    [SerializeField] private bool testing = false;

    [Header("Requires Interaction Per Page (Navigation Source)")]
    [SerializeField] private List<bool> requiresInteraction = new();

    // Events
    public static event Action<int> OnPageChanged;
    public static event Action OnNavigationUnlockRequested;

    // State
    public static int CurrentIndex { get; private set; }
    public static PageNavigationController Instance { get; private set; }

    [SerializeField] private int currentIndex = 0;

    // Runtime State
    private readonly HashSet<int> visitedPages = new();
    private readonly HashSet<int> completedPages = new();

    // Calculated Bounds Indices (0-based)
    private int StartIndex => Mathf.Max(0, startPageNumber - 1);
    private int EndIndex => Mathf.Max(StartIndex, endPageNumber - 1);
    private int NavigationPageCount => (EndIndex - StartIndex) + 1;

    private void Awake()
    {
        Instance = this;

        // Ensure current index initializes safely within configured start/end bounds
        currentIndex = Mathf.Clamp(currentIndex, StartIndex, EndIndex);
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
        if (currentIndex >= EndIndex)
            return;

        currentIndex++;

        visitedPages.Add(currentIndex);

        UpdateButtons();
        UpdateDisplay();
        RaisePageChanged();
    }

    public void PreviousPage()
    {
        if (currentIndex <= StartIndex)
            return;

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
        // Disable back button at or before start page index
        if (previousButton)
            previousButton.interactable = currentIndex > StartIndex;

        if (testing)
        {
            SetNormalButtonState();
            return;
        }

        bool needsInteraction =
            currentIndex < requiresInteraction.Count &&
            requiresInteraction[currentIndex];

        bool isCompleted = completedPages.Contains(currentIndex);

        // Disable next button if reached end page index or if interaction required
        if (nextButton)
        {
            if (currentIndex >= EndIndex)
            {
                nextButton.interactable = false;
            }
            else if (!needsInteraction)
            {
                nextButton.interactable = true;
            }
            else
            {
                nextButton.interactable = isCompleted;
            }
        }
    }

    private void SetNormalButtonState()
    {
        if (previousButton)
            previousButton.interactable = currentIndex > StartIndex;

        if (nextButton)
            nextButton.interactable = currentIndex < EndIndex;
    }

    /// <summary>
    /// Called by the existing event.
    /// Marks the current page as completed, then refreshes navigation.
    /// </summary>
    public void EnableNavigationButtons()
    {
        completedPages.Add(currentIndex);
        UpdateButtons();
    }

    /// <summary>
    /// Existing API. No dependent scripts need to change.
    /// </summary>
    public static void RequestNavigationUnlock()
    {
        OnNavigationUnlockRequested?.Invoke();
    }

    /// <summary>
    /// Updates the page number display.
    /// Developer Mode ON  : 0/17, 1/17, ..., 16/17
    /// Developer Mode OFF : 1/17, 2/17, ..., 17/17
    /// </summary>
    private void UpdateDisplay()
    {
        if (!pageNumberText)
            return;

        int displayedPage = developerIndexMode
            ? currentIndex
            : currentIndex + 1;

        pageNumberText.text = $"{displayedPage}/{endPageNumber}";
    }

    // Optional helper methods

    public bool IsPageVisited(int pageIndex)
    {
        return visitedPages.Contains(pageIndex);
    }

    public bool IsPageCompleted(int pageIndex)
    {
        return completedPages.Contains(pageIndex);
    }
}