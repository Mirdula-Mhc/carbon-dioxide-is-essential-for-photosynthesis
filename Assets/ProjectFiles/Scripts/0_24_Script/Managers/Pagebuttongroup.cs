using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// -----------------------------------------------------------------
// ONLY job: watch a set of buttons for THIS page, and call
// PageFlowManager.Instance.ReportProgress() once per distinct
// button pressed. Nothing else - no knowledge of pages, gates,
// cameras, or navigation.
//
// Setup:
//   1. Put this on a child object under the page that has buttons
//      (so it activates/deactivates along with the page automatically).
//   2. Drag that page's buttons into "buttons".
//   3. On PageFlowManager's Page entry for this page: autoComplete
//      OFF, requiredCount = however many presses you want to require
//      (usually buttons.Count, but can be fewer).
// -----------------------------------------------------------------
public class PageButtonGroup : MonoBehaviour
{
    public List<Button> buttons = new List<Button>();

    private HashSet<Button> pressed = new HashSet<Button>();

    void Awake()
    {
        foreach (var b in buttons)
        {
            var captured = b;
            captured.onClick.AddListener(() => OnButtonPressed(captured));
        }
    }

    void OnEnable()
    {
        // fresh start each time this page is (re)entered
        pressed.Clear();
        foreach (var b in buttons)
            if (b != null) b.interactable = true;
    }

    void OnButtonPressed(Button b)
    {
        if (pressed.Contains(b)) return; // ignore repeat clicks on the same button

        pressed.Add(b);
        PageFlowManager.Instance.ReportProgress();
    }
}