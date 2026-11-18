using Froggy.Socle;
using Froggy.Template;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Froggy.Deplacement
{
    /// <summary>
    /// Déplacement à la World of Warcraft (proposition pour la vitrine, 28 octobre 2026), qui va avec CameraOrbitale :
    /// - avant / arrière suivent le cap de Froggy (sa rotation, que la caméra fait tourner) ; en reculant, Froggy reste
    ///   face à l'avant et va moins vite ;
    /// - pas de côté par Q / E (place physique : A / E en AZERTY), ou par gauche / droite clic droit tenu (la caméra coupe
    ///   la composante latérale de Move sinon, et s'en sert pour tourner) ;
    /// - les deux boutons de la souris : en avant ; Verr. num : course automatique, qu'avant ou arrière arrête ;
    /// - en diagonale, Froggy se tourne à demi vers sa course.
    /// Course, saut et tolérances (coyote time, tampon, saut variable) : les mêmes que TemplatePlayerController, avec les
    /// mêmes réglages ; seul le repère change. Pas de sprint : le dash le remplace (#134, décision de Florent du
    /// 18 novembre 2026), un geste au sol que PartieDash déclenche (Dasher) avec sa recharge. Le remplace tant qu'il est actif (CameraOrbitale l'active et le
    /// coupe avec elle). La logique tient dans Pas(), appelée au pas physique, pour que les tests la fassent tourner hors jeu.
    /// Le composant Modificateurs de Froggy (socle commun, T-01 ; ajouté ici s'il manque) multiplie la vitesse et la hauteur du saut.
    /// Sur un nénuphar (#16), il le signale à chaque pas (le dessin du nénuphar s'enfonce) et son propre dessin le suit ;
    /// la physique ne bouge pas : le collider du nénuphar reste où il est.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
    public class DeplacementWow : MonoBehaviour
    {
        /// <summary>Les entrées d'un pas : x pas de côté, y avant / arrière.</summary>
        public struct Entrees { public Vector2 deplacement; public bool sautAppuye, sautTenu; }

        [SerializeField] private TemplateMovementSettings reglages;
        [SerializeField] private ReglagesWow reglagesWow;
        [Tooltip("Partie visible, tournée vers le cap (vide : l'enfant « Visual »).")]
        [SerializeField] private Transform visuel;
        [SerializeField] private float vitesseRotationVisuel = 720f;

        private Rigidbody corps;
        private BoxCollider boite;
        private TemplatePlayerController ancien;
        private Modificateurs modificateurs;
        private NageFroggy nage;
        private InputAction moveAction, jumpAction, pasDeCote, courseAuto;
        private bool pret;

        // entrées captées dans Update (ou imposées par un test), état au pas physique
        [System.NonSerialized] private Entrees entrees;
        [System.NonSerialized] private bool imposees, sautAppuye, auto;
        [System.NonSerialized] private float coyote, tampon;
        [System.NonSerialized] private bool montee, etaitAuSol;
        // le dessin posé sur le dessin d'un nénuphar : descendu de tant, en m (rien ne bouge côté physique)
        [System.NonSerialized] private float decalageVisuel;
        [System.NonSerialized] private Vector3 baseVisuel;
        [System.NonSerialized] private bool baseVisuelLue;
        // en l'air, le dessin revient à la place du corps à cette vitesse, en m/s (le saut le cache)
        private const float RetourVisuel = 1.5f;
        // le dash en cours (#134) : sa vitesse (dérivée de la distance et de la durée) et le temps qui lui reste
        [System.NonSerialized] private Vector3 vitesseDash;
        [System.NonSerialized] private float resteDash;
        [System.NonSerialized] private bool finDash;

        public bool AuSol { get; private set; }
        public bool CourseAuto => auto;
        /// <summary>Le collider sur lequel Froggy se tient (ou presque : à groundCheckDistance près), sinon null.</summary>
        public Collider SolTouche { get; private set; }
        /// <summary>
        /// Froggy vient de se poser, avec le sol touché : au pas physique où il redevient « au sol », même s'il repart
        /// aussitôt par le tampon de saut (il n'a alors jamais touché le sol physiquement). Parcours de test, sons, poussière…
        /// </summary>
        public event System.Action<Collider> Atterrissage;

        /// <summary>Une impulsion de saut a réellement été appliquée.</summary>
        public event System.Action ImpulsionSaut;

        /// <summary>Un dash vient de partir (son, effet, animation).</summary>
        public event System.Action DashParti;

        /// <summary>Vrai pendant un dash.</summary>
        public bool EnDash => resteDash > 0f;

        /// <summary>La direction du dash en cours (horizontale, unitaire).</summary>
        public Vector3 DirectionDash => vitesseDash.sqrMagnitude > 0f ? vitesseDash.normalized : transform.forward;

        /// <summary>Là où Froggy veut aller (entrées et cap), à plat ; devant elle si on ne touche à rien.</summary>
        public Vector3 DirectionVoulue
        {
            get
            {
                Initialiser();
                Vector3 d = reglages != null && reglagesWow != null
                    ? Vitesse(entrees.deplacement, corps.rotation.eulerAngles.y, reglages, reglagesWow) : Vector3.zero;
                d.y = 0f;
                if (d.sqrMagnitude < 1e-4f) d = transform.forward;
                d.y = 0f;
                return d.sqrMagnitude > 1e-6f ? d.normalized : Vector3.forward;
            }
        }

        /// <summary>
        /// Lance un dash de « distance » m en « duree » s vers « direction » (la vitesse en découle). Refusé (faux) en
        /// l'air, dans l'eau, déjà en dash, ou quand un modificateur arrête Froggy (gelée, assommée) : au sol seulement,
        /// pour que les portées de saut ne changent pas (choix de Florent, #134). La recharge est l'affaire de PartieDash.
        /// </summary>
        public bool Dasher(Vector3 direction, float distance, float duree)
        {
            Initialiser();
            if (!AuSol || EnDash || distance <= 0f || duree <= 0f) return false;
            if (nage != null && nage.DansLEau) return false;
            if (modificateurs.Vitesse <= 0f) return false;
            direction.y = 0f;
            if (direction.sqrMagnitude < 1e-4f) direction = transform.forward;
            direction.y = 0f;
            vitesseDash = direction.normalized * (distance / duree);   // la distance se donne, la vitesse se dérive
            resteDash = duree;
            finDash = false;
            DashParti?.Invoke();
            return true;
        }

        /// <summary>Arrête le dash en cours (un choc) : Froggy repart à la vitesse de course au plus.</summary>
        public void ArreterDash()
        {
            if (!EnDash) return;
            resteDash = 0f;
            finDash = true;
        }

        private void Initialiser()
        {
            if (pret) return;
            pret = true;
            corps = GetComponent<Rigidbody>();
            boite = GetComponent<BoxCollider>();
            ancien = GetComponent<TemplatePlayerController>();
            // chaque Froggy en a un, pour que les autres postes le trouvent sans toucher au préfabriqué
            modificateurs = GetComponent<Modificateurs>();
            if (modificateurs == null) modificateurs = gameObject.AddComponent<Modificateurs>();
            // la nage (P1-02) : sans effet tant que Froggy n'est pas dans une ZoneEau
            nage = GetComponent<NageFroggy>();
            if (nage == null) nage = gameObject.AddComponent<NageFroggy>();
            if (visuel == null) visuel = transform.Find("Visual");
            var entree = GetComponent<PlayerInput>();
            if (entree != null)
            {
                moveAction = entree.actions["Move"];
                jumpAction = entree.actions["Jump"];
            }
            pasDeCote = new InputAction("PasDeCote", InputActionType.Value);
            pasDeCote.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/q").With("Positive", "<Keyboard>/e");
            courseAuto = new InputAction("CourseAuto", InputActionType.Button, "<Keyboard>/numLock");
        }

        private void OnEnable()
        {
            Initialiser();
            if (ancien != null) ancien.enabled = false;
            pasDeCote.Enable();
            courseAuto.Enable();
        }

        private void OnDisable()
        {
            pasDeCote.Disable();
            courseAuto.Disable();
            auto = false;
            if (ancien != null) ancien.enabled = true;
        }

        /// <summary>Pour un test : les entrées viennent d'ici, plus des appareils.</summary>
        public void Imposer(Entrees e)
        {
            imposees = true;
            entrees = e;
            if (e.sautAppuye) sautAppuye = true;
        }

        /// <summary>Pour un test : les réglages (des copies, jamais les assets partagés).</summary>
        public void Regler(TemplateMovementSettings course, ReglagesWow wow)
        {
            reglages = course;
            reglagesWow = wow;
        }

        private void Update()
        {
            if (!imposees && moveAction != null)
            {
                var souris = Froggy.Cameras.MenuCamera.Ouvert ? null : Mouse.current;
                bool deuxBoutons = souris != null && souris.leftButton.isPressed && souris.rightButton.isPressed;
                Vector2 m = moveAction.ReadValue<Vector2>();
                if (courseAuto.WasPressedThisFrame()) auto = !auto;
                else if (Mathf.Abs(m.y) > 0.1f) auto = false;
                entrees.deplacement = new Vector2(Mathf.Clamp(m.x + pasDeCote.ReadValue<float>(), -1f, 1f),
                                                  deuxBoutons || auto ? 1f : m.y);
                entrees.sautTenu = jumpAction.IsPressed();
                if (jumpAction.WasPressedThisFrame()) sautAppuye = true;
            }
            Dessiner(Time.deltaTime);
        }

        /// <summary>Le dessin, à chaque image (ou dans un test) : tourné vers le cap, posé sur le dessin du nénuphar.</summary>
        public void Dessiner(float dt)
        {
            Initialiser();
            TournerVisuel(dt);
            SuivreNenuphar(dt);
        }

        private void SuivreNenuphar(float dt)
        {
            if (visuel == null) return;
            if (!baseVisuelLue)
            {
                baseVisuel = visuel.localPosition;
                baseVisuelLue = true;
            }
            var nenuphar = AuSol ? Nenuphar.Sous(SolTouche) : null;
            float avant = decalageVisuel;
            decalageVisuel = nenuphar != null ? nenuphar.DecalageEn(transform.position)
                                              : Mathf.MoveTowards(decalageVisuel, 0f, RetourVisuel * dt);
            if (decalageVisuel != 0f || avant != 0f)   // hors des nénuphars, on ne touche pas au dessin
                visuel.localPosition = baseVisuel + Vector3.down * decalageVisuel;
        }

        private void FixedUpdate() => Pas(Time.fixedDeltaTime);

        /// <summary>Un pas physique : sol, tolérances, course dans le repère du cap, saut.</summary>
        public void Pas(float dt)
        {
            Initialiser();
            // l'appui se consomme et s'efface à chaque pas, même sans effet
            if (sautAppuye) tampon = reglages.jumpBufferTime;
            sautAppuye = false;

            Vector3 v = corps.linearVelocity;
            // juste après un dash (fini ou coupé) : on repart à la vitesse de course au plus, sans glisser plus loin
            if (finDash)
            {
                finDash = false;
                Vector3 h = Vector3.ClampMagnitude(new Vector3(v.x, 0f, v.z), reglages.moveSpeed * Mathf.Max(0f, modificateurs.Vitesse));
                v.x = h.x;
                v.z = h.z;
            }

            // dans l'eau, la nage prend la main (P1-02) ; un saut demandé peut servir à ressortir
            if (nage != null && nage.Nager(dt, transform.TransformPoint(boite.center), entrees.deplacement, tampon > 0f,
                    reglages, reglagesWow, modificateurs, corps, out bool sautUtilise))
            {
                AuSol = etaitAuSol = montee = false;
                SolTouche = null;
                coyote = 0f;
                resteDash = 0f;   // pas de dash dans l'eau
                finDash = false;
                if (sautUtilise) tampon = 0f;
                tampon -= dt;
                return;
            }

            AuSol = v.y <= 0.01f && Sol();   // en montée, on ne se recolle pas au sol
            // annoncé avant le saut éventuel du même pas : un saut enchaîné par le tampon part sans contact physique
            bool pose = AuSol && !etaitAuSol;
            if (pose) Atterrissage?.Invoke(SolTouche);
            etaitAuSol = AuSol;
            // le nénuphar sous les pieds le sait (son dessin s'enfonce, plonge d'autant plus qu'on tombe vite)
            if (AuSol)
            {
                var nenuphar = Nenuphar.Sous(SolTouche);
                if (nenuphar != null) nenuphar.Porter(this, transform.position, pose ? -v.y : 0f);
            }
            coyote = AuSol ? reglages.coyoteTime : coyote - dt;
            tampon -= dt;
            if (AuSol) montee = false;

            // météo, objets, cookies portés : lus au pas physique, sans savoir qui les a posés
            float facteurVitesse = modificateurs.Vitesse, facteurSaut = modificateurs.Saut;

            Vector3 voulue = Vitesse(entrees.deplacement, corps.rotation.eulerAngles.y, reglages, reglagesWow) * facteurVitesse;
            float acceleration = AuSol ? reglages.groundAcceleration : reglagesWow.elanFigeEnLair ? 0f : reglages.airAcceleration;
            Vector3 horizontal = Vector3.MoveTowards(new Vector3(v.x, 0f, v.z), voulue, acceleration * dt);

            bool saute = tampon > 0f && coyote > 0f;

            // le dash (#134) : une vitesse imposée, au sol. Sauter ou quitter le sol le coupe, à la vitesse de course au
            // plus : il ne rallonge jamais un saut (les portées des nénuphars et du Bâtisseur ne changent pas).
            if (resteDash > 0f)
            {
                if (saute || !AuSol)
                {
                    horizontal = Vector3.ClampMagnitude(vitesseDash, reglages.moveSpeed * facteurVitesse);
                    resteDash = 0f;
                }
                else
                {
                    float part = Mathf.Min(dt, resteDash);
                    horizontal = vitesseDash * (part / dt);   // le dernier pas n'avance que ce qui reste : la distance pile
                    resteDash -= dt;
                    if (resteDash <= 0f) finDash = true;
                }
            }

            // la hauteur se donne, la vitesse se dérive
            if (saute)
            {
                v.y = Mathf.Sqrt(2f * -Physics.gravity.y * reglages.jumpHeight * facteurSaut);
                tampon = coyote = 0f;
                montee = true;
                AuSol = false;
            }
            // saut variable : relâcher coupe la montée, une fois, et seulement ce qui vient d'un saut
            if (montee && !entrees.sautTenu && v.y > 0f)
            {
                v.y *= reglages.jumpCutMultiplier;
                montee = false;
            }
            if (v.y <= 0f) montee = false;

            corps.linearVelocity = new Vector3(horizontal.x, v.y, horizontal.z);
            if (saute) ImpulsionSaut?.Invoke();
        }

        /// <summary>La vitesse voulue au sol : x pas de côté, y avant / arrière, dans le repère du cap ; recul plus lent.</summary>
        public static Vector3 Vitesse(Vector2 entree, float cap, TemplateMovementSettings course, ReglagesWow wow)
        {
            entree = Vector2.ClampMagnitude(entree, 1f);
            float vitesse = course.moveSpeed * (entree.y < -0.01f ? wow.facteurRecul : 1f);
            return Quaternion.Euler(0f, cap, 0f) * new Vector3(entree.x, 0f, entree.y) * vitesse;
        }

        /// <summary>De combien Froggy se tourne par rapport à son cap : à demi vers sa course en diagonale, face à l'avant sinon.</summary>
        public static float Orientation(Vector2 entree, float angleDiagonale)
        {
            if (Mathf.Abs(entree.x) < 0.1f || Mathf.Abs(entree.y) < 0.1f) return 0f;
            return angleDiagonale * Mathf.Sign(entree.x) * Mathf.Sign(entree.y);
        }

        private void TournerVisuel(float dt)
        {
            if (visuel == null || reglagesWow == null) return;
            var voulue = Quaternion.Euler(0f, transform.eulerAngles.y + Orientation(entrees.deplacement, reglagesWow.angleDiagonale), 0f);
            visuel.rotation = Quaternion.RotateTowards(visuel.rotation, voulue, vitesseRotationVisuel * dt);
        }

        private bool Sol()
        {
            // une boîte un peu plus étroite que le collider, lancée vers le bas ; elle ne voit pas le collider qu'elle
            // chevauche au départ (le sien). La scène physique du joueur : celle du jeu, ou celle d'un test.
            Vector3 taille = Vector3.Scale(boite.size, transform.lossyScale);
            Vector3 demi = new Vector3(taille.x * 0.45f, 0.05f, taille.z * 0.45f);
            float distance = taille.y * 0.5f - demi.y + reglages.groundCheckDistance;
            bool sol = gameObject.scene.GetPhysicsScene().BoxCast(transform.TransformPoint(boite.center), demi, Vector3.down,
                out var touche, transform.rotation, distance, reglages.groundLayers, QueryTriggerInteraction.Ignore);
            SolTouche = sol ? touche.collider : null;
            return sol;
        }
    }
}
