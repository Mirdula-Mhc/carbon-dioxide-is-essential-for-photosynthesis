using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// -----------------------------------------------------------------
// ONLY job: watch a set of buttons for THIS page, and lock/unlock
// Next on PageFlowManager accordingly. Nothing else - no knowledge
// of other pages, cameras, or navigation.
//
// Setup:
//   1. Put this on a child object under the page that has buttons
//      (so it activates/deactivates along with the page automatically).
//   2. Drag that page's buttons into "buttons".
//   3. Set "requiredCount" to however many presses you want to
//      require (usually leave at 0, which defaults to buttons.Count).
//   4. Do NOT also add this page to ButtonGroupManager - use one
//      system or the other per page, not both.
// -----------------------------------------------------------------
public class PageButtonGroup : MonoBehaviour
{
    public List<Button> buttons = new List<Button>();

    [Tooltip("How many distinct button presses are required. Leave at 0 to require all buttons.")]
    public int requiredCount = 0;

    HashSet<Button> pressed = new HashSet<Button>();
    bool wired = false;

    void Awake()
    {
        if (wired) return;
        wired = true;

        foreach (var b in buttons)
        {
            if (b == null) continue;
            var captured = b;
            captured.onClick.AddListener(() => OnButtonPressed(captured));
        }
    }

    void OnEnable()
    {
        // Fresh start each time this page is (re)entered.
        pressed.Clear();
        foreach (var b in buttons)
            if (b != null) b.interactable = true;

        PageFlowManager.Instance.LockInteraction();
    }

    void OnButtonPressed(Button b)
    {
        if (pressed.Contains(b)) return; // ignore repeat clicks on the same button

        pressed.Add(b);

        int target = requiredCount > 0 ? requiredCount : buttons.Count;
        if (pressed.Count >= target)
        {
            PageFlowManager.Instance.UnlockInteraction();
        }
    }
}