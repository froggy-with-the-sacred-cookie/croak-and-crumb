using Froggy.Socle;
using Froggy.Template;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Froggy.Deplacement
{
    /// <summary>
    /// La nage de Froggy (P1-02) : quand son centre est dans une ZoneEau, DeplacementWow lui passe la main. Froggy flotte
    /// la tête hors de l'eau, nage plus lentement qu'elle ne court (sans dash), plonge tant qu'on tient la touche de
    /// plongée (C, ou le bouton est de la manette : l'action Crouch), et ressort en sautant sur un bord ou un nénuphar.
    /// Dans l'eau, la gravité est coupée : la hauteur suit la surface. Le demi-tour reste net ; l'inertie se verra par
    /// l'animation et les éclaboussures (pas encore). Les Modificateurs du socle jouent aussi dans l'eau.
    /// La logique tient dans Nager(), appelée par DeplacementWow au pas physique, pour que les tests la fassent tourner.
    /// </summary>
    public class NageFroggy : MonoBehaviour
    {
        [SerializeField] private ReglagesNage reglages;

        private readonly Collider[] tampon = new Collider[8];
        private InputAction plonger;
        [System.NonSerialized] private bool imposee, plongeeImposee, sortie;

        /// <summary>Vrai tant que Froggy est dans l'eau.</summary>
        public bool DansLEau { get; private set; }

        /// <summary>Vrai tant que Froggy plonge.</summary>
        public bool Plonge { get; private set; }

        /// <summary>Froggy vient d'entrer dans l'eau (éclaboussure, son) ou d'en sortir (sortie d'eau, gerbe).</summary>
        public event System.Action Entree, Sortie;

        /// <summary>Pour un test : des réglages à soi (une copie, jamais l'asset partagé).</summary>
        public void Regler(ReglagesNage r) => reglages = r;

        /// <summary>Pour un test : la plongée vient d'ici, plus des appareils.</summary>
        public void Imposer(bool plongee)
        {
            imposee = true;
            plongeeImposee = plongee;
        }

        private void OnEnable()
        {
            if (plonger == null)
            {
                plonger = Froggy.Interface.Commandes.Creer("Plonger");   // C ou bouton est, réaffectable (P6-05)
            }
            plonger.Enable();
        }

        private void OnDisable() => plonger?.Disable();

        private void OnDestroy() => Froggy.Interface.Commandes.Liberer(plonger);

        /// <summary>La zone d'eau où se trouve ce point (le centre de Froggy), ou null.</summary>
        public ZoneEau Zone(Vector3 centre) => ZoneEau.Trouver(gameObject.scene.GetPhysicsScene(), centre, tampon);

        /// <summary>
        /// Un pas de nage, si le centre de Froggy est dans l'eau. Rend false hors de l'eau (DeplacementWow continue alors
        /// comme au sol). « sauter » : un saut est demandé ; « sautUtilise » dit s'il a servi (sortie d'eau).
        /// </summary>
        public bool Nager(float dt, Vector3 centre, Vector2 deplacement, bool sauter, TemplateMovementSettings course,
            ReglagesWow wow, Modificateurs modificateurs, Rigidbody corps, out bool sautUtilise)
        {
            sautUtilise = false;
            if (reglages == null) reglages = ScriptableObject.CreateInstance<ReglagesNage>();
            var zone = Zone(centre);
            if (zone == null)
            {
                Quitter(corps);
                return false;
            }
            if (!DansLEau)
            {
                DansLEau = true;
                Entree?.Invoke();
            }
            Vector3 v = corps.linearVelocity;
            // un saut de sortie monte avec la gravité normale jusqu'à quitter l'eau : l'eau ne le freine pas
            if (sortie && v.y > 0f) sortie = true;
            else sortie = false;
            corps.useGravity = sortie;

            Plonge = imposee ? plongeeImposee : plonger != null && plonger.IsPressed();
            float surface = zone.Surface;
            float facteurVitesse = modificateurs != null ? modificateurs.Vitesse : 1f;
            float facteurSaut = modificateurs != null ? modificateurs.Saut : 1f;

            Vector3 voulue = DeplacementWow.Vitesse(deplacement, corps.rotation.eulerAngles.y, course, wow)
                             * reglages.facteurVitesse * facteurVitesse;
            Vector3 horizontal = Vector3.MoveTowards(new Vector3(v.x, 0f, v.z), voulue, reglages.acceleration * dt);

            // la hauteur se donne : on va vers la profondeur voulue, vitesse bornée, freinage par l'accélération verticale
            float nage = surface - reglages.flottaison;
            float cible = Plonge ? surface - reglages.plongee : nage;
            if (!sortie)
            {
                float vy = Mathf.Clamp((cible - centre.y) * 4f, -reglages.vitesseVerticale, reglages.vitesseVerticale);
                v.y = Mathf.MoveTowards(v.y, vy, reglages.accelerationVerticale * dt);
            }

            // ressortir : un saut depuis la surface (pas en plongée), assez haut pour monter sur un bord
            if (sauter && !sortie && !Plonge && centre.y >= nage - reglages.margeSortie)
            {
                float hauteur = reglages.hauteurSortie * facteurSaut;
                v.y = Mathf.Sqrt(2f * -Physics.gravity.y * hauteur);
                corps.useGravity = true;
                sortie = true;
                sautUtilise = true;
            }
            corps.linearVelocity = new Vector3(horizontal.x, v.y, horizontal.z);
            return true;
        }

        /// <summary>Hors de l'eau : la gravité revient.</summary>
        public void Quitter(Rigidbody corps)
        {
            if (!DansLEau) return;
            DansLEau = false;
            Plonge = false;
            sortie = false;
            if (corps != null) corps.useGravity = true;
            Sortie?.Invoke();
        }
    }
}
