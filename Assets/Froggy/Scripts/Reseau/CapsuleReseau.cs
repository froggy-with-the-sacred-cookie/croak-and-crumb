using FishNet.Object;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Froggy.Reseau
{
    /// <summary>
    /// Capsule de test réseau (P2-01) : seul son propriétaire la déplace, le NetworkTransform
    /// (client autoritaire) envoie la position aux autres. Volontairement minimal : le contrôleur de
    /// Froggy en réseau arrive avec P2-02.
    /// </summary>
    public class CapsuleReseau : NetworkBehaviour
    {
        [SerializeField] private float vitesse = 5f;

        [Tooltip("Teinte de sa propre capsule.")]
        [SerializeField] private Color couleurLocale = new Color(0.30f, 0.85f, 0.35f);

        [Tooltip("Teinte des capsules des autres joueurs.")]
        [SerializeField] private Color couleurAutres = new Color(0.90f, 0.35f, 0.30f);

        private InputAction actionDeplacement;

        public override void OnStartClient()
        {
            base.OnStartClient();

            // Dans Awake et Start, le propriétaire n'est pas encore connu : on décide ici.
            var rendu = GetComponentInChildren<Renderer>();
            if (rendu != null)
            {
                // Dans un salon (P2-03) : la couleur choisie par le joueur ; sinon vert = la mienne, rouge = les autres.
                rendu.material.color = Salon.Local != null && Salon.Local.CouleurDe(OwnerId, out Color choisie)
                    ? choisie
                    : IsOwner ? couleurLocale : couleurAutres;
            }

            if (IsOwner)
            {
                // Actions du projet (Assets/InputSystem_Actions), carte « Player ».
                actionDeplacement = InputSystem.actions?.FindAction("Player/Move");
                actionDeplacement?.Enable();
            }
        }

        private void Update()
        {
            if (!IsOwner || actionDeplacement == null)
                return;

            Vector2 entree = actionDeplacement.ReadValue<Vector2>();
            Vector3 deplacement = new Vector3(entree.x, 0f, entree.y);
            transform.position += Vector3.ClampMagnitude(deplacement, 1f) * (vitesse * Time.deltaTime);
        }
    }
}
