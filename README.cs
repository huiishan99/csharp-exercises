[Header("Operation Lock")]
[SerializeField] private bool allowDragScroll = false;
[SerializeField] private bool allowCardClickMove = true;

public void OnCardClicked(int slotOffset)
{
    if (!allowCardClickMove)
    {
        return;
    }

    if (slotOffset == 0)
    {
        return;
    }

    if (Mathf.Abs(slotOffset) > clickableDistance)
    {
        return;
    }

    MoveByStep(slotOffset);
}

public void OnBeginDrag(PointerEventData eventData)
{
    if (!allowDragScroll)
    {
        return;
    }

    if (!CanOperate())
    {
        return;
    }

    StopAnimation();

    isDragging = true;
    dragStartPosition = eventData.position;
}

public void OnEndDrag(PointerEventData eventData)
{
    if (!allowDragScroll)
    {
        isDragging = false;
        rawOffset = 0f;
        return;
    }

    if (!isDragging)
    {
        return;
    }

    isDragging = false;

    int step = -Mathf.RoundToInt(rawOffset);

    if (Mathf.Abs(rawOffset) < minSwipeStep || step == 0)
    {
        AnimateBack(rawOffset);
        return;
    }

    step = Mathf.Clamp(step, -maxCommitStep, maxCommitStep);
    MoveByStep(step, rawOffset);
}
