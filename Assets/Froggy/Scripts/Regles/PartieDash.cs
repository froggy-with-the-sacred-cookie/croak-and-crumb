using System.Collections.Generic;
using Froggy.Deplacement;
using Froggy.Socle;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Froggy.Regles
{
    /// <summary>
    /// Le dash dans le jeu (#134), à la place du sprint : Maj gauche ou R1 (l'action « Dash » de l'Input System, lue sur
    /// le joueur de cette machine). Il s'installe seul au lancement, comme le HUD : partout où il y a une Froggy (vitrine,
    /// scènes de test, partie complète), sans toucher à aucune scène.
    ///
    /// - Le geste est dans DeplacementWow.Dasher (au sol seulement, 4 m en 0,2 s) ; la recharge et l'assommement dans
    ///   ReglesDash ; les valeurs dans ReglagesDash (Resources/ReglagesDash).
    /// - Une Froggy en dash bouscule celle qu'elle percute (recul, son dash s'arrête) ; deux Froggy en dash qui se
    ///   touchent sont assommées toutes les deux (ni course, ni saut, ni torgnole, ni objet : Modificateurs). Une Froggy
    ///   intouchable (tombée, tout juste réapparue) ne subit rien.
    /// - Un bot dashe par Dasher(froggy), comme il frappe par PartieTorgnoles.Frapper.
    /// - Pas encore en réseau : chez chacun, sa propre Froggy dashe (sa position part avec elle) ; la bousculade et
    ///   l'assommement se décideront chez l'hôte avec la #128, comme la torgnole.
    /// </summary>
    public class PartieDash : MonoBehaviour
    {
        private const float Recherche = 0.5f;

        /// <summary>Le dash de cette partie (celui qui s'est installé au lancement).</summary>
        public static PartieDash Actuel { get; private set; }

        private ReglagesDash reglages;
        private PartieCookies cookies;
        private readonly List<DeplacementWow> froggys = new List<DeplacementWow>();
        private readonly Dictionary<DeplacementWow, int> ids = new Dictionary<DeplacementWow, int>();
        private readonly Dictionary<DeplacementWow, Transform> etoiles = new Dictionary<DeplacementWow, Transform>();
        private readonly List<DeplacementWow> aRelever = new List<DeplacementWow>();
        private int prochainId = 1000;
        private bool listeFixe;
        private float prochaineRecherche;
        private static Material matiereEtoile;

        public ReglesDash Regles { get; private set; }
        public ReglagesDash Reglages => reglages;

        /// <summary>L'action « Dash » du joueur de cette machine (le HUD y lit la touche), ou null.</summary>
        public InputAction ActionDash { get; private set; }

        /// <summary>Nombre de bousculades et d'assommements depuis le lancement (tests, réglages).</summary>
        public int Bousculades { get; private set; }
        public int Assommements { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reinitialiser()
        {
            Actuel = null;
            matiereEtoile = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Installer()
        {
            if (Actuel != null) return;
            var objet = new GameObject("Dash");
            DontDestroyOnLoad(objet);
            objet.AddComponent<PartieDash>();
        }

        private void Awake()
        {
            if (Actuel == null) Actuel = this;
            Preparer();
            EvenementsPartie.Emis += QuandEvenement;
        }

        private void OnDestroy()
        {
            EvenementsPartie.Emis -= QuandEvenement;
            foreach (var f in new List<DeplacementWow>(etoiles.Keys)) Relever(f);
            if (Actuel == this) Actuel = null;
        }

        private void Preparer()
        {
            if (Regles != null) return;
            if (reglages == null) reglages = Resources.Load<ReglagesDash>("ReglagesDash");
            if (reglages == null) reglages = ScriptableObject.CreateInstance<ReglagesDash>();
            Regles = new ReglesDash(reglages.recharge, reglages.dureeAssomme);
        }

        /// <summary>Pour un test : des réglages à soi et une liste de Froggy fixe (pas de recherche dans les scènes).</summary>
        public void Regler(ReglagesDash r, IEnumerable<DeplacementWow> liste)
        {
            reglages = r;
            Regles = null;
            Preparer();
            froggys.Clear();
            froggys.AddRange(liste);
            listeFixe = true;
        }

        // une partie qui (re)commence : tout le monde debout, dash prêt
        private void QuandEvenement(EvenementPartie e)
        {
            if (e.Type != TypeEvenement.PartieCommencee) return;
            Regles?.Vider();
            foreach (var f in new List<DeplacementWow>(etoiles.Keys)) Relever(f);
        }

        private void Update()
        {
            if (!listeFixe && Time.unscaledTime >= prochaineRecherche)
            {
                prochaineRecherche = Time.unscaledTime + Recherche;
                froggys.Clear();
                froggys.AddRange(FindObjectsByType<DeplacementWow>(FindObjectsInactive.Exclude));
                if (cookies == null) cookies = FindAnyObjectByType<PartieCookies>();
            }

            DeplacementWow local = JoueurLocal();
            ActionDash = local != null ? local.GetComponent<PlayerInput>().actions.FindAction("Dash") : null;
            if (ActionDash != null && ActionDash.WasPressedThisFrame()) Dasher(local);

            // les étoiles tournent au-dessus des assommés
            foreach (var kv in etoiles)
                if (kv.Value != null) kv.Value.Rotate(0f, 360f * Time.deltaTime, 0f, Space.World);
        }

        private void FixedUpdate() => Pas(Time.fixedDeltaTime);

        /// <summary>La Froggy du joueur de cette machine : un PlayerInput allumé (les bots en gardent un, éteint).</summary>
        public DeplacementWow JoueurLocal()
        {
            foreach (var f in froggys)
            {
                if (f == null || !f.isActiveAndEnabled) continue;
                var entree = f.GetComponent<PlayerInput>();
                if (entree != null && entree.enabled && entree.actions != null) return f;
            }
            return null;
        }

        /// <summary>Le numéro d'une Froggy pour les règles : son numéro de joueur, sinon un numéro à elle.</summary>
        public int Cle(DeplacementWow froggy)
        {
            var porteur = froggy.GetComponent<PorteurCookies>();
            if (porteur != null && porteur.Joueur > 0) return porteur.Joueur;
            if (!ids.TryGetValue(froggy, out int id)) ids[froggy] = id = prochainId++;
            return id;
        }

        /// <summary>Le temps avant le prochain dash de cette Froggy ; 0 : prêt.</summary>
        public float Reste(DeplacementWow froggy)
        {
            Preparer();
            return froggy != null ? Regles.Reste(Cle(froggy)) : 0f;
        }

        /// <summary>Vrai si cette Froggy peut dasher maintenant (recharge finie, debout, au sol, partie en cours).</summary>
        public bool Pret(DeplacementWow froggy)
        {
            Preparer();
            return froggy != null && froggy.AuSol && !froggy.EnDash && Regles.Pret(Cle(froggy)) && !EnPause() && !Entravee(froggy);
        }

        private bool EnPause() => cookies != null && cookies.EnPause;   // compte à rebours, fin de partie

        private static bool Entravee(DeplacementWow froggy)
        {
            var m = froggy.GetComponent<Modificateurs>();
            return m != null && m.Entrave;
        }

        /// <summary>
        /// « froggy » dashe vers où elle va (devant elle sans direction). Faux si rien ne part : recharge, assommée ou
        /// gelée, en l'air, dans l'eau, partie arrêtée. C'est ce qu'un bot ou, plus tard, l'hôte appelle.
        /// </summary>
        public bool Dasher(DeplacementWow froggy)
        {
            Preparer();
            if (froggy == null || !froggy.isActiveAndEnabled || EnPause() || Entravee(froggy)) return false;
            int cle = Cle(froggy);
            if (!Regles.Dasher(cle)) return false;
            if (!froggy.Dasher(froggy.DirectionVoulue, reglages.distance, reglages.duree))
            {
                Regles.Rendre(cle);   // refusé par le contrôleur (en l'air, dans l'eau) : la recharge n'a pas commencé
                return false;
            }
            froggy.SendMessage("Jouer", "Dash", SendMessageOptions.DontRequireReceiver);   // le geste du modèle, s'il en a un
            return true;
        }

        /// <summary>Un pas, au pas physique (un test l'appelle hors jeu) : recharges, relevés, chocs.</summary>
        public void Pas(float dt)
        {
            Preparer();
            Regles.Pas(dt);

            aRelever.Clear();
            foreach (var f in etoiles.Keys)
                if (f == null || !Regles.Assomme(Cle(f))) aRelever.Add(f);
            foreach (var f in aRelever) Relever(f);

            for (int i = 0; i < froggys.Count; i++)
            {
                var a = froggys[i];
                if (a == null || !a.EnDash) continue;
                for (int j = 0; j < froggys.Count; j++)
                {
                    var b = froggys[j];
                    if (j == i || b == null || !b.isActiveAndEnabled) continue;
                    Vector3 entre = b.transform.position - a.transform.position;
                    entre.y = 0f;
                    if (entre.magnitude > reglages.rayonChoc) continue;
                    // devant elle : on ne percute pas ce qu'on laisse derrière soi
                    if (entre.sqrMagnitude > 1e-4f && Vector3.Dot(entre.normalized, a.DirectionDash) < 0.2f) continue;

                    var porteur = b.GetComponent<PorteurCookies>();
                    ChocDash choc = ReglesDash.Choc(b.EnDash, porteur != null && porteur.Intouchable);
                    if (choc == ChocDash.Rien) continue;
                    if (choc == ChocDash.DoubleAssomme)
                    {
                        a.ArreterDash();
                        b.ArreterDash();
                        Assommer(a);
                        Assommer(b);
                        Assommements++;
                    }
                    else
                    {
                        Vector3 sens = a.DirectionDash;
                        a.ArreterDash();
                        Bousculer(b, sens);
                        Bousculades++;
                    }
                    break;   // un choc par dash
                }
            }
        }

        // le recul dit « bousculé » (skill impact-feedback) ; un corps cinématique (une Froggy d'un autre joueur) ne bouge pas
        private void Bousculer(DeplacementWow cible, Vector3 sens)
        {
            var corps = cible.GetComponent<Rigidbody>();
            if (corps == null || corps.isKinematic || reglages.recul <= 0f) return;
            Vector3 v = corps.linearVelocity;
            corps.linearVelocity = new Vector3(sens.x * reglages.recul, Mathf.Max(v.y, 0f), sens.z * reglages.recul);
        }

        private void Assommer(DeplacementWow froggy)
        {
            Regles.Assommer(Cle(froggy));
            var m = froggy.GetComponent<Modificateurs>();
            if (m == null) m = froggy.gameObject.AddComponent<Modificateurs>();
            m.Poser(this, 0f, 0f);   // ni course ni saut
            m.Entraver(this);        // ni torgnole, ni objet, ni dash
            if (!etoiles.ContainsKey(froggy)) etoiles[froggy] = reglages.visuelProvisoire ? Etoiles(froggy) : null;
            froggy.SendMessage("Jouer", "Assomme", SendMessageOptions.DontRequireReceiver);
        }

        private void Relever(DeplacementWow froggy)
        {
            if (etoiles.TryGetValue(froggy, out var e) && e != null) Destroy(e.gameObject);
            etoiles.Remove(froggy);
            if (froggy == null) return;
            var m = froggy.GetComponent<Modificateurs>();
            if (m == null) return;
            m.Retirer(this);
            m.Desentraver(this);
        }

        /// <summary>PROVISOIRE : trois petites étoiles qui tournent au-dessus de la tête, en attendant l'animation (P7).</summary>
        private static Transform Etoiles(DeplacementWow froggy)
        {
            var centre = new GameObject("Assommée (provisoire)").transform;
            centre.SetParent(froggy.transform, false);
            centre.localPosition = Vector3.up * 1.4f;
            for (int i = 0; i < 3; i++)
            {
                var etoile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                DestroyImmediate(etoile.GetComponent<Collider>());   // jamais un obstacle
                etoile.name = "Étoile";
                etoile.transform.SetParent(centre, false);
                float angle = i * 120f * Mathf.Deg2Rad;
                etoile.transform.localPosition = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.35f;
                etoile.transform.localScale = Vector3.one * 0.14f;
                if (matiereEtoile == null)
                {
                    var shader = Shader.Find("Sprites/Default");
                    if (shader != null) matiereEtoile = new Material(shader) { color = new Color(1f, 0.86f, 0.25f, 1f) };
                }
                if (matiereEtoile != null) etoile.GetComponent<Renderer>().sharedMaterial = matiereEtoile;
            }
            return centre;
        }
    }
}
