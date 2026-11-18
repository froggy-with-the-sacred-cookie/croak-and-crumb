using FishNet.Object;
using Froggy.Deplacement;
using Froggy.Personnages;
using Froggy.Regles;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using ScenesUnity = UnityEngine.SceneManagement.SceneManager;

namespace Froggy.Reseau
{
    /// <summary>
    /// La Froggy d'un joueur en réseau (P2-02, première version : chacun pilote la sienne). Cet objet réseau ne se voit
    /// pas : il porte la position et la rotation (NetworkTransform, client autoritaire, lissé chez les autres). La vraie
    /// Froggy est une copie de celle du jardin (« PlayerTemplate »), faite à l'arrivée sur chaque machine, comme les bots :
    /// - chez son propriétaire, elle garde son contrôleur, sa caméra et ses touches, et l'objet réseau la suit ;
    /// - chez les autres, elle n'a ni caméra ni touches, son corps ne bouge plus seul, elle suit l'objet réseau et
    ///   s'anime d'après sa vitesse et le sol sous elle (FroggyApparence.VitesseExterne / AuSolExterne).
    /// La copie porte les cookies sous le numéro du joueur dans le salon (PorteurCookies) : l'hôte décide avec elle.
    /// Pas encore : la prédiction (l'hôte qui fait foi), les coups et les objets en réseau (#128), l'apparence choisie.
    /// </summary>
    public class FroggyReseau : NetworkBehaviour
    {
        [Tooltip("Nom de la Froggy du jardin à copier (objet racine d'une scène chargée).")]
        [SerializeField] private string modele = "PlayerTemplate";
        [Tooltip("Marge sous les pieds pour dire qu'une Froggy des autres touche le sol, en m.")]
        [SerializeField] private float margeSol = 0.15f;
        [Tooltip("Lissage de la vitesse lue chez les autres (animation), en s.")]
        [SerializeField] private float lissageVitesse = 0.08f;

        private GameObject froggy;
        private Collider collisionFroggy;
        private Vector3 dernierePosition, vitesse;
        private bool auSol = true;
        private readonly RaycastHit[] touches = new RaycastHit[8];

        /// <summary>La Froggy de ce joueur (la copie qui se voit), ou null avant son arrivée.</summary>
        public GameObject Froggy => froggy;

        /// <summary>Celle du joueur de cette machine, ou null.</summary>
        public static FroggyReseau Locale { get; private set; }

        public override void OnStartClient()
        {
            base.OnStartClient();
            GameObject source = TrouverModele();
            if (source == null)
            {
                Debug.LogWarning($"[FroggyReseau] Pas de « {modele} » dans les scènes chargées : rien à copier.");
                return;
            }
            froggy = Copier(source, IsOwner);
            collisionFroggy = froggy.GetComponent<Collider>();
            dernierePosition = transform.position;
            if (IsOwner) Locale = this;
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            if (Locale == this) Locale = null;
            if (froggy != null) Destroy(froggy);
            froggy = null;
        }

        private void LateUpdate()
        {
            if (froggy == null) return;
            if (IsOwner)
            {
                // c'est ma Froggy qui bouge : l'objet réseau prend sa place, le NetworkTransform l'envoie
                transform.SetPositionAndRotation(froggy.transform.position, froggy.transform.rotation);
                return;
            }
            // une Froggy des autres : elle suit la position reçue (déjà lissée par le NetworkTransform)
            Vector3 ici = transform.position;
            froggy.transform.SetPositionAndRotation(ici, transform.rotation);
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            Vector3 mesure = (ici - dernierePosition) / dt;
            vitesse = Vector3.Lerp(vitesse, mesure, 1f - Mathf.Exp(-dt / Mathf.Max(1e-3f, lissageVitesse)));
            dernierePosition = ici;
            auSol = ToucheLeSol();
        }

