using System.Collections.Generic;
using BindingFlags = System.Reflection.BindingFlags;
using FieldInfo = System.Reflection.FieldInfo;
using Froggy.Deplacement;
using Froggy.Template;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace Froggy.Cameras
{
    /// <summary>
    /// Caméra à la troisième personne à la World of Warcraft (proposition pour la vitrine, 28 octobre 2026) :
    /// - le curseur reste libre ; clic gauche maintenu : on regarde autour sans changer le cap de Froggy, et la caméra
    ///   revient derrière lui dès qu'il court ; clic droit maintenu (ou stick droit) : la caméra tourne et Froggy avec elle ;
    /// - au clavier, gauche / droite font tourner Froggy, et la caméra avec lui ; clic droit maintenu, ils font un pas de
    ///   côté. Pour ça, hors clic droit, la composante latérale des touches de Move est mise à zéro pour le contrôleur
    ///   (surcharge de processeur, retirée à la désactivation) ; les touches du virage sont reprises de Move, réaffectation
    ///   comprise. La manette garde le pas de côté au stick gauche et tourne au stick droit ;
    /// - avec DeplacementWow sur le joueur (recul face à l'avant, Q / E, deux boutons pour avancer, course automatique),
    ///   elle l'active et le coupe avec elle : une seule case pour revenir au Template ;
    /// - au relâcher d'un bouton, le curseur revient où il était ;
    /// - molette : on rapproche ou on éloigne, jusqu'à la première personne (Froggy se cache, son ombre reste) ;
    /// - un mur entre Froggy et la caméra : elle se rapproche aussitôt, puis se rallonge doucement (rien ne devient
    ///   transparent, skill camera). Comme WoW, seuls le sol et les murs l'arrêtent (colliders sous un BloqueCamera) :
    ///   elle traverse le petit décor. Florent, 28 octobre 2026 : butée contre chaque pierre, rocher ou bambou, elle
    ///   changeait de distance, donc de hauteur, sans arrêt (« vraiment très désagréable »).
    /// Se pose sur le CameraRig du PlayerTemplate, à côté de TemplateCameraRig qu'elle remplace tant qu'elle est active :
    /// décocher le composant rend l'ancienne caméra (bascule pour comparer). Le cap passe par la rotation du joueur, que
    /// TemplatePlayerController prend comme repère quand il n'a pas de rig. Seule propriétaire de la caméra ; elle suit la
    /// position interpolée du Rigidbody, sans lissage de position à elle.
    /// </summary>
    public class CameraOrbitale : MonoBehaviour
    {
        [Header("Sensibilité (à terme dans les options du joueur)")]
        [Tooltip("Degrés par pixel de souris.")]
        [SerializeField] private float sensibiliteSouris = 0.15f;
        [Tooltip("Degrés par seconde au stick poussé à fond.")]
        [SerializeField] private float sensibiliteStick = 180f;
        [SerializeField] private bool inverserY;

        [Header("Inclinaison, en degrés")]
        [SerializeField] private float inclinaisonMin = -35f;
        [SerializeField] private float inclinaisonMax = 80f;
        [SerializeField] private float inclinaisonDepart = 18f;

        [Header("Distance, en m")]
        [SerializeField] private float distanceDepart = 6f;
        [SerializeField] private float distanceMax = 15f;
        [Tooltip("Mètres par cran de molette.")]
        [SerializeField] private float pasMolette = 1f;
        [Tooltip("Plus près que ça, on passe en première personne.")]
        [SerializeField] private float seuilPremierePersonne = 0.8f;
        [Tooltip("Temps du ressort de zoom, en s.")]
        [SerializeField] private float tempsZoom = 0.12f;
        [Tooltip("Temps pour se rallonger après un obstacle, en s (le rapprochement, lui, est immédiat).")]
        [SerializeField] private float tempsRallonge = 0.35f;

        [Header("Cadrage")]
        [Tooltip("Hauteur du point visé au-dessus du centre du joueur, en troisième personne.")]
        [SerializeField] private float hauteurPivot = 0.75f;
        [Tooltip("Hauteur des yeux au-dessus du centre du joueur, en première personne (hypothèse : Froggy ≈ 1 m).")]
        [SerializeField] private float hauteurYeux = 0.3f;
        [SerializeField] private float champTroisiemePersonne = 60f;
        [Tooltip("Champ de vision en première personne (réglable : confort, skill accessibilite).")]
        [SerializeField] private float champPremierePersonne = 75f;

        [Header("Virage au clavier")]
        [Tooltip("Degrés par seconde, touche gauche ou droite tenue (hypothèse : à peu près celle de WoW).")]
        [SerializeField] private float vitesseVirage = 180f;

        [Header("Retour derrière Froggy")]
        [Tooltip("Degrés par seconde, plafonnés : la caméra ne fait jamais de demi-tour d'un coup.")]
        [SerializeField] private float vitesseRetour = 200f;
        [Tooltip("Vitesse au sol au-delà de laquelle Froggy « court » et rappelle la caméra, en m/s.")]
        [SerializeField] private float vitesseCourse = 0.5f;

        [Header("Collision")]
        [SerializeField] private float rayonCollision = 0.25f;
        [SerializeField] private LayerMask obstacles = ~0;
        [Tooltip("Coché : tout collider arrête la caméra (le premier essai). Décoché : seulement ceux sous un BloqueCamera.")]
        [SerializeField] private bool toutBloque;

        private Transform joueur;
        private Rigidbody corps;
        private Camera cam;
        private TemplateCameraRig ancienRig;
        private TemplatePlayerController controleur;
        private DeplacementWow deplacement;
        private Object rigDuControleur;
        private Vector3 posLocaleCam;
        private Quaternion rotLocaleCam;
        private InputAction lookAction, moveAction, virage;
        private readonly List<int> compositesClavier = new List<int>();
        private readonly List<(Renderer rendu, ShadowCastingMode mode)> rendusJoueur = new List<(Renderer, ShadowCastingMode)>();
        private readonly RaycastHit[] touches = new RaycastHit[32];

        [System.NonSerialized] private float lacet, inclinaison, cap;
        [System.NonSerialized] private float distanceVoulue, distanceZoom, vitesseZoom, distance, vitesseRallonge;
        [System.NonSerialized] private bool cache, initialise, pasDeCote = true, capture;
        [System.NonSerialized] private Vector2 positionCurseur;

        private static readonly FieldInfo ChampRig =
            typeof(TemplatePlayerController).GetField("cameraRig", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>Distance réelle de la caméra (après obstacles), en m.</summary>
        public float Distance => distance;
        public bool PremierePersonne => cache;
        /// <summary>Cap de Froggy, en degrés : le repère de sa course.</summary>
        public float Cap => cap;

        // pour le menu de debug (MenuCamera) : des réglages à essayer en jouant, oubliés à la fin de la partie
        public bool ToutBloque { get => toutBloque; set => toutBloque = value; }
        public bool InverserY { get => inverserY; set => inverserY = value; }
        public float SensibiliteSouris { get => sensibiliteSouris; set => sensibiliteSouris = value; }
        public float VitesseVirage { get => vitesseVirage; set => vitesseVirage = value; }
        public float ChampTroisiemePersonne { get => champTroisiemePersonne; set => champTroisiemePersonne = value; }
        public float DistanceMax
        {
            get => distanceMax;
            set { distanceMax = value; distanceVoulue = Mathf.Min(distanceVoulue, value); }
        }

        private void Awake()
        {
            joueur = GetComponentInParent<Rigidbody>().transform;
            corps = joueur.GetComponent<Rigidbody>();
            cam = GetComponentInChildren<Camera>(true);
            ancienRig = GetComponent<TemplateCameraRig>();
            controleur = joueur.GetComponent<TemplatePlayerController>();
            deplacement = joueur.GetComponent<DeplacementWow>();
            lookAction = joueur.GetComponent<PlayerInput>().actions["Look"];
            moveAction = joueur.GetComponent<PlayerInput>().actions["Move"];
            virage = ActionVirage(moveAction, compositesClavier);
            foreach (var r in joueur.GetComponentsInChildren<Renderer>(true)) rendusJoueur.Add((r, r.shadowCastingMode));
        }

        private void OnEnable()
        {
            if (ancienRig != null) ancienRig.enabled = false;
            if (deplacement != null) deplacement.enabled = true;
            capture = true;
            Curseur(false);   // l'ancien rig a pu le verrouiller
            if (controleur != null && ChampRig != null)
            {
                rigDuControleur = (Object)ChampRig.GetValue(controleur);
                ChampRig.SetValue(controleur, null);
            }
            posLocaleCam = cam.transform.localPosition;
            rotLocaleCam = cam.transform.localRotation;
            if (!initialise)
            {
                cap = lacet = joueur.eulerAngles.y;
                inclinaison = inclinaisonDepart;
                distanceVoulue = distanceZoom = distance = distanceDepart;
                initialise = true;
            }
            virage.Enable();
            PasDeCote(false);
        }

        private void OnDisable()
        {
            Cacher(false);
            Curseur(false);
            PasDeCote(true);
            virage.Disable();
            if (controleur != null && ChampRig != null) ChampRig.SetValue(controleur, rigDuControleur);
            cam.transform.localPosition = posLocaleCam;
            cam.transform.localRotation = rotLocaleCam;
            cam.fieldOfView = champTroisiemePersonne;
            if (deplacement != null) deplacement.enabled = false;
            if (ancienRig != null) ancienRig.enabled = true;
        }

        /// <summary>Pose la caméra d'un coup (captures, tests, réapparition) : lacet, inclinaison et distance voulue.</summary>
        public void Regler(float nouveauLacet, float nouvelleInclinaison, float nouvelleDistance)
        {
            cap = lacet = Mathf.Repeat(nouveauLacet, 360f);
            inclinaison = Mathf.Clamp(nouvelleInclinaison, inclinaisonMin, inclinaisonMax);
            distanceVoulue = distanceZoom = distance = Zoomer(nouvelleDistance, 0f, 1f, distanceMax, seuilPremierePersonne);
            vitesseZoom = vitesseRallonge = 0f;
            corps.rotation = Quaternion.Euler(0f, cap, 0f);
        }

        private void Update()
        {
            var souris = MenuCamera.Ouvert ? null : Mouse.current;   // menu de debug ouvert : la souris est à lui
            bool gauche = souris != null && souris.leftButton.isPressed;
            bool droit = souris != null && souris.rightButton.isPressed;
            Curseur(gauche || droit);

            Vector2 delta = Vector2.zero;
            if ((gauche || droit) && souris != null) delta = souris.delta.ReadValue() * sensibiliteSouris;
            bool stick = lookAction.activeControl != null && !(lookAction.activeControl.device is Pointer);
            if (stick) delta += lookAction.ReadValue<Vector2>() * (sensibiliteStick * Time.deltaTime);

            lacet = Mathf.Repeat(lacet + delta.x, 360f);
            inclinaison = Mathf.Clamp(inclinaison + (inverserY ? delta.y : -delta.y), inclinaisonMin, inclinaisonMax);

            PasDeCote(droit);
            if (!droit)
                (cap, lacet) = Tourner(cap, lacet, virage.ReadValue<float>(), vitesseVirage, Time.deltaTime, gauche);

            if (droit || (stick && delta.x != 0f)) cap = lacet;   // on dirige : Froggy tourne avec la caméra
            else if (!gauche && Course())   // on court sans regarder ailleurs : la caméra revient derrière
                lacet = Mathf.MoveTowardsAngle(lacet, cap, vitesseRetour * Time.deltaTime);

            if (souris != null)
                distanceVoulue = Zoomer(distanceVoulue, -souris.scroll.ReadValue().y, pasMolette, distanceMax, seuilPremierePersonne);
        }

        private void FixedUpdate()
        {
            if (Mathf.Abs(Mathf.DeltaAngle(corps.rotation.eulerAngles.y, cap)) > 0.01f)
                corps.MoveRotation(Quaternion.Euler(0f, cap, 0f));
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            distanceZoom = Mathf.SmoothDamp(distanceZoom, distanceVoulue, ref vitesseZoom, tempsZoom, Mathf.Infinity, dt);
            float t = Mathf.Clamp01(distanceZoom / Mathf.Max(seuilPremierePersonne, 0.01f));
            Vector3 pivot = joueur.position + Vector3.up * Mathf.Lerp(hauteurYeux, hauteurPivot, t);
            Quaternion rotation = Quaternion.Euler(inclinaison, lacet, 0f);
            Vector3 arriere = rotation * Vector3.back;

            float libre = Libre(joueur.gameObject.scene.GetPhysicsScene(), pivot, arriere, distanceZoom, rayonCollision, obstacles, joueur, touches, toutBloque);
            distance = Suivre(distance, libre, distanceZoom, ref vitesseRallonge, tempsRallonge, dt);
            cam.transform.SetPositionAndRotation(pivot + arriere * distance, rotation);
            cam.fieldOfView = Mathf.Lerp(champPremierePersonne, champTroisiemePersonne, t);
            Cacher(distance < seuilPremierePersonne * 0.5f);
        }

        /// <summary>
        /// La distance de la caméra au pas suivant : collée à l'obstacle s'il est plus près (aussitôt), sinon un ressort
        /// critique (sans dépassement) vers la distance voulue.
        /// </summary>
        public static float Suivre(float actuelle, float libre, float voulue, ref float vitesse, float temps, float dt)
        {
            float cible = Mathf.Min(libre, voulue);
            if (cible < actuelle) { vitesse = 0f; return cible; }
            return Mathf.Min(Mathf.SmoothDamp(actuelle, cible, ref vitesse, temps, Mathf.Infinity, dt), cible);
        }

        /// <summary>
        /// Le virage au clavier : le cap tourne de entree × vitesse × dt, et la caméra du même angle, sauf quand on regarde
        /// ailleurs (clic gauche) : elle reviendra derrière Froggy dès qu'il court.
        /// </summary>
        public static (float cap, float lacet) Tourner(float cap, float lacet, float entree, float vitesse, float dt, bool regardeAilleurs)
        {
            float d = Mathf.Clamp(entree, -1f, 1f) * vitesse * dt;
            return (Mathf.Repeat(cap + d, 360f), regardeAilleurs ? lacet : Mathf.Repeat(lacet + d, 360f));
        }

        /// <summary>La distance voulue après un mouvement de molette : bornée, et ramenée à 0 sous le seuil de la première personne.</summary>
        public static float Zoomer(float distance, float crans, float pas, float max, float seuil)
        {
            float d = Mathf.Clamp(distance + crans * pas, 0f, max);
            if (crans < 0f && d < seuil) return 0f;              // en rapprochant, sous le seuil : les yeux de Froggy
            if (crans > 0f && distance < seuil) return Mathf.Max(d, seuil);   // en reculant depuis les yeux : on ressort d'un coup
            return d;
        }

        /// <summary>
        /// La place libre derrière Froggy, le long de direction : la distance du premier obstacle, ou longueur. Un obstacle,
        /// c'est un collider sous un BloqueCamera (ou n'importe lequel avec toutBloque), hors du joueur.
        /// </summary>
        public static float Libre(PhysicsScene physique, Vector3 pivot, Vector3 direction, float longueur, float rayon,
            int masque, Transform joueur, RaycastHit[] touches, bool toutBloque)
        {
            if (longueur <= 0f) return 0f;
            int n = physique.SphereCast(pivot, rayon, direction, touches, longueur, masque, QueryTriggerInteraction.Ignore);
            float libre = longueur;
            for (int i = 0; i < n; i++)
            {
                var h = touches[i];
                if (joueur != null && h.collider.transform.IsChildOf(joueur)) continue;
                if (!toutBloque && h.collider.GetComponentInParent<BloqueCamera>() == null) continue;
                if (h.distance <= 0f && h.point == Vector3.zero) continue;   // départ dans le collider : le rayon ne le voit pas
                libre = Mathf.Min(libre, h.distance);
            }
            return libre;
        }

        /// <summary>
        /// Une action « gauche / droite » faite des touches clavier de Move (chemins effectifs : la réaffectation suit) ;
        /// range dans composites les index des composites clavier de Move, ceux dont on coupe la composante latérale.
        /// </summary>
        private static InputAction ActionVirage(InputAction move, List<int> composites)
        {
            var action = new InputAction("Virage", InputActionType.Value);
            var axe = action.AddCompositeBinding("1DAxis");
            var b = move.bindings;
            for (int i = 0; i < b.Count; i++)
            {
                if (!b[i].isComposite) continue;
                bool clavier = false;
                for (int j = i + 1; j < b.Count && b[j].isPartOfComposite; j++)
                {
                    if (!b[j].effectivePath.StartsWith("<Keyboard>")) continue;
                    clavier = true;
                    if (b[j].name == "left") axe.With("Negative", b[j].effectivePath);
                    else if (b[j].name == "right") axe.With("Positive", b[j].effectivePath);
                }
                if (clavier) composites.Add(i);
            }
            return action;
        }

        private void PasDeCote(bool oui)
        {
            if (oui == pasDeCote) return;
            pasDeCote = oui;
            foreach (int i in compositesClavier)
            {
                // la surcharge remplace aussi le chemin et les interactions : on garde ceux qui y sont (réaffectation)
                var b = moveAction.bindings[i];
                moveAction.ApplyBindingOverride(i, new InputBinding
                {
                    overridePath = b.overridePath, overrideInteractions = b.overrideInteractions,
                    overrideProcessors = oui ? null : "ScaleVector2(x=0,y=1)"
                });
            }
        }

        private bool Course()
        {
            Vector3 v = corps.linearVelocity;
            return v.x * v.x + v.z * v.z > vitesseCourse * vitesseCourse;
        }

        private void Cacher(bool oui)
        {
            if (oui == cache) return;
            cache = oui;
            foreach (var (rendu, mode) in rendusJoueur)
                if (rendu != null) rendu.shadowCastingMode = oui ? ShadowCastingMode.ShadowsOnly : mode;
        }

        private void Curseur(bool oui)
        {
            if (oui == capture) return;
            capture = oui;
            var souris = Mouse.current;
            if (oui && souris != null) positionCurseur = souris.position.ReadValue();
            Cursor.lockState = oui ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !oui;
            // comme WoW : au relâcher, le curseur revient où il était (le verrou l'a remis au centre)
            if (!oui && souris != null && positionCurseur != Vector2.zero) souris.WarpCursorPosition(positionCurseur);
        }
    }
}
