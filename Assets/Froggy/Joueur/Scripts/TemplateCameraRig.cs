using UnityEngine;
using UnityEngine.InputSystem;

namespace Froggy.Template
{
    /// <summary>
    /// Caméra orbitale de référence, en troisième personne : seul propriétaire de la caméra.
    /// Enfant du joueur, elle suit sa position interpolée (Rigidbody en Interpolate) sans lissage à elle.
    /// L'action « Look » vient de la souris (delta en pixels) ou du stick droit (valeur par seconde) :
    /// les deux se dosent séparément.
    /// </summary>
    public class TemplateCameraRig : MonoBehaviour
    {
        [Header("Sensibilité (à terme dans les options du joueur)")]
        [Tooltip("Degrés par pixel de souris.")]
        [SerializeField] private float mouseSensitivity = 0.15f;

        [Tooltip("Degrés par seconde au stick poussé à fond.")]
        [SerializeField] private float stickSensitivity = 180f;

        [SerializeField] private bool invertY;

        [Header("Inclinaison")]
        [SerializeField] private float minPitch = -30f;
        [SerializeField] private float maxPitch = 70f;
        [SerializeField] private float startPitch = 15f;

        [Tooltip("Verrouille le curseur au lancement (Échap le libère, un clic le reprend).")]
        [SerializeField] private bool lockCursor = true;

        private InputAction lookAction;
        private float yaw;
        private float pitch;

        /// <summary>Cap horizontal de la caméra, en degrés : repère du déplacement.</summary>
        public float Yaw => yaw;

        private void Awake()
        {
            var playerInput = GetComponentInParent<PlayerInput>();
            lookAction = playerInput.actions["Look"];
            yaw = transform.eulerAngles.y;
            pitch = startPitch;
        }

        private void OnEnable()
        {
            if (lockCursor)
                SetCursorLocked(true);
        }

        private void OnDisable()
        {
            SetCursorLocked(false);
        }

        private void Update()
        {
            UpdateCursorLock();

            Vector2 look = lookAction.ReadValue<Vector2>();
            bool fromPointer = lookAction.activeControl != null && lookAction.activeControl.device is Pointer;
            if (fromPointer && Cursor.lockState != CursorLockMode.Locked)
                look = Vector2.zero;

            Vector2 delta = fromPointer ? look * mouseSensitivity : look * (stickSensitivity * Time.deltaTime);
            yaw = Mathf.Repeat(yaw + delta.x, 360f);
            pitch = Mathf.Clamp(pitch + (invertY ? delta.y : -delta.y), minPitch, maxPitch);

            // Le joueur ne tourne pas (rotation gelée) : la rotation du rig se pose en monde.
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private void UpdateCursorLock()
        {
            if (!lockCursor)
                return;
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                SetCursorLocked(false);
            else if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                SetCursorLocked(true);
        }

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
