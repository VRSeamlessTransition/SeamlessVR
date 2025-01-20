using TMPro;
using UnityEngine;

public enum Answer
{
    Correct, Incorrect, Pending
}

public struct Dimensions
{
    public Vector3 topLeft, topRight, bottomRight;

    public Dimensions doubled()
    {
        Vector3 centerPoint = (topLeft + bottomRight) / 2f;

        return new Dimensions()
        {
            topLeft = 2 * this.topLeft - centerPoint, // tl2 = cp + 2 (tl - cp)
            topRight = 2 * this.topRight - centerPoint,
            bottomRight = 2 * this.bottomRight - centerPoint,
        };
    }

    public readonly Vector3  bottomLeft
    {
        get
        {
            return topLeft + (bottomRight - topRight);
        }

    }
}
public interface ITaskInterface
{
    //public void SetTaskText(TMP_Text textMeshProText);
    public void DestroyObjects();
    public void HideObjects();
    public bool TaskStarted();
    public void SetSpawned(bool spawned);
    public void StartTask(ScreenManager screenManager, int conditionIndex, int trial_idx, bool isFinalTask = false);  // only shell game needs
    public void SetTaskText(TMP_Text text, int currConditionIndex, int currTrialIndex);
    public void ActionBeforeTranstion() { }
    public void SetVEObjectBounds(Bounds bounds);
    public Bounds GetVEObjectBounds();

    //todo need a way of verifying answer provided, either by detecting mouse click
    // in case of counting objects of a shape, will need some input method

    // public Answer GetAnswer();
}
