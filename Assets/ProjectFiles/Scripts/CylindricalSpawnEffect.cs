using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace HeatingSolutionsInaTestTube
{
    public class CylindricalSpawnEffect : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private GameObject prefab;
        [SerializeField] private float radius = 1.5f;
        [SerializeField] private float height = 2f;
        
        [Header("Auto Start")]
        [SerializeField] private bool autoStartOnEnable = false;
    
        [Header("Bubble Size")]
        [SerializeField] private float minScale = 0.2f;
        [SerializeField] private float maxScale = 0.6f;
    
        [Header("Movement")]
        [SerializeField] private float moveDuration = 2f;
    
        [Header("Timing")]
        [SerializeField] private float spawnInterval = 0.1f;
    
        [Header("Trigger Delay")]
        [SerializeField] private float triggerDelay = 2f;
    
        [Header("Delayed Objects")]
        [SerializeField] private GameObject[] delayedObjects;
        [SerializeField] private float delayedObjectsEnableTime = 2f;
    
        private Coroutine spawnRoutine;
        private Coroutine delayedRoutine;
    
        private readonly List<GameObject> spawnedObjects = new();
    
        private bool isRunning = false;
    
        // =========================
        // UNITY EVENTS
        // =========================
    
        private void OnEnable()
        {
            if (autoStartOnEnable)
            {
                isRunning = true;
                StartCoroutine(StartWithDelay());
                return;
            }
    
            if (isRunning)
            {
                // Remove only frozen bubbles
                RemoveStaticBubbles();
    
                spawnRoutine = StartCoroutine(SpawnRoutine());
    
                if (delayedObjects != null && delayedObjects.Length > 0)
                    delayedRoutine = StartCoroutine(EnableDelayedObjects());
            }
        }
    
    
        private void OnDisable()
        {
            if (spawnRoutine != null)
                StopCoroutine(spawnRoutine);
    
            if (delayedRoutine != null)
                StopCoroutine(delayedRoutine);
        }
    
        // =========================
        // PUBLIC API WITH DELAY
        // =========================
    
        public void TriggerEffect()
        {
            if (isRunning) return;
    
            isRunning = true;
    
            StartCoroutine(StartWithDelay());
        }
    
        private IEnumerator StartWithDelay()
        {
            yield return new WaitForSeconds(triggerDelay);
    
            spawnRoutine = StartCoroutine(SpawnRoutine());
    
            if (delayedObjects != null && delayedObjects.Length > 0)
                delayedRoutine = StartCoroutine(EnableDelayedObjects());
        }
    
        public void StopEffect()
        {
            isRunning = false;
    
            if (spawnRoutine != null)
                StopCoroutine(spawnRoutine);
    
            if (delayedRoutine != null)
                StopCoroutine(delayedRoutine);
        }
    
        // =========================
        // SPAWN ROUTINE
        // =========================
    
        private IEnumerator SpawnRoutine()
        {
            while (isRunning)
            {
                SpawnOne();
                yield return new WaitForSeconds(spawnInterval);
            }
        }
    
        private void SpawnOne()
        {
            Vector3 offset = GetRandomCirclePoint();
    
            Vector3 start = transform.position + offset;
            Vector3 end = start + transform.up * height;
    
            GameObject bubble =
                Instantiate(prefab, start, Quaternion.identity, transform);
    
            float scale = Random.Range(minScale, maxScale);
            bubble.transform.localScale = Vector3.one * scale;
    
            spawnedObjects.Add(bubble);
    
            StartCoroutine(MoveBubble(bubble, start, end));
        }
    
        private Vector3 GetRandomCirclePoint()
        {
            float r = Mathf.Sqrt(Random.Range(0f, 1f)) * radius;
            float angle = Random.Range(0f, Mathf.PI * 2f);
    
            float x = r * Mathf.Cos(angle);
            float z = r * Mathf.Sin(angle);
    
            return transform.right * x + transform.forward * z;
        }
    
        // =========================
        // MOVE BUBBLE
        // =========================
    
        private IEnumerator MoveBubble(GameObject obj, Vector3 start, Vector3 end)
        {
            float elapsed = 0f;
    
            while (elapsed < moveDuration)
            {
                if (obj == null)
                    yield break;
    
                elapsed += Time.deltaTime;
    
                obj.transform.position =
                    Vector3.Lerp(start, end, elapsed / moveDuration);
    
                yield return null;
            }
    
            spawnedObjects.Remove(obj);
    
            if (obj != null)
                Destroy(obj);
        }
    
        // =========================
        // REMOVE ONLY STATIC BUBBLES
        // =========================
    
        private void RemoveStaticBubbles()
        {
            for (int i = spawnedObjects.Count - 1; i >= 0; i--)
            {
                GameObject obj = spawnedObjects[i];
    
                if (obj == null)
                {
                    spawnedObjects.RemoveAt(i);
                    continue;
                }
    
                // If object hasn't moved upward enough, consider it frozen
                float heightFromBase =
                    Vector3.Dot(
                        obj.transform.position - transform.position,
                        transform.up);
    
                if (heightFromBase < height * 0.95f)
                {
                    Destroy(obj);
                    spawnedObjects.RemoveAt(i);
                }
            }
        }
    
        // =========================
        // DELAYED OBJECTS
        // =========================
    
        private IEnumerator EnableDelayedObjects()
        {
            yield return new WaitForSeconds(delayedObjectsEnableTime);
    
            foreach (var obj in delayedObjects)
            {
                if (obj != null)
                    obj.SetActive(true);
            }
        }
    
    #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
    
            Vector3 bottom = transform.position;
            Vector3 top = transform.position + transform.up * height;
    
            DrawCircle(bottom);
            DrawCircle(top);
        }
    
        private void DrawCircle(Vector3 center)
        {
            int segments = 24;
    
            Vector3 prev =
                center + transform.right * radius;
    
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
    
                Vector3 next =
                    center +
                    (transform.right * Mathf.Cos(angle) +
                     transform.forward * Mathf.Sin(angle)) * radius;
    
                Gizmos.DrawLine(prev, next);
    
                prev = next;
            }
        }
    #endif
    }
    
    
}