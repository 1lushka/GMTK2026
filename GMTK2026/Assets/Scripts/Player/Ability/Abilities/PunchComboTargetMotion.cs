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

    public void ApplyImpulse(Vector3 direction, float distance, float duration, float returnDuration, Ease ease)
    {
        if (!hasGroundY)
        {
            groundY = transform.position.y;
            hasGroundY = true;
        }

        direction.y = 0f;
        direction = direction.sqrMagnitude > 0f ? direction.normalized : transform.forward;

        Vector3 destination = transform.position + direction * distance;
        destination.y = groundY;

        KillMovementTween();
        Sequence sequence = DOTween.Sequence().SetLink(gameObject);
        sequence.Join(transform.DOMoveX(destination.x, duration).SetEase(ease));
        sequence.Join(transform.DOMoveZ(destination.z, duration).SetEase(ease));
        sequence.Join(transform.DOMoveY(groundY, returnDuration).SetEase(ease));
        sequence.OnComplete(ForgetGroundHeight);
        movementTween = sequence;
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
