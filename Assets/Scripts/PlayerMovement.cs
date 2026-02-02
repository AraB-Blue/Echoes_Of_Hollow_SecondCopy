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

    [Header("Sistema de Audio")]
    [Tooltip("Déjalo vacío, se creará automáticamente")]
    public AudioSource audioSource;

    [Header("Sonidos de Movimiento")]
    [Tooltip("Sonidos de pasos que se reproducen al caminar")]
    public AudioClip[] footstepSounds;

    [Tooltip("Tiempo entre cada paso (en segundos)")]
    public float footstepInterval = 0.5f;

    [Tooltip("Volumen de los pasos (0-1)")]
    [Range(0f, 1f)]
    public float footstepVolume = 0.3f;

    [Tooltip("Velocidad mínima para reproducir sonidos de pasos")]
    public float minSpeedForFootsteps = 0.1f;

    [Header("Sonidos de Dash")]
    [Tooltip("Sonido que se reproduce al hacer dash")]
    public AudioClip dashSound;

    [Tooltip("Volumen del dash (0-1)")]
    [Range(0f, 1f)]
    public float dashVolume = 0.7f;

    [Header("Configuración de Audio 3D")]
    [Tooltip("Usar audio 3D (true) o 2D (false)")]
    public bool use3DAudio = false;

    [Tooltip("Distancia mínima para audio 3D")]
    public float audioMinDistance = 3f;

    [Tooltip("Distancia máxima para audio 3D")]
    public float audioMaxDistance = 15f;

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

    // Variables de audio
    private float nextFootstepTime;
    private bool isMoving;

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

        // Configurar AudioSource automáticamente
        SetupAudioSource();
    }

    /// <summary>
    /// Configura el AudioSource automáticamente si no existe o lo actualiza si ya existe
    /// </summary>
    private void SetupAudioSource()
    {
        // Si no hay referencia, intentar obtener componente existente
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // Si aún no existe, crear uno nuevo
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            Debug.Log($"AudioSource creado automáticamente en {gameObject.name}");
        }

        // Configurar propiedades
        audioSource.playOnAwake = false;
        audioSource.loop = false;

        // Configurar como 2D o 3D según preferencia
        if (use3DAudio)
        {
            audioSource.spatialBlend = 1f; // 3D
            audioSource.minDistance = audioMinDistance;
            audioSource.maxDistance = audioMaxDistance;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
        }
        else
        {
            audioSource.spatialBlend = 0f; // 2D (normal para jugador)
        }

        audioSource.dopplerLevel = 0f;
        audioSource.priority = 64; // Prioridad alta para el jugador
    }

    void OnEnable()
    {
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

        if (dashAction != null)
        {
            dashAction.performed -= OnDash;
        }
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
            isMoving = false;
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

        // Sistema de audio para pasos
        isMoving = targetSpeed > minSpeedForFootsteps && controller.isGrounded;
        PlayFootstepSounds();

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
        if (context.performed && Time.time >= lastDashTime + dashCooldown)
        {
            if (moveDirection == Vector3.zero)
            {
                moveDirection = transform.forward;
            }

            isDashing = true;
            dashTime = 0f;
            lastDashTime = Time.time;

            // Reproducir sonido de dash
            PlayDashSound();
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

    // MÉTODOS DE AUDIO

    /// <summary>
    /// Reproduce sonidos de pasos mientras el jugador se mueve
    /// </summary>
    private void PlayFootstepSounds()
    {
        if (footstepSounds == null || footstepSounds.Length == 0)
            return;

        if (audioSource == null)
        {
            Debug.LogWarning($"AudioSource no encontrado en {gameObject.name}");
            return;
        }

        // Solo reproducir si el jugador se está moviendo y está en el suelo
        if (isMoving && Time.time >= nextFootstepTime)
        {
            // Seleccionar un sonido aleatorio de paso
            AudioClip randomFootstep = footstepSounds[Random.Range(0, footstepSounds.Length)];

            if (randomFootstep != null)
            {
                audioSource.PlayOneShot(randomFootstep, footstepVolume);
            }

            // Establecer el próximo tiempo de paso
            nextFootstepTime = Time.time + footstepInterval;
        }
    }

    /// <summary>
    /// Reproduce el sonido de dash
    /// </summary>
    private void PlayDashSound()
    {
        if (dashSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(dashSound, dashVolume);
        }
    }

    /// <summary>
    /// Reproduce un sonido personalizado (para uso externo)
    /// </summary>
    public void PlayCustomSound(AudioClip clip, float volume = 1f)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }

    public Vector3 GetMoveDirection() => moveDirection;
    public Animator GetAnimator() => animator;
}