using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class DragAndCheckCollision : MonoBehaviour
{
    private Camera cam;
    private bool isDragging;
    private bool isReturning;
    private bool collisionTriggered;

    private Vector3 startPosition;
    private Vector3 offset;

    private Collider myCollider;

    [Header("Target To Check")]
    public Collider targetCollider;

    [Header("Object Management Arrays")]
    [Tooltip("Objects that turn ON while you are actively dragging this object.")]
    public GameObject[] objectsToEnableWhileDragging;

    [Tooltip("Objects that turn OFF permanently when a correct snap/collision happens.")]
    public GameObject[] objectsToDeactivateOnCorrect;

    [Header("Return Settings")]
    public float returnSpeed = 5f;

    [Header("Events")]
    public UnityEvent onCorrectCollision;
    public UnityEvent onWrongCollision;

    private void Start()
    {
        cam = Camera.main;
        startPosition = transform.position;
        myCollider = GetComponent<Collider>();

        if (targetCollider == null)
        {
            Debug.LogError("Target Collider is not assigned!");
        }

        // Ensure drag visuals start turned off
        ToggleArrayObjects(objectsToEnableWhileDragging, false);
    }

    private void OnMouseDown()
    {
        // Capture initial position on press so active animations won't break return target
        startPosition = transform.position;

        isDragging = true;
        isReturning = false;
        collisionTriggered = false;

        Vector3 mousePos = GetMouseWorldPos();
        offset = transform.position - mousePos;

        // Turn ON the dragging objects/visuals
        ToggleArrayObjects(objectsToEnableWhileDragging, true);
    }

    private void Update()
    {
        if (isDragging)
        {
            Vector3 mousePos = GetMouseWorldPos();
            transform.position = mousePos + offset;

            if (!collisionTriggered &&
                targetCollider != null &&
                myCollider.bounds.Intersects(targetCollider.bounds))
            {
                collisionTriggered = true;

                Debug.Log("✅ Correct Collision Detected");

                // Correct Snap: Turn OFF the dragging objects AND the completion objects
                ToggleArrayObjects(objectsToEnableWhileDragging, false);
                ToggleArrayObjects(objectsToDeactivateOnCorrect, false);

                onCorrectCollision?.Invoke();

                isDragging = false;
                isReturning = false;
            }
        }

        if (isReturning)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                startPosition,
                returnSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, startPosition) < 0.01f)
            {
                transform.position = startPosition;
                isReturning = false;
            }
        }
    }

    private void OnMouseUp()
    {
        if (!collisionTriggered)
        {
            isDragging = false;
            isReturning = true;

            // If let go incorrectly, turn the dragging visuals back OFF
            ToggleArrayObjects(objectsToEnableWhileDragging, false);

            if (targetCollider != null &&
                !myCollider.bounds.Intersects(targetCollider.bounds))
            {
                Debug.Log("❌ Wrong Collision");
                onWrongCollision?.Invoke();
            }
        }
    }

    private Vector3 GetMouseWorldPos()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(-cam.transform.forward, startPosition);

        if (plane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }

        return transform.position;
    }

    // Helper method to easily enable/disable groups of arrays safely
    private void ToggleArrayObjects(GameObject[] array, bool state)
    {
        if (array == null) return;

        foreach (GameObject obj in array)
        {
            if (obj != null)
            {
                obj.SetActive(state);
            }
        }
    }
}