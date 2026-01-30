using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    public float speed = 5f;
    public float gravity = -9.81f;

    [Header("Dash")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    [Header("Rotación")]
    public Camera mainCamera;

    [Header("Animación")]
    [Tooltip("Velocidad de transición del blend tree")]
    public float animationSmoothTime = 0.1f;

    [HideInInspector]
    public bool canMove = true;

    private CharacterController controller;
    private PlayerInput playerInput;
    private Animator animator;

    private Vector2 moveInput;
    private Vector3 moveDirection;
    private float verticalVelocity = 0f;

    private bool isDashing = false;
    private float dashTime = 0f;
    private float lastDashTime = -999f;

    private InputAction moveAction;
    private InputAction dashAction;

    private float currentSpeed;
    private float speedVelocity;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
        animator = GetComponent<Animator>();

         playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
        
        if (mainCamera == null)
            mainCamera = Camera.main;

        var actions = playerInput.actions;
        if (actions != null)
        {
            moveAction = actions.FindAction("Move");
            dashAction = actions.FindAction("Dash");
        }
    }

    void OnEnable()
    {
        moveAction?.Enable();
        dashAction?.Enable();

        if (moveAction != null)
        {
            moveAction.performed += OnMove;
            moveAction.canceled += OnMove;
        }

        if (dashAction != null)
            dashAction.performed += OnDash;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.performed -= OnMove;
            moveAction.canceled -= OnMove;
        }

        dashAction?.Disable();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (!canMove)
        {
            UpdateAnimator(0f);
            return;
        }

        // Aplicar gravedad
        if (controller.isGrounded)
            verticalVelocity = -2f;
        else
            verticalVelocity += gravity * Time.deltaTime;

        if (isDashing)
        {
            Vector3 dashMove = moveDirection * dashSpeed + Vector3.up * verticalVelocity;
            controller.Move(dashMove * Time.deltaTime);
            dashTime += Time.deltaTime;
            
            UpdateAnimator(1.5f);
            animator.SetBool("IsDashing", true);
            
            if (dashTime >= dashDuration)
            {
                isDashing = false;
                animator.SetBool("IsDashing", false);
            }
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        // Calcular dirección de movimiento
        Vector3 camForward = mainCamera.transform.forward;
        Vector3 camRight = mainCamera.transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 inputDir = camForward * moveInput.y + camRight * moveInput.x;
        if (inputDir.magnitude > 1f)
            inputDir.Normalize();

        moveDirection = inputDir;

        // Combinar movimiento horizontal y vertical
        Vector3 move = moveDirection * speed + Vector3.up * verticalVelocity;
        controller.Move(move * Time.deltaTime);

        float targetSpeed = moveDirection.magnitude;
        UpdateAnimator(targetSpeed);

        RotateTowardsMouse();
    }

    void UpdateAnimator(float targetSpeed)
    {
        if (animator == null) return;

        currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedVelocity, animationSmoothTime);
        animator.SetFloat("Speed", currentSpeed);
        animator.SetBool("IsMoving", currentSpeed > 0.01f);
    }

    void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    void OnDash(InputAction.CallbackContext context)
    {
        if (context.performed && Time.time >= lastDashTime + dashCooldown && moveDirection != Vector3.zero)
        {
            isDashing = true;
            dashTime = 0f;
            lastDashTime = Time.time;
        }
    }

    void RotateTowardsMouse()
    {
        if (mainCamera == null || Mouse.current == null) return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 hitPoint = ray.GetPoint(distance);
            Vector3 lookDir = hitPoint - transform.position;
            lookDir.y = 0f;

            if (lookDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, 15f * Time.deltaTime);
            }
        }
    }

    public Vector3 GetMoveDirection() => moveDirection;
    public Animator GetAnimator() => animator;
}