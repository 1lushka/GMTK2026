using UnityEngine;

public class PunchComboAbility : ActiveAbility
{
    private Animator animator;

    [Header("Animation")]
    [SerializeField] private string comboBool = "Combo";
    [SerializeField] private bool useAnimator = true;
    [SerializeField]
    private float rotationSpeed = 720f;

    private bool isPunching = false;
    [SerializeField] private float pushForce = 10f;
    [SerializeField] Transform punchCollider;

    private PunchComboTrigger punchTrigger;


    protected override void Activate()
    {
        if (!CanActivate()) return;
        cooldownTimer=cooldown;
        animator.SetBool(comboBool, true);
        isPunching = true;
        controller.ActivateRotation(false);
        punchTrigger.BeginAttack();
        punchCollider.gameObject.SetActive(true);
        Vector3 direction = LookAtMouse();
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        rb.MoveRotation(targetRotation);
    }

    protected void Deactivate()
    {
        if (!isPunching) return;

        animator.SetBool(comboBool, false);
        isPunching = false;
        controller.ActivateRotation(true);
        Vector3 attackDirection = punchCollider.position - transform.position;
        attackDirection.y = 0f;
        punchTrigger.FinalHit(attackDirection);
        punchCollider.gameObject.SetActive(false);

    }


    protected override void Start()
    {
       
        base.Start();

        if (punchCollider == null)
        {
            Debug.LogError("PunchComboAbility: punchCollider is not assigned!");
            enabled = false;
            return;
        }

        punchTrigger = punchCollider.GetComponent<PunchComboTrigger>();
        if (punchTrigger == null)
            punchTrigger = punchCollider.gameObject.AddComponent<PunchComboTrigger>();
        punchCollider.gameObject.SetActive(false);

        if (useAnimator && controller != null)
        {
            animator = controller.Animator;
            if (animator == null)
                Debug.LogWarning("PunchComboAbility: Animator not found in TopDownController!");
        }
    }

    protected override void Update()
    {
        if ((cooldownTimer > 0)&& (!isPunching))
        {
            cooldownTimer -= Time.deltaTime;
            
        }

        if (Input.GetKeyDown(activationKey) )
        {
            Activate();
        }
        if (Input.GetKeyUp(activationKey))
        {
            Deactivate();
        }

        if (isPunching)
        {
            Vector3 direction = LookAtMouse();
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            Quaternion smoothRotation = Quaternion.RotateTowards(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(smoothRotation);
            ApplyPush(-direction);

        }
    }


    Vector3 LookAtMouse()
    {
        

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 mouseWorldPos = ray.GetPoint(distance);

            Vector3 direction = mouseWorldPos - transform.position;
            direction.y = 0f;

            return direction;

            Debug.Log(mouseWorldPos);
        }
        return Vector3.zero;
    }
    private void ApplyPush(Vector3 direction)
    {
        if (rb != null && !rb.isKinematic)
        {
            var pushDir = direction.normalized;
            pushDir.y = 0f;
            //rb.AddForce(direction * pushForce, ForceMode.Force);
            rb.MovePosition(rb.position + pushDir * pushForce * Time.fixedDeltaTime);
        }
    }

    protected override bool CanActivate()
    {
        return (cooldownTimer <= 0);
    }

    private void OnDisable()
    {
        if (isPunching)
            Deactivate();
    }
}
