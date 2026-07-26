using DG.Tweening;
using UnityEngine;

public class PunchComboTargetMotion : MonoBehaviour
{
    private float groundY;
    private bool hasGroundY;
    private Tween movementTween;

    public void Lift(float height, float duration, Ease ease)
    {
        if (!hasGroundY)
        {
            groundY = transform.position.y;
            hasGroundY = true;
        }

        KillMovementTween();
        movementTween = transform.DOMoveY(groundY + height, duration)
            .SetEase(ease)
            .SetLink(gameObject);
    }

    public void ReturnToGround(float duration, Ease ease)
    {
        if (!hasGroundY) return;

        KillMovementTween();
        movementTween = transform.DOMoveY(groundY, duration)
            .SetEase(ease)
            .SetLink(gameObject)
            .OnComplete(ForgetGroundHeight);
    }

    private void KillMovementTween()
    {
        if (movementTween != null && movementTween.IsActive())
            movementTween.Kill();
    }

    private void ForgetGroundHeight()
    {
        hasGroundY = false;
        movementTween = null;
    }

    private void OnDestroy()
    {
        KillMovementTween();
    }
}
