using System.Collections.Generic;
using Froggy.Template;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Froggy.Personnages
{
    /// <summary>
    /// Outil de test : choisit le modèle de Froggy affiché (actions Previous / Next) et l'anime d'après le Rigidbody
    /// (Repos, Marche, Course, Saut) ; Interact lance la Victoire. Seul l'habillage change : déplacement, collider et
    /// physique restent ceux du joueur. Le choix n'est pas sauvegardé. Sans raccourcis (vitrines du jardin, où E est le pas
    /// de côté), le menu de debug choisit le modèle et lance la Victoire. L'index -1 garde la forme provisoire du template.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(PlayerInput))]
    public class FroggyApparence : MonoBehaviour
    {
        [System.Serializable]
        public class Modele
        {
            public string nom;
            [Tooltip("Racine du FBX qui porte le modèle (le Repos).")]
            public GameObject prefab;
            public RuntimeAnimatorController animateur;
            [Tooltip("Rotation du modèle autour de la verticale, en degrés, si son avant n'est pas l'avant du joueur.")]
            public float lacet;
        }

        [SerializeField] private List<Modele> modeles = new List<Modele>();

        [Tooltip("Partie visible du joueur, tournée vers la direction de course : le modèle y est posé.")]
        [SerializeField] private Transform visual;

        [Tooltip("Rendus de la forme provisoire du template, cachés tant qu'un modèle est affiché.")]
        [SerializeField] private List<Renderer> aCacher = new List<Renderer>();

        [Tooltip("Hauteur du modèle, en m, pieds posés en bas du collider. PROVISOIRE.")]
        [SerializeField] private float hauteur = 1f;

        [Tooltip("-1 : la forme provisoire du template.")]
        [SerializeField] private int modeleDeDepart;

        [Tooltip("Previous / Next changent de modèle, Interact lance la Victoire, et l'aide s'affiche. Sans : le menu de debug.")]
        [SerializeField] private bool raccourcis = true;

        private static readonly int Vitesse = Animator.StringToHash("Vitesse");
        private static readonly int AuSol = Animator.StringToHash("AuSol");
        private static readonly int Saut = Animator.StringToHash("Saut");
        private static readonly int Victoire = Animator.StringToHash("Victoire");

        private Rigidbody body;
        private TemplatePlayerController controleur;
        private Froggy.Deplacement.DeplacementWow wow;   // la caméra WoW éteint le contrôleur du template : c'est lui qui sait le sol
        private InputAction precedent;
        private InputAction suivant;
        private InputAction victoire;

        // le dernier modèle choisi dans la partie : il suit un changement de scène (contour de l'arène dans les vitrines)
        private static int? choixDeLaPartie;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Oublier() => choixDeLaPartie = null;   // sans rechargement de domaine, il survivrait à la partie

        private int courant = -1;
        private GameObject instance;
        private Animator animator;
        private bool etaitAuSol = true;

        public string NomCourant => courant >= 0 ? modeles[courant].nom : "";
        public int Courant => courant;
        public int Nombre => modeles.Count;
        public string Nom(int index) => modeles[index].nom;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            controleur = GetComponent<TemplatePlayerController>();
            wow = GetComponent<Froggy.Deplacement.DeplacementWow>();
            var actions = GetComponent<PlayerInput>().actions;
            precedent = actions["Previous"];
            suivant = actions["Next"];
            victoire = actions["Interact"];
        }

        private void Start()
        {
            int depart = choixDeLaPartie ?? modeleDeDepart;
            if (modeles.Count > 0 && depart >= 0)
                Afficher(Mathf.Min(depart, modeles.Count - 1));
        }

        private void Update()
        {
            if (raccourcis && modeles.Count > 1)
            {
                if (suivant.WasPressedThisFrame()) Afficher((courant + 1) % modeles.Count);
                if (precedent.WasPressedThisFrame()) Afficher((courant + modeles.Count - 1) % modeles.Count);
            }
            if (animator == null)
                return;

            Vector3 v = body.linearVelocity;
            bool auSol = wow != null && wow.enabled ? wow.AuSol : controleur == null || controleur.IsGrounded;
            animator.SetFloat(Vitesse, new Vector2(v.x, v.z).magnitude, 0.1f, Time.deltaTime);
            animator.SetBool(AuSol, auSol);
            if (etaitAuSol && !auSol && v.y > 0.5f)
                animator.SetTrigger(Saut);
            etaitAuSol = auSol;
            if (raccourcis && victoire.WasPressedThisFrame())
                animator.SetTrigger(Victoire);
        }

        /// <summary>Lance la célébration du modèle affiché (aussi depuis le menu de debug).</summary>
        public void Celebrer()
        {
            if (animator != null)
                animator.SetTrigger(Victoire);
        }

        public void Afficher(int index)
        {
            if (index >= -1 && index < modeles.Count)
                choixDeLaPartie = index;
            if (index == -1 && courant != -1)
            {
                // retour à la forme provisoire du template
                if (instance != null)
                    Destroy(instance);
                instance = null;
                animator = null;
                courant = -1;
                foreach (var r in aCacher)
                    if (r != null) r.enabled = true;
                return;
            }
            if (index == courant || index < 0 || index >= modeles.Count || modeles[index].prefab == null)
                return;
            if (instance != null)
                Destroy(instance);

            var m = modeles[index];
            courant = index;
            instance = Instantiate(m.prefab, visual);
            instance.name = m.nom;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.Euler(0f, m.lacet, 0f);
            instance.transform.localScale = Vector3.one;

            // Mise à la hauteur voulue, pieds en bas du collider (mesuré sur la pose de liaison du modèle).
            Bounds b = Bornes(instance);
            float echelle = b.size.y > 0.001f ? hauteur / b.size.y : 1f;
            instance.transform.localScale = Vector3.one * echelle;
            b = Bornes(instance);
            var box = GetComponent<BoxCollider>();
            float bas = box != null ? transform.TransformPoint(box.center - Vector3.up * box.size.y * 0.5f).y : transform.position.y;
            instance.transform.position += Vector3.up * (bas - b.min.y);

            animator = instance.GetComponent<Animator>();
            if (animator == null)
                animator = instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = m.animateur;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            etaitAuSol = true;

            foreach (var r in aCacher)
                if (r != null) r.enabled = false;
        }

        private static Bounds Bornes(GameObject racine)
        {
            var rendus = racine.GetComponentsInChildren<Renderer>();
            if (rendus.Length == 0)
                return new Bounds(racine.transform.position, Vector3.zero);
            Bounds b = rendus[0].bounds;
            foreach (var r in rendus)
                b.Encapsulate(r.bounds);
            return b;
        }

        private void OnGUI()
        {
            if (courant < 0 || !raccourcis)
                return;
            string texte = $"Modèle : {modeles[courant].nom}   ({precedent.GetBindingDisplayString()} / {suivant.GetBindingDisplayString()} : changer, " +
                           $"{victoire.GetBindingDisplayString()} : victoire)";
            GUI.Label(new Rect(16, 16, 900, 30), texte, new GUIStyle(GUI.skin.label) { fontSize = 20 });
        }
    }
}