        // Une marge sous le bas de sa boîte de collision : du décor, pas elle-même.
        private bool ToucheLeSol()
        {
            if (collisionFroggy == null) return true;
            Bounds b = collisionFroggy.bounds;
            float distance = b.extents.y + margeSol;
            int n = froggy.scene.GetPhysicsScene().Raycast(b.center, Vector3.down, touches, distance, ~0,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
                if (!touches[i].transform.IsChildOf(froggy.transform)) return true;
            return false;
        }

        // La Froggy du jardin, éteinte pour la partie en réseau : seules les copies des joueurs jouent.
        private GameObject TrouverModele()
        {
            for (int s = 0; s < ScenesUnity.sceneCount; s++)
            {
                Scene scene = ScenesUnity.GetSceneAt(s);
                if (!scene.isLoaded) continue;
                foreach (GameObject racine in scene.GetRootGameObjects())
                {
                    if (racine.name != modele || racine.GetComponent<DeplacementWow>() == null) continue;
                    racine.SetActive(false);
                    return racine;
                }
            }
            return null;
        }

        private GameObject Copier(GameObject source, bool moi)
        {
            int numero = 0;
            bool numerote = Salon.Local != null && Salon.Local.NumeroDe(OwnerId, out numero);

            // copiée sous un parent éteint : rien ne s'éveille avant le tri (même méthode que BotsPartie)
            var attente = new GameObject("(Froggy en préparation)");
            attente.SetActive(false);
            GameObject copie = Instantiate(source, transform.position, transform.rotation, attente.transform);
            copie.name = numerote ? $"Froggy {numero}" + (moi ? " (moi)" : "") : moi ? "Froggy (moi)" : "Froggy";

            // le modèle affiché a été copié sans son lien vers FroggyApparence : il resterait figé
            foreach (var a in copie.GetComponentsInChildren<Animator>(true))
                if (a != null && a.gameObject != copie) DestroyImmediate(a.gameObject);

            if (!moi)
            {
                foreach (var c in copie.GetComponentsInChildren<Camera>(true))
                {
                    // la caméra et son support (CameraOrbitale) : le plus haut enfant qui la porte
                    Transform t = c.transform;
                    while (t.parent != null && t.parent != copie.transform) t = t.parent;
                    if (t != copie.transform) DestroyImmediate(t.gameObject);
                }
                foreach (var l in copie.GetComponentsInChildren<AudioListener>(true)) DestroyImmediate(l);
                foreach (var c in copie.GetComponents<Froggy.Template.TemplatePlayerController>()) DestroyImmediate(c);
                // FroggyApparence exige un PlayerInput : il reste, éteint. Le joueur de cette machine se reconnaît à un
                // PlayerInput allumé (PartieTorgnoles, PartieObjets, BotsPartie.Humain).
                foreach (var c in copie.GetComponents<PlayerInput>()) c.enabled = false;
                foreach (var c in copie.GetComponents<DeplacementWow>()) c.enabled = false;
                foreach (var c in copie.GetComponents<NageFroggy>()) c.enabled = false;
                var corps = copie.GetComponent<Rigidbody>();
                if (corps != null)
                {
                    corps.isKinematic = true;
                    corps.interpolation = RigidbodyInterpolation.None;
                }
                var apparence = copie.GetComponent<FroggyApparence>();
                if (apparence != null)
                {
                    apparence.VitesseExterne = () => vitesse;
                    apparence.AuSolExterne = () => auSol;
                }
            }

            var porteur = copie.GetComponent<PorteurCookies>();
            if (porteur == null) porteur = copie.AddComponent<PorteurCookies>();
            if (numerote) porteur.Numeroter(numero);

            copie.transform.SetParent(null, true);
            ScenesUnity.MoveGameObjectToScene(copie, source.scene);
            copie.SetActive(true);   // le modèle est éteint : sa copie l'était aussi
            Destroy(attente);
            return copie;
        }
    }
}
