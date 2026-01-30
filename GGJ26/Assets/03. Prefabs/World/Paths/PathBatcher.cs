using UnityEngine;
using System.Collections.Generic;

public class PathBatcher : MonoBehaviour
{
    [Header("Generation Settings")]
    public GameObject objectPrefab;
    public int numberOfObjects = 8;

    [Header("Circular Arrangement Settings")]
    public Vector3 center = Vector3.zero;
    public float radius = 5f;
    [Range(0, 360)]
    public float angleOffset = 0f;
    public Vector3 positionOffset = Vector3.zero;

    private List<GameObject> _createdObjects = new List<GameObject>();

    [ContextMenu("Generate Circle")]
    public void GenerateCircle()
    {
        if (objectPrefab == null)
        {
            Debug.LogWarning("Object Prefab is not assigned.");
            return;
        }

        ClearGeneratedObjects();

        float angleStep = 360f / numberOfObjects;

        for (int i = 0; i < numberOfObjects; i++)
        {
            // Calculate position and rotation
            float currentAngle = (i * angleStep + angleOffset) * Mathf.Deg2Rad;

            Vector3 newPos = new Vector3(
                center.x + radius * Mathf.Cos(currentAngle),
                center.y,
                center.z + radius * Mathf.Sin(currentAngle)
            );
            newPos += positionOffset;

            Quaternion rotation = Quaternion.LookRotation(center - newPos);

            // Instantiate and apply transformations
            GameObject newObject = Instantiate(objectPrefab, newPos, rotation);
            newObject.transform.SetParent(transform); // Optional: Keep hierarchy clean
            _createdObjects.Add(newObject);
        }
    }

    [ContextMenu("Clear Generated Objects")]
    public void ClearGeneratedObjects()
    {
        // Using a backward loop is safer when removing items from a list
        for (int i = _createdObjects.Count - 1; i >= 0; i--)
        {
            if (_createdObjects[i] != null)
            {
                // Use DestroyImmediate in editor scripts, Destroy in runtime
                if (Application.isPlaying)
                {
                    Destroy(_createdObjects[i]);
                }
                else
                {
                    DestroyImmediate(_createdObjects[i]);
                }
            }
        }
        _createdObjects.Clear();
    }
}

