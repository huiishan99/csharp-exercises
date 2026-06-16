using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class UiLocalGraphicRaycastUtility
{
    public static bool TryRaycastTopGraphic(
        Canvas targetCanvas,
        RectTransform referenceRect,
        Vector2 referenceLocalPoint,
        Vector2 screenPoint,
        List<Graphic> graphicBuffer,
        out RaycastResult raycastResult
    )
    {
        raycastResult = new RaycastResult();

        if (targetCanvas == null || referenceRect == null || graphicBuffer == null)
        {
            return false;
        }

        GraphicRaycaster raycaster = targetCanvas.GetComponent<GraphicRaycaster>();

        graphicBuffer.Clear();
        targetCanvas.GetComponentsInChildren(false, graphicBuffer);

        Vector3 worldPoint = referenceRect.TransformPoint(
            new Vector3(referenceLocalPoint.x, referenceLocalPoint.y, 0f)
        );

        Graphic topAnyGraphic = null;
        int topAnyDepth = int.MinValue;

        Graphic topInteractiveGraphic = null;
        GameObject topInteractiveHandler = null;
        int topInteractiveDepth = int.MinValue;

        for (int i = 0; i < graphicBuffer.Count; i++)
        {
            Graphic graphic = graphicBuffer[i];

            if (!IsGraphicCandidate(graphic))
            {
                continue;
            }

            RectTransform rectTransform = graphic.rectTransform;

            if (rectTransform == null)
            {
                continue;
            }

            Vector3 localPoint = rectTransform.InverseTransformPoint(worldPoint);

            if (!rectTransform.rect.Contains(localPoint))
            {
                continue;
            }

            if (!IsAllowedByCanvasGroups(graphic.transform))
            {
                continue;
            }

            if (graphic.depth > topAnyDepth)
            {
                topAnyDepth = graphic.depth;
                topAnyGraphic = graphic;
            }

            GameObject handlerObject = FindInteractiveHandler(graphic.gameObject);

            if (handlerObject != null && graphic.depth > topInteractiveDepth)
            {
                topInteractiveDepth = graphic.depth;
                topInteractiveGraphic = graphic;
                topInteractiveHandler = handlerObject;
            }
        }

        if (topInteractiveHandler != null)
        {
            raycastResult = CreateRaycastResult(
                topInteractiveHandler,
                raycaster,
                screenPoint,
                topInteractiveDepth,
                targetCanvas
            );

            Debug.Log(
                "[UI Local Raycast] mode=Interactive"
                + " graphic="
                + GetHierarchyPath(topInteractiveGraphic.gameObject)
                + " handler="
                + GetHierarchyPath(topInteractiveHandler)
                + " depth="
                + topInteractiveDepth
                + GetSelectableStateText(topInteractiveHandler)
            );

            return true;
        }

        if (topAnyGraphic != null)
        {
            raycastResult = CreateRaycastResult(
                topAnyGraphic.gameObject,
                raycaster,
                screenPoint,
                topAnyDepth,
                targetCanvas
            );

            Debug.Log(
                "[UI Local Raycast] mode=GraphicOnly"
                + " graphic="
                + GetHierarchyPath(topAnyGraphic.gameObject)
                + " handler=None"
                + " depth="
                + topAnyDepth
            );

            return true;
        }

        Debug.Log("[UI Local Raycast] mode=None");
        return false;
    }

    private static bool IsGraphicCandidate(Graphic graphic)
    {
        if (graphic == null)
        {
            return false;
        }

        if (!graphic.gameObject.activeInHierarchy)
        {
            return false;
        }

        if (!graphic.enabled)
        {
            return false;
        }

        if (!graphic.raycastTarget)
        {
            return false;
        }

        if (graphic.canvasRenderer == null || graphic.canvasRenderer.cull)
        {
            return false;
        }

        return true;
    }

    private static RaycastResult CreateRaycastResult(
        GameObject target,
        BaseRaycaster raycaster,
        Vector2 screenPoint,
        int depth,
        Canvas canvas
    )
    {
        return new RaycastResult
        {
            gameObject = target,
            module = raycaster,
            screenPosition = screenPoint,
            depth = depth,
            sortingLayer = canvas == null ? 0 : canvas.sortingLayerID,
            sortingOrder = canvas == null ? 0 : canvas.sortingOrder
        };
    }

    private static GameObject FindInteractiveHandler(GameObject startObject)
    {
        if (startObject == null)
        {
            return null;
        }

        GameObject clickHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(startObject);

        if (clickHandler != null)
        {
            return clickHandler;
        }

        GameObject downHandler = ExecuteEvents.GetEventHandler<IPointerDownHandler>(startObject);

        if (downHandler != null)
        {
            return downHandler;
        }

        GameObject dragHandler = ExecuteEvents.GetEventHandler<IDragHandler>(startObject);

        if (dragHandler != null)
        {
            return dragHandler;
        }

        GameObject beginDragHandler = ExecuteEvents.GetEventHandler<IBeginDragHandler>(startObject);

        if (beginDragHandler != null)
        {
            return beginDragHandler;
        }

        GameObject endDragHandler = ExecuteEvents.GetEventHandler<IEndDragHandler>(startObject);

        if (endDragHandler != null)
        {
            return endDragHandler;
        }

        return null;
    }

    private static bool IsAllowedByCanvasGroups(Transform target)
    {
        Transform current = target;

        while (current != null)
        {
            CanvasGroup[] groups = current.GetComponents<CanvasGroup>();

            for (int i = 0; i < groups.Length; i++)
            {
                CanvasGroup group = groups[i];

                if (group == null)
                {
                    continue;
                }

                if (!group.blocksRaycasts)
                {
                    return false;
                }

                if (group.ignoreParentGroups)
                {
                    return true;
                }
            }

            current = current.parent;
        }

        return true;
    }

    private static string GetSelectableStateText(GameObject target)
    {
        if (target == null)
        {
            return "";
        }

        Selectable selectable = target.GetComponent<Selectable>();

        if (selectable == null)
        {
            return "";
        }

        return " selectableInteractable=" + selectable.interactable;
    }

    private static string GetHierarchyPath(GameObject target)
    {
        if (target == null)
        {
            return "None";
        }

        string path = target.name;
        Transform current = target.transform.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}
