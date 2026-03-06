using UnityEngine;

public class UIDead : MonoBehaviour
{
    [SerializeField] private Canvas CanvasDead;
    [SerializeField] private Canvas[] notDeadCanvas;

    public void ShowDeadUI()
    {
        Debug.Log("Showing Dead UI");
        foreach (var c in notDeadCanvas)
        {
            if (c != null)
            {
                c.enabled = false;
            }
        }

        if (CanvasDead != null)
        {
            CanvasDead.enabled = true;
        }
    }
}
