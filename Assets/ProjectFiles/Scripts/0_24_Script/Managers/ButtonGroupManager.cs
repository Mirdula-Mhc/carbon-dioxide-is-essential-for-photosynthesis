using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// -----------------------------------------------------------------
// A MANAGER, same relationship to PageFlowManager as
// ResistanceBoxManager had to PotentiometerPageFlowManager:
//
//   - It owns EVERY button-group page's buttons, not just one page.
//   - PageFlowManager tells it which page is now active via
//     SetPageContext(pageIndex) - called from PageFlowManager.ShowPage()
//     the same way resistanceBox.SetPageContext(index) was called.
//   - It keeps its own per-page state (which buttons were pressed on
//     each page) so revisiting an already-solved page shows it solved.
//   - When a page's buttons are all pressed, it calls
//     PageFlowManager.Instance.OnButtonGroupDone() - the one and only
//     place the two scripts talk to each other.
//
// Setup:
//   1. One ButtonGroupManager in the scene (like the one
//      ResistanceBoxManager instance in the reference project).
//   2. In "pageButtonSets", add one entry PER page that needs a
//      button group, with that page's index and its list of buttons.
//   3. Drag this ButtonGroupManager into PageFlowManager's
//      "Button Group Manager" field. Nothing else to configure on
//      PageFlowManager - it asks this script directly via OwnsPage().
// -----------------------------------------------------------------
public class ButtonGroupManager : MonoBehaviour
{
    [System.Serializable]
    public class PageButtonSet
    {
        public int pageIndex;
        public List<Button> buttons;
    }

    [Header("Per-Page Button Sets")]
    [Tooltip("One entry per page that has a button-group interaction.")]
    public List<PageButtonSet> pageButtonSets;

    class PageState
    {
        public bool locked;
        public HashSet<Button> pressed = new HashSet<Button>();
    }

    Dictionary<int, PageState> pageStates = new();
    int currentPageIndex = -1;

    void Start()
    {
        foreach (var set in pageButtonSets)
        {
            pageStates[set.pageIndex] = new PageState();

            foreach (var b in set.buttons)
            {
                var capturedButton = b;
                var capturedPage = set.pageIndex;
                b.onClick.AddListener(() => OnButtonPressed(capturedPage, capturedButton));
            }
        }
    }

    // Called by PageFlowManager.ShowPage() every time the active
    // page changes - same role as resistanceBox.SetPageContext(index).
    public void SetPageContext(int pageIndex)
    {
        currentPageIndex = pageIndex;

        if (!pageStates.TryGetValue(pageIndex, out var state)) return; // not a button-group page

        var set = pageButtonSets.Find(s => s.pageIndex == pageIndex);
        if (set == null) return;

        // Restore visuals to match saved state (solved or in-progress)
        foreach (var b in set.buttons)
        {
            bool isPressed = state.pressed.Contains(b);
            b.interactable = !state.locked;
            SetPressedVisual(b, isPressed);
        }
    }

    // Lets PageFlowManager check "does this page belong to me" without
    // keeping its own duplicate list of page indexes.
    public bool OwnsPage(int pageIndex)
    {
        return pageStates.ContainsKey(pageIndex);
    }

    void OnButtonPressed(int pageIndex, Button b)
    {
        var state = pageStates[pageIndex];
        if (state.locked) return;
        if (state.pressed.Contains(b)) return; // ignore repeat clicks

        state.pressed.Add(b);
        SetPressedVisual(b, true);

        var set = pageButtonSets.Find(s => s.pageIndex == pageIndex);
        if (state.pressed.Count >= set.buttons.Count)
        {
            state.locked = true;
            foreach (var button in set.buttons)
                button.interactable = false;

            PageFlowManager.Instance.OnButtonGroupDone();
        }
    }

    void SetPressedVisual(Button b, bool pressed)
    {
        // Minimal default visual feedback - swap for your own
        // (color tint, icon swap, etc.) as needed.
        var colors = b.colors;
        colors.normalColor = pressed ? Color.gray : Color.white;
        b.colors = colors;
    }
}