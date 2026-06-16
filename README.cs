using UnityEngine;

public class CanvasWorldCameraWatcher : MonoBehaviour
{
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private bool logEveryChange = true;

    private Camera lastCamera;

    private void Awake()
    {
        ResolveReferences();
        lastCamera = targetCanvas == null ? null : targetCanvas.worldCamera;
    }

    private void LateUpdate()
    {
        ResolveReferences();

        if (targetCanvas == null)
        {
            return;
        }

        if (targetCanvas.worldCamera == lastCamera)
        {
            return;
        }

        if (logEveryChange)
        {
            string oldName = lastCamera == null ? "None" : lastCamera.name;
            string newName = targetCanvas.worldCamera == null ? "None" : targetCanvas.worldCamera.name;

            Debug.Log("[CanvasCameraWatcher] EventCamera changed: " + oldName + " -> " + newName);
        }

        lastCamera = targetCanvas.worldCamera;
    }

    private void ResolveReferences()
    {
        if (targetCanvas == null)
        {
            targetCanvas = FindFirstObjectByType<Canvas>();
        }
    }
}
