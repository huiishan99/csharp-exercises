using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class DisplayCameraViewFitter : MonoBehaviour
{
    [SerializeField] private RectTransform targetRect;
    [SerializeField] private float cameraDistance = 10f;

    [Header("Direction")]
    [SerializeField] private bool invertForward = false;

    [Header("Output Rotation")]
    [SerializeField] private bool rotateOutput180 = false;

    [Tooltip("0 / 90 / 180 / 270 only. Normally use 0 or 180.")]
    [SerializeField] private int outputRotationDegrees = 0;

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

        Quaternion baseRotation = Quaternion.LookRotation(lookDirection, targetRect.up);
        float roll = GetNormalizedOutputRotation();
        transform.rotation = baseRotation * Quaternion.Euler(0f, 0f, roll);

        targetCamera.orthographic = true;

        float cameraAspect = targetCamera.aspect;

        if (cameraAspect <= 0.001f)
        {
            cameraAspect = width / height;
        }

        float fitWidth = width;
        float fitHeight = height;

        if (IsRightAngleRotation(roll))
        {
            fitWidth = height;
            fitHeight = width;
        }

        float targetAspect = fitWidth / fitHeight;

        if (cameraAspect >= targetAspect)
        {
            targetCamera.orthographicSize = fitHeight * 0.5f;
        }
        else
        {
            targetCamera.orthographicSize = fitWidth / (2f * cameraAspect);
        }
    }

    private float GetNormalizedOutputRotation()
    {
        if (rotateOutput180)
        {
            return 180f;
        }

        int normalized = outputRotationDegrees % 360;

        if (normalized < 0)
        {
            normalized += 360;
        }

        if (normalized == 90 || normalized == 180 || normalized == 270)
        {
            return normalized;
        }

        return 0f;
    }

    private bool IsRightAngleRotation(float rotation)
    {
        return Mathf.Approximately(rotation, 90f)
            || Mathf.Approximately(rotation, 270f);
    }

    private void ResolveReferences()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }
    }
}
