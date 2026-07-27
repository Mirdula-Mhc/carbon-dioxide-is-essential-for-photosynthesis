using UnityEngine;
using UnityEngine.Events;

public class DragAndCheckCollision : MonoBehaviour
{
    private Vector3 offset;
    private Camera cam;
    private bool isDragging = false;

    private Vector3 startPosition;
    private Vector3 targetPosition;

    [Header("Target To Check")]
    public Collider targetCollider;

    [Header("Drag Settings")]
    public float liftHeight = 1.5f;        // how much it moves up while dragging
    public float moveSpeed = 10f;          // drag smoothness

    [Header("Return Settings")]
    public float returnSpeed = 5f;         // speed to go back

    [Header("Events")]
    public UnityEvent onCorrectCollision;
    public UnityEvent onWrongCollision;

    private bool isReturning = false;

    void Start()
    {
        cam = Camera.main;
        startPosition = transform.position;
        targetPosition = startPosition;
    }

    void OnMouseDown()
    {
        isDragging = true;
        isReturning = false;

        Vector3 mousePos = GetMouseWorldPos();
        offset = transform.position - mousePos;

        // Apply lift
        transform.position += Vector3.up * liftHeight;
    }

    void Update()
    {
        if (isDragging)
        {
            Vector3 mousePos = GetMouseWorldPos();
            Vector3 desiredPos = mousePos + offset;

            // Keep lifted height
            desiredPos.y = startPosition.y + liftHeight;

            transform.position = Vector3.Lerp(transform.position, desiredPos, moveSpeed * Time.deltaTime);
        }

        // Smooth return after release
        if (isReturning)
        {
            transform.position = Vector3.Lerp(transform.position, startPosition, returnSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, startPosition) < 0.01f)
            {
                transform.position = startPosition;
                isReturning = false;
            }
        }
    }

    void OnMouseUp()
    {
        isDragging = false;
        isReturning = true;
    }

    Vector3 GetMouseWorldPos()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = cam.WorldToScreenPoint(transform.position).z;
        return cam.ScreenToWorldPoint(mousePoint);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == targetCollider)
        {
            Debug.Log("Correct Object Hit!");
            onCorrectCollision?.Invoke();
        }
        else
        {
            Debug.Log("Wrong Object Hit!");
            onWrongCollision?.Invoke();
        }
    }
}
