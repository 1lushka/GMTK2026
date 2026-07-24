using System.Collections;
using UnityEngine;

public class DashAbility : ActiveAbility
{
    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float windUpTime = 0.2f;   // задержка перед рывком

    [Header("Animation")]
    [SerializeField] private string dashTrigger = "Dash";
    [SerializeField] private bool useAnimator = true;

    [Header("Particles")]
    [SerializeField] private ParticleSystem dashParticle;

    private Animator animator;

    protected override void Start()
    {
        base.Start();

        if (useAnimator && controller != null)
        {
            animator = controller.Animator;
            if (animator == null)
                Debug.LogWarning("DashAbility: Animator not found in TopDownController!");
        }
    }

    protected override void Activate()
    {
        StartCoroutine(DashRoutine());
    }

    private IEnumerator DashRoutine()
    {
        // 1. Анимация подготовки (проигрывается мгновенно, триггер)
        if (animator != null)
            animator.SetTrigger(dashTrigger);

        // 2. Ждём подготовительное время (анимация подготовки успевает проиграться)
        yield return new WaitForSeconds(windUpTime);

        // 3. Запускаем партиклы
        if (dashParticle != null)
        {
            dashParticle.Clear();
            dashParticle.Play();
        }

        // 4. Сам рывок
        Vector3 dashDirection = GetDashDirection();
        controller.enableMovement = false;
        float elapsed = 0f;

        while (elapsed < dashDuration)
        {
            Vector3 newPos = rb.position + dashDirection * dashSpeed * Time.fixedDeltaTime;
            rb.MovePosition(newPos);
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        controller.SetVelocity(Vector3.zero);
        controller.enableMovement = true;
    }

    private Vector3 GetDashDirection()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(h, 0, v).normalized;

        if (inputDir.magnitude > 0.1f)
            return inputDir;
        else
            return transform.forward;
    }
}