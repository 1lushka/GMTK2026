using DG.Tweening;
using UnityEngine;

public class PunchComboTargetMotion : MonoBehaviour
{
    private float groundY;
    private bool hasGroundY;
    private bool originalUseGravity;
    private Rigidbody body;
    private Tween movementTween;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    public void Lift(float height, float duration, Ease ease, float hoverAmplitude, float hoverPeriod)
    {
        if (!hasGroundY)
        {
            groundY = transform.position.y;
            hasGroundY = true;
            if (body != null)
            {
                originalUseGravity = body.useGravity;
                body.useGravity = false;
            }
        }

        KillMovementTween();
        StopVerticalVelocity();
        movementTween = CreateMoveTween(groundY + height, duration)
            .SetEase(ease)
            .SetLink(gameObject)
            .OnComplete(() => BeginHover(height, hoverAmplitude, hoverPeriod));
    }

    public void ReturnToGround(float duration, Ease ease)
    {
        if (!hasGroundY) return;

        KillMovementTween();
        StopVerticalVelocity();
        movementTween = CreateMoveTween(groundY, duration)
            .SetEase(ease)
            .SetLink(gameObject)
            .OnComplete(ForgetGroundHeight);
    }

    public void ReleaseForImpulse()
    {
        if (!hasGroundY) return;

        KillMovementTween();
        ForgetGroundHeight();
    }

    private void BeginHover(float height, float amplitude, float period)
    {
        float centerY = groundY + height;
        float phase = 0f;
        movementTween = DOTween.To(() => phase, value =>
            {
                phase = value;
                SetHeight(centerY + Mathf.Sin(value) * amplitude);
            }, Mathf.PI * 2f, Mathf.Max(0.01f, period))
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart)
            .SetLink(gameObject);
    }

    private Tween CreateMoveTween(float y, float duration)
    {
        return body != null ? body.DOMoveY(y, duration) : transform.DOMoveY(y, duration);
    }

    private void SetHeight(float y)
    {
        Vector3 position = body != null ? body.position : transform.position;
        position.y = y;
        if (body != null)
            body.MovePosition(position);
        else
            transform.position = position;
    }

    private void StopVerticalVelocity()
    {
        if (body == null) return;

        Vector3 velocity = body.linearVelocity;
        velocity.y = 0f;
        body.linearVelocity = velocity;
    }

    private void KillMovementTween()
    {
        if (movementTween != null && movementTween.IsActive())
            movementTween.Kill();
    }

    private void ForgetGroundHeight()
    {
        if (body != null)
            body.useGravity = originalUseGravity;
        hasGroundY = false;
        movementTween = null;
    }

    private void OnDestroy()
    {
        KillMovementTween();
    }
}
