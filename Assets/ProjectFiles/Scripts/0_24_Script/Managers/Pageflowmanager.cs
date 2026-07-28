using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PageFlowManager : MonoBehaviour
{
    public static PageFlowManager Instance { get; private set; }

    [Header("Pages")]
    public List<GameObject> pages;

    [Header("Navigation")]
    public Button nextButton;
    public Button prevButton;

    [Header("Page Counter")]
    public TMP_Text pageCounterText;
    public int pageOffset = 1;
    public int totalPageCount = 43;

    [Header("Camera (optional)")]
    public CameraMover cameraMover;

    [Header("Interaction Managers (optional)")]
    public ButtonGroupManager buttonGroupManager;
    public ClickAnimManager clickAnimManager;
    public PageVisibilityManager pageVisibilityManager;
    public PageEnterAnimManager pageEnterAnimManager;

    [Header("Auto Complete Pages")]
    [Tooltip("Pages with no interaction. Next unlocks after all active animation locks have finished.")]
    public List<int> autoCompletePages;

    [Header("End-of-Flow Handoff (optional)")]
    public GameObject ownRootObject;
    public GameObject nextModuleRootObject;

    private int currentPage = 0;

    // IMPORTANT:
    // Counter instead of bool because camera + another animation
    // may be running at the same time.
    private readonly HashSet<string> interactionLocks = new();

    private bool InteractionLocked => interactionLocks.Count > 0;

    // Completion tracking
    private readonly HashSet<int> completedButtonGroupPages = new();
    private readonly HashSet<int> completedClickAnimPages = new();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (nextButton != null)
            nextButton.onClick.AddListener(Next);

        if (prevButton != null)
            prevButton.onClick.AddListener(Previous);

        ShowPage(0);
    }

    // =========================================================
    // NEXT
    // =========================================================

    public void Next()
    {
        if (InteractionLocked)
            return;

        if (currentPage < pages.Count - 1)
        {
            int targetPage = currentPage + 1;
            string cameraLock = $"CameraMove_{targetPage}";

            if (cameraMover != null)
                LockInteraction(cameraLock);

            currentPage = targetPage;

            ShowPage(currentPage);

            if (cameraMover != null)
            {
                cameraMover.MoveNext(() =>
                {
                    UnlockInteraction(cameraLock);
                });
            }
        }
        else if (nextButton != null && nextButton.interactable)
        {
            HandOffToNextModule();
        }
    }

    // =========================================================
    // PREVIOUS
    // =========================================================

    public void Previous()
    {
        if (InteractionLocked)
            return;

        if (currentPage > 0)
        {
            int targetPage = currentPage - 1;
            string cameraLock = $"CameraMove_{targetPage}";

            if (cameraMover != null)
                LockInteraction(cameraLock);

            currentPage = targetPage;

            ShowPage(currentPage);

            if (cameraMover != null)
            {
                cameraMover.MovePrevious(() =>
                {
                    UnlockInteraction(cameraLock);
                });
            }
        }
    }

    // =========================================================
    // PAGE DISPLAY
    // =========================================================

    void ShowPage(int index)
    {
        for (int i = 0; i < pages.Count; i++)
        {
            if (pages[i] != null)
                pages[i].SetActive(i == index);
        }

        buttonGroupManager?.SetPageContext(index);
        clickAnimManager?.SetPageContext(index);
        pageVisibilityManager?.SetPageContext(index);
        pageEnterAnimManager?.SetPageContext(index);

        UpdatePageCounter();
        RefreshNavigation();
    }

    // =========================================================
    // PAGE COUNTER
    // =========================================================

    void UpdatePageCounter()
    {
        if (pageCounterText == null)
            return;

        int displayedPage = currentPage + pageOffset;
        pageCounterText.text = $"{displayedPage}/{totalPageCount}";
    }

    // =========================================================
    // NAVIGATION STATE
    // =========================================================

    void RefreshNavigation()
    {
        // Any animation/camera lock disables BOTH buttons.
        if (InteractionLocked)
        {
            if (nextButton != null)
                nextButton.interactable = false;

            if (prevButton != null)
                prevButton.interactable = false;

            return;
        }

        bool isAutoComplete =
            autoCompletePages != null &&
            autoCompletePages.Contains(currentPage);

        bool hasButtonGroup =
            buttonGroupManager != null &&
            buttonGroupManager.OwnsPage(currentPage);

        bool hasClickAnim =
            clickAnimManager != null &&
            clickAnimManager.OwnsPage(currentPage);

        bool hasPageEnterAnim =
            pageEnterAnimManager != null &&
            pageEnterAnimManager.OwnsPage(currentPage);

        bool hasAnyGate =
            hasButtonGroup ||
            hasClickAnim ||
            hasPageEnterAnim;

        // Locked by default.
        bool pageComplete = false;

        if (isAutoComplete)
        {
            pageComplete = true;
        }
        else if (hasAnyGate)
        {
            pageComplete = true;

            if (hasButtonGroup &&
                !completedButtonGroupPages.Contains(currentPage))
            {
                pageComplete = false;
            }

            if (hasClickAnim &&
                !completedClickAnimPages.Contains(currentPage))
            {
                pageComplete = false;
            }

            if (hasPageEnterAnim &&
                !pageEnterAnimManager.IsPageDone(currentPage))
            {
                pageComplete = false;
            }
        }
        else
        {
            Debug.LogWarning(
                $"[PageFlowManager] Page {currentPage} has NO completion rule. " +
                "Next remains locked. Add it to Auto Complete Pages " +
                "or configure an interaction."
            );
        }

        if (nextButton != null)
            nextButton.interactable = pageComplete;

        if (prevButton != null)
            prevButton.interactable = currentPage > 0;

        Debug.Log(
            $"[PageFlow] Page {currentPage} | " +
            $"Complete={pageComplete} | " +
            $"Locks={interactionLocks.Count} | " +
            $"Auto={isAutoComplete} | " +
            $"ButtonGroup={hasButtonGroup} | " +
            $"ClickAnim={hasClickAnim} | " +
            $"EnterAnim={hasPageEnterAnim}"
        );
    }

    // =========================================================
    // COMPLETION EVENTS
    // =========================================================

    public void OnButtonGroupDone()
    {
        completedButtonGroupPages.Add(currentPage);

        Debug.Log(
            $"[PageFlow] Button group completed on page {currentPage}"
        );

        RefreshNavigation();
    }

    public void OnClickAnimDone()
    {
        completedClickAnimPages.Add(currentPage);

        Debug.Log(
            $"[PageFlow] Click animation gate completed on page {currentPage}"
        );

        RefreshNavigation();
    }

    public void OnPageEnterAnimDone()
    {
        Debug.Log(
            $"[PageFlow] Page-enter animation gate completed on page {currentPage}"
        );

        RefreshNavigation();
    }

    // =========================================================
    // GLOBAL ANIMATION / CAMERA LOCK
    // =========================================================

    public void LockInteraction(string source)
    {
        if (string.IsNullOrEmpty(source))
            return;

        interactionLocks.Add(source);

        Debug.Log(
            $"[PageFlow] LOCK + {source} | Active locks: " +
            string.Join(", ", interactionLocks)
        );

        RefreshNavigation();
    }

    public void UnlockInteraction(string source)
    {
        if (string.IsNullOrEmpty(source))
            return;

        bool removed = interactionLocks.Remove(source);

        Debug.Log(
            $"[PageFlow] LOCK - {source} | Removed={removed} | Active locks: " +
            string.Join(", ", interactionLocks)
        );

        RefreshNavigation();
    }

    // =========================================================
    // EXTERNAL NEXT ENABLE
    // =========================================================

    public void EnableNextButton()
    {
        // Do not allow an external script to enable Next
        // while an animation/camera movement is still running.
        if (InteractionLocked)
            return;

        if (nextButton != null)
            nextButton.interactable = true;
    }

    // =========================================================
    // MODULE HANDOFF
    // =========================================================

    void HandOffToNextModule()
    {
        Debug.Log("[HANDOFF] Starting");

        if (cameraMover != null)
        {
            Debug.Log(
                "[HANDOFF] Module 1 CameraMover camAnimator = " +
                (cameraMover.camAnimator != null
                    ? GetHierarchyPath(cameraMover.camAnimator.transform)
                    : "NULL")
            );

            // KEEP the camera fix.
            cameraMover.PrepareForModuleHandoff();
        }

        if (nextModuleRootObject != null)
            nextModuleRootObject.SetActive(true);

        if (ownRootObject != null)
            ownRootObject.SetActive(false);
    }

    private string GetHierarchyPath(Transform t)
    {
        string path = t.name;

        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }

        return path;
    }

    public int CurrentPage => currentPage;
}