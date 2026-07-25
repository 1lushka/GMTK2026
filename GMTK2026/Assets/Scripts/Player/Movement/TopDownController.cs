using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TopDownController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float maxSpeed = 6f;
    [SerializeField] private float acceleration = 25f;
    [SerializeField] private float deceleration = 30f;

    [Header("Turning")]
    [SerializeField] private bool rotateToMovement = true;
    [SerializeField] private float rotationSpeed = 720f;

    [Header("Physics")]
    [SerializeField] private float drag = 0f;
    [SerializeField] private float mass = 1f;

    [Header("Input")]
    [SerializeField] private bool normalizeInput = true;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParam = "Speed";

    [Header("Particles (Run Effect)")]
    [SerializeField] private ParticleSystem runParticles;
    [SerializeField] private float runParticleSpeedThreshold = 0.8f;

    [Header("Debug")]
    [SerializeField] private bool debugVelocity = false;

    private Rigidbody rb;
    private Vector3 input;
    private Vector3 velocity;

    // Публичный доступ для способностей
    public bool enableMovement = true;
    public Vector3 CurrentVelocity => velocity;
    public Animator Animator => animator;
    public void SetVelocity(Vector3 newVelocity) { velocity = newVelocity; }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        //rb.useGravity = false;
        rb.linearDamping = drag;
        rb.mass = mass;

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (runParticles == null) runParticles = GetComponentInChildren<ParticleSystem>();
    }

    void Update()
    {
        GetInput();
        UpdateAnimation();
        UpdateRunParticles();
    }

    void FixedUpdate()
    {
        if (enableMovement)
        {
            Move();
            Rotate();
        }
    }

    void GetInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        input = new Vector3(h, 0, v);
        if (normalizeInput) input = Vector3.ClampMagnitude(input, 1f);
    }

    void Move()
    {
        Vector3 targetVelocity = input * maxSpeed;
        float accel = input.magnitude > 0.01f ? acceleration : deceleration;
        velocity = Vector3.MoveTowards(velocity, targetVelocity, accel * Time.fixedDeltaTime);
        rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
    }

    void Rotate()
    {
        if (!rotateToMovement) return;
        if (velocity.sqrMagnitude < 0.001f) return;
        Quaternion targetRotation = Quaternion.LookRotation(velocity);
        Quaternion smoothRotation = Quaternion.RotateTowards(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(smoothRotation);
    }

    public void AddImpulse(Vector3 impulseVelocity)
    {
        velocity = impulseVelocity;
        // контроллер сам погасит скорость, если нет ввода
    }

    void UpdateAnimation()
    {
        if (animator == null) return;
        float currentSpeed = velocity.magnitude / maxSpeed;
        animator.SetFloat(speedParam, currentSpeed);
    }

    void UpdateRunParticles()
    {
        if (runParticles == null) return;
        float speedFactor = velocity.magnitude / maxSpeed;
        bool shouldPlay = speedFactor >= runParticleSpeedThreshold;
        if (shouldPlay && !runParticles.isPlaying) runParticles.Play();
        else if (!shouldPlay && runParticles.isPlaying) runParticles.Stop();
    }

    void OnDrawGizmos()
    {
        if (!debugVelocity) return;
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, velocity);
    }

    public void ActivateRotation (bool activate)
    {
        rotateToMovement = activate;
    }
}