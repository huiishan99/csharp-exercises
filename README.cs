using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class DisplayCameraViewFitter : MonoBehaviour
{
    [SerializeField] private RectTransform targetRect;
    [SerializeField] private float cameraDistance = 10f;

    [Header("Direction")]
    [SerializeField] private bool invertForward = false;

    [Header("Update")]
    [SerializeField] private bool fitOnLateUpdate = true;

    private Camera targetCamera;
    private readonly Vector3[] worldCorners = new Vector3[4];

    private void Awake()
    {
        ResolveReferences();
        FitNow();
    }

    private void OnEnable()
    {
        ResolveReferences();
        FitNow();
    }

    private void LateUpdate()
    {
        if (!fitOnLateUpdate)
        {
            return;
        }

        FitNow();
    }

    public void SetTargetRect(RectTransform nextTarget)
    {
        targetRect = nextTarget;
        FitNow();
    }

    public void FitNow()
    {
        ResolveReferences();

        if (targetCamera == null || targetRect == null)
        {
            return;
        }

        targetRect.GetWorldCorners(worldCorners);

        Vector3 bottomLeft = worldCorners[0];
        Vector3 topLeft = worldCorners[1];
        Vector3 topRight = worldCorners[2];
        Vector3 bottomRight = worldCorners[3];

        Vector3 center = (bottomLeft + topRight) * 0.5f;

        float width = Vector3.Distance(bottomLeft, bottomRight);
        float height = Vector3.Distance(bottomLeft, topLeft);

        if (width <= 0.001f || height <= 0.001f)
        {
            return;
        }

        Vector3 lookDirection = invertForward
            ? -targetRect.forward
            : targetRect.forward;

        transform.position = center - lookDirection.normalized * cameraDistance;
        transform.rotation = Quaternion.LookRotation(lookDirection, targetRect.up);

        targetCamera.orthographic = true;

        float cameraAspect = targetCamera.aspect;

        if (cameraAspect <= 0.001f)
        {
            cameraAspect = width / height;
        }

        float targetAspect = width / height;

        if (cameraAspect >= targetAspect)
        {
            targetCamera.orthographicSize = height * 0.5f;
        }
        else
        {
            targetCamera.orthographicSize = width / (2f * cameraAspect);
        }
    }

    private void ResolveReferences()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }
    }
}
