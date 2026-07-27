using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// -----------------------------------------------------------------
// Page flow manager, same pattern as PotentiometerPageFlowManager:
//
//   - Pages are just indexes into "pages".
//   - A page's "type" is whichever role-list its index appears in
//     (e.g. buttonGroupPages, dragDropPages, mcqPages...).
//   - Independent interaction MANAGERS elsewhere in the scene (like
//     ButtonGroupManager below) own ALL the pages of their type and
//     track per-page state internally. When a page under their care
//     is solved, they call back ONE matching method here, e.g.
//     OnButtonGroupDone(). This script never reaches into them.
//   - ShowPage() re-checks every role-list the current index belongs
//     to; Next stays locked until every relevant role is completed
//     for that page.
//
// To add a new interaction TYPE later (say, drag-drop):
//   1. Add a new List<int> dragDropPages field.
//   2. Add a new HashSet<int> completedDragDropPages field.
//   3. Add one "if (dragDropPages.Contains(index) &&
//      !completedDragDropPages.Contains(index)) allowNext = false;"
//      line in ShowPage().
//   4. Add one public OnDragDropDone() method, same shape as the
//      others below.
//   The core loop (Next/Previous/ShowPage/camera) never changes -
//   only new role lists get added.
// -----------------------------------------------------------------
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
    [Tooltip("Assign if this project has button-group pages. Same role as resistanceBox in the reference project.")]
    public ButtonGroupManager buttonGroupManager;

    [Tooltip("Assign if this project has click-to-animate pages (UI or 3D objects that highlight/animate on click).")]
    public ClickAnimManager clickAnimManager;

    [Tooltip("Assign if some objects should only be visible on specific pages. Not a completion gate - just visibility.")]
    public PageVisibilityManager pageVisibilityManager;

    [Tooltip("Assign if this project has pages that auto-play an animation on enter, blocking Next until it finishes.")]
    public PageEnterAnimManager pageEnterAnimManager;

    [Header("Auto Complete Pages")]
    [Tooltip("Page indexes with nothing to interact with - Next unlocks immediately on entering these, regardless of any manager above.")]
    public List<int> autoCompletePages;

    [Header("End-of-Flow Handoff (optional)")]
    [Tooltip("This entire simulation's own root GameObject (i.e. everything under a single parent). Gets SetActive(false) when Next is pressed on the very last page.")]
    public GameObject ownRootObject;

    [Tooltip("The next module/scene's root GameObject. Gets SetActive(true) at the same moment ownRootObject is disabled. Leave both fields empty if this flow doesn't hand off to anything.")]
    public GameObject nextModuleRootObject;



    int currentPage = 0;
    bool interactionLocked = false;

    // =========================================================
    // COMPLETION TRACKING - one HashSet per interaction manager
    // type. No separate page-index list needed here - each manager
    // (e.g. ButtonGroupManager) already knows which pages it owns
    // via its own OwnsPage(index) check, so PageFlowManager just
    // asks the manager instead of keeping its own duplicate list.
    // =========================================================
    HashSet<int> completedButtonGroupPages = new();
    HashSet<int> completedClickAnimPages = new();
    // HashSet<int> completedDragDropPages = new();
    // HashSet<int> completedMcqPages = new();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        nextButton.onClick.AddListener(Next);
        prevButton.onClick.AddListener(Previous);
        ShowPage(0);
    }

    public void Next()
    {
        if (interactionLocked) return;
        if (currentPage < pages.Count - 1)
        {
            currentPage++;
            ShowPage(currentPage);

            if (cameraMover != null)
            {
                LockInteraction();
                cameraMover.MoveNext(UnlockInteraction);
            }
        }
        else if (nextButton.interactable)
        {
            // Already on the last page and it's fully complete (Next
            // wouldn't be interactable otherwise) - hand off to
            // whatever comes after this whole simulation.
            HandOffToNextModule();
        }
    }

    // Disables this simulation's own root object and enables the next
    // module's root, if both are assigned. Safe to call even if one or
    // both are left empty (e.g. this flow doesn't hand off to anything).
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

    public void Previous()
    {
        if (interactionLocked) return;
        if (currentPage > 0)
        {
            currentPage--;
            ShowPage(currentPage);

            if (cameraMover != null)
            {
                LockInteraction();
                cameraMover.MovePrevious(UnlockInteraction);
            }
        }
    }

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
    void UpdatePageCounter()
    {
        if (pageCounterText == null)
            return;

        int displayedPage = currentPage + pageOffset;

        pageCounterText.text = $"{displayedPage}/{totalPageCount}";
    }
    void RefreshNavigation()
    {
        // During camera movement, both navigation buttons stay locked.
        if (interactionLocked)
        {
            nextButton.interactable = false;
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

        // LOCKED BY DEFAULT.
        bool pageComplete = false;

        // Explicitly configured as a no-interaction page.
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
                "Next remains locked. Add it to Auto Complete Pages or configure an interaction."
            );
        }

        nextButton.interactable = pageComplete;

        // Previous is based on navigation position,
        // not whether the current page has been completed.
        prevButton.interactable = currentPage > 0;

        Debug.Log(
            $"[PageFlow] Page {currentPage} | " +
            $"Complete={pageComplete} | " +
            $"Auto={isAutoComplete} | " +
            $"ButtonGroup={hasButtonGroup} | " +
            $"ClickAnim={hasClickAnim} | " +
            $"EnterAnim={hasPageEnterAnim}"
        );
    }
    // =========================================================
    // EVENTS FROM INTERACTION MANAGERS
    // Each interaction manager calls its matching method here when
    // ITS current page is done. That's the entire integration
    // surface - nothing else to wire.
    // =========================================================
    public void OnButtonGroupDone()
    {
        completedButtonGroupPages.Add(currentPage);

        Debug.Log(
            $"[PageFlow] Button group gate completed for page {currentPage}"
        );

        RefreshNavigation();
    }

    public void OnClickAnimDone()
    {
        completedClickAnimPages.Add(currentPage);

        Debug.Log(
            $"[PageFlow] Click animation gate completed for page {currentPage}"
        );

        RefreshNavigation();
    }

    public void OnPageEnterAnimDone()
    {
        Debug.Log(
            $"[PageFlow] Page-enter animation gate completed for page {currentPage}"
        );

        RefreshNavigation();
    }

    // Add more On___Done() methods here as new interaction types
    // get built, e.g.:
    // public void OnDragDropDone()
    // {
    //     completedDragDropPages.Add(currentPage);
    //     ShowPage(currentPage);
    // }

    // =========================================================
    // Optional external lock (e.g. while an animation plays)
    // =========================================================
    void LockInteraction()
    {
        interactionLocked = true;
        RefreshNavigation();
    }

    void UnlockInteraction()
    {
        interactionLocked = false;
        RefreshNavigation();
    }

    public int CurrentPage => currentPage;


}