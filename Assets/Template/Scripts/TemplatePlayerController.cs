using UnityEngine;
using UnityEngine.InputSystem;

namespace Froggy.Template
{
    /// <summary>
    /// Déplacement de référence : course relative à la caméra, sprint, saut avec coyote time, tampon et saut variable.
    /// Les entrées se captent dans Update et se consomment au pas physique (FixedUpdate).
    /// Les actions viennent de PlayerInput (Assets/InputSystem_Actions, carte « Player ») : clavier/souris et manette.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(PlayerInput))]
    public class TemplatePlayerController : MonoBehaviour
    {
        [SerializeField] private TemplateMovementSettings settings;

        [Tooltip("Rig de caméra : son cap (yaw) donne le repère de déplacement.")]
        [SerializeField] private TemplateCameraRig cameraRig;

        [Tooltip("Partie visible, tournée vers la direction de course (purement visuel).")]
        [SerializeField] private Transform visual;

        [SerializeField] private float visualTurnSpeed = 720f;

        private Rigidbody body;
        private BoxCollider box;
        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction sprintAction;

        // Entrées captées dans Update.
        private Vector2 moveInput;
        private bool sprintHeld;
        private bool jumpHeld;
        private bool jumpPressed;

        // État au pas physique.
        private float coyoteTimer;
        private float jumpBufferTimer;
        private bool isJumpRising;

        public bool IsGrounded { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            box = GetComponent<BoxCollider>();

            var actions = GetComponent<PlayerInput>().actions;
            moveAction = actions["Move"];
            jumpAction = actions["Jump"];
            sprintAction = actions["Sprint"];
        }

        private void Update()
        {
            moveInput = moveAction.ReadValue<Vector2>();
            sprintHeld = sprintAction.IsPressed();
            jumpHeld = jumpAction.IsPressed();
            if (jumpAction.WasPressedThisFrame())
                jumpPressed = true;

            UpdateVisualFacing();
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            // L'appui se consomme et s'efface à chaque pas, même sans effet.
            if (jumpPressed)
                jumpBufferTimer = settings.jumpBufferTime;
            jumpPressed = false;

            Vector3 velocity = body.linearVelocity;

            // En montée, on ne se recolle pas au sol (sinon le coyote time relancerait un saut).
            IsGrounded = velocity.y <= 0.01f && CheckGround();
            coyoteTimer = IsGrounded ? settings.coyoteTime : coyoteTimer - dt;
            jumpBufferTimer -= dt;
            if (IsGrounded)
                isJumpRising = false;

            // Course relative au cap de la caméra.
            float yaw = cameraRig != null ? cameraRig.Yaw : transform.eulerAngles.y;
            Vector3 direction = Quaternion.Euler(0f, yaw, 0f) * new Vector3(moveInput.x, 0f, moveInput.y);
            direction = Vector3.ClampMagnitude(direction, 1f);

            float speed = settings.moveSpeed * (sprintHeld ? settings.sprintMultiplier : 1f);
            float acceleration = IsGrounded ? settings.groundAcceleration : settings.airAcceleration;
            Vector3 horizontal = Vector3.MoveTowards(new Vector3(velocity.x, 0f, velocity.z), direction * speed,
                acceleration * dt);

            // Saut : la hauteur se donne, la vitesse se dérive.
            if (jumpBufferTimer > 0f && coyoteTimer > 0f)
            {
                velocity.y = Mathf.Sqrt(2f * -Physics.gravity.y * settings.jumpHeight);
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
                isJumpRising = true;
                IsGrounded = false;
            }

            // Saut variable : relâcher coupe la montée, une seule fois, et seulement ce qui vient d'un saut.
            if (isJumpRising && !jumpHeld && velocity.y > 0f)
            {
                velocity.y *= settings.jumpCutMultiplier;
                isJumpRising = false;
            }
            if (velocity.y <= 0f)
                isJumpRising = false;

            body.linearVelocity = new Vector3(horizontal.x, velocity.y, horizontal.z);
        }

        private bool CheckGround()
        {
            // Une boîte un peu plus étroite que le collider, lancée vers le bas.
            // Un BoxCast ne voit pas les colliders qu'il chevauche au départ : le sien est ignoré.
            Vector3 halfExtents = Vector3.Scale(box.size, transform.lossyScale) * 0.5f;
            halfExtents.x *= 0.9f;
            halfExtents.z *= 0.9f;
            halfExtents.y = 0.05f;
            Vector3 center = transform.TransformPoint(box.center);
            float distance = Vector3.Scale(box.size, transform.lossyScale).y * 0.5f - halfExtents.y
                             + settings.groundCheckDistance;
            return Physics.BoxCast(center, halfExtents, Vector3.down, transform.rotation, distance,
                settings.groundLayers, QueryTriggerInteraction.Ignore);
        }

        private void UpdateVisualFacing()
        {
            if (visual == null)
                return;
            Vector3 horizontal = body.linearVelocity;
            horizontal.y = 0f;
            if (horizontal.sqrMagnitude < 0.01f)
                return;
            Quaternion target = Quaternion.LookRotation(horizontal, Vector3.up);
            visual.rotation = Quaternion.RotateTowards(visual.rotation, target, visualTurnSpeed * Time.deltaTime);
        }
    }
}
