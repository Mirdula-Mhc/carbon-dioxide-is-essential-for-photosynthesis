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

    [Header("Camera (optional)")]
    public CameraMover cameraMover;

    [Header("Interaction Managers (optional)")]
    [Tooltip("Assign if this project has button-group pages. Same role as resistanceBox in the reference project.")]
    public ButtonGroupManager buttonGroupManager;

    [Tooltip("Assign if this project has click-to-animate pages (UI or 3D objects that highlight/animate on click).")]
    public ClickAnimManager clickAnimManager;

    [Tooltip("Assign if some objects should only be visible on specific pages. Not a completion gate - just visibility.")]
    public PageVisibilityManager pageVisibilityManager;

    [Header("Auto Complete Pages")]
    [Tooltip("Page indexes with nothing to interact with - Next unlocks immediately on entering these, regardless of any manager above.")]
    public List<int> autoCompletePages;

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
            cameraMover?.MoveNext();
        }
    }

    public void Previous()
    {
        if (interactionLocked) return;
        if (currentPage > 0)
        {
            currentPage--;
            ShowPage(currentPage);
            cameraMover?.MovePrevious();
        }
    }

    void ShowPage(int index)
    {
        for (int i = 0; i < pages.Count; i++)
            pages[i].SetActive(i == index);

        bool allowNext = true;

        buttonGroupManager?.SetPageContext(index);
        clickAnimManager?.SetPageContext(index);
        pageVisibilityManager?.SetPageContext(index);

        if (autoCompletePages.Contains(index))
        {
            // Nothing to interact with on this page - skip every
            // other role check below, Next unlocks immediately.
        }
        else
        {
            // ---------------- BUTTON GROUP ----------------
            if (buttonGroupManager != null && buttonGroupManager.OwnsPage(index) && !completedButtonGroupPages.Contains(index))
                allowNext = false;

            // ---------------- CLICK ANIM ----------------
            if (clickAnimManager != null && clickAnimManager.OwnsPage(index) && !completedClickAnimPages.Contains(index))
                allowNext = false;

            // Add more role checks here as new interaction managers get
            // added, e.g.:
            // if (dragDropManager != null && dragDropManager.OwnsPage(index) && !completedDragDropPages.Contains(index))
            //     allowNext = false;
        }

        nextButton.interactable = allowNext && !interactionLocked;
        prevButton.interactable = index > 0;

        if (pageCounterText != null)
            pageCounterText.text = (index + pageOffset) + " / " + pages.Count;
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
        ShowPage(currentPage);
    }

    public void OnClickAnimDone()
    {
        completedClickAnimPages.Add(currentPage);
        ShowPage(currentPage);
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
    public void LockInteraction()
    {
        interactionLocked = true;
        nextButton.interactable = false;
    }

    public void UnlockInteraction()
    {
        interactionLocked = false;
        ShowPage(currentPage);
    }

    public int CurrentPage => currentPage;
}