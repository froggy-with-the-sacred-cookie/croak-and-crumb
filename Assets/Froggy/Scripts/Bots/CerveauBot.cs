using Froggy.Deplacement;
using Froggy.Objets;
using Froggy.Regles;
using UnityEngine;
using UnityEngine.AI;

namespace Froggy.Bots
{
    /// <summary>
    /// Le cerveau d'une Froggy jouée par l'ordinateur (P3-05). Il ne triche pas : il « appuie » sur les mêmes commandes
    /// qu'un humain (DeplacementWow.Imposer : avancer, sauter ; PartieDash.Dasher : dasher ; NageFroggy.Imposer : plonger), se tourne comme
    /// la caméra tourne Froggy, et frappe par PartieTorgnoles.Frapper comme la touche V. Il subit donc la même physique,
    /// la pluie, les malus et les recharges.
    /// Toutes les « tempsReaction » secondes, il lit la partie et décide (ReglesBot) ; entre deux décisions, il suit son
    /// idée. Le chemin vient de la NavMesh que BotsPartie construit au lancement (la carte des endroits où l'on marche) ;
    /// sans elle, il va tout droit. Il saute quand le chemin monte ou quand il reste bloqué, et nage tout seul dans l'eau.
    /// </summary>
    [DefaultExecutionOrder(-50)]   // ses commandes sont posées avant le pas de DeplacementWow
    [RequireComponent(typeof(DeplacementWow))]
    public class CerveauBot : MonoBehaviour
    {
        [SerializeField] private ReglagesBot reglages;

        private DeplacementWow deplacement;
        private NageFroggy nage;
        private Rigidbody corps;
        private Collider boite;
        private PartieCookies partie;
        private PartieTorgnoles torgnoles;
        private PartieObjets objets;
        private PartieCookieSacre sacre;
        private DeroulementPartie deroulement;
        private System.Random alea;
        private float prochaineRecherche;

        private readonly SituationBot situation = new SituationBot();
        private NavMeshPath chemin;
        private int coin;
        private float prochainChemin, bloque, sautTenu, prochaineDecision, toutDroit;
        private Vector3 butDuChemin;
        private bool aUnChemin;

        /// <summary>Ce qu'il veut faire en ce moment (menu de debug, tests).</summary>
        public IntentionBot Intention { get; private set; }

        /// <summary>Nombre de dashs depuis le début (tests, réglages).</summary>
        public int Dashs { get; private set; }

        /// <summary>Nombre de torgnoles données depuis le début (tests, réglages).</summary>
        public int Torgnoles { get; private set; }

        public void Regler(ReglagesBot r, int graine)
        {
            reglages = r;
            alea = new System.Random(graine);
        }

        private void Awake()
        {
            deplacement = GetComponent<DeplacementWow>();
            nage = GetComponent<NageFroggy>();
            corps = GetComponent<Rigidbody>();
            boite = GetComponent<Collider>();
            chemin = new NavMeshPath();
            if (alea == null) alea = new System.Random(name.GetHashCode());
        }

        private void FixedUpdate() => Pas(Time.fixedDeltaTime, Time.time);

        /// <summary>Un pas, au pas physique : décider si c'est l'heure, puis appuyer sur les commandes.</summary>
        public void Pas(float dt, float maintenant)
        {
            if (reglages == null) reglages = ScriptableObject.CreateInstance<ReglagesBot>();
            Trouver(maintenant);
            if (nage == null) nage = GetComponent<NageFroggy>();
            if (nage != null) nage.Imposer(false);   // jamais la touche C d'un humain : la plongée vient d'ici

            if (maintenant >= prochaineDecision)
            {
                Lire();
                Intention = ReglesBot.Decider(situation, reglages, alea);
                prochaineDecision = maintenant + reglages.tempsReaction
                                    + (float)(alea.NextDouble() * 2.0 - 1.0) * reglages.variationReaction;
            }
            Agir(dt, maintenant);
        }

        // les composants de la partie arrivent avec le décor : on les cherche, sans le faire à chaque pas
        private void Trouver(float maintenant)
        {
            if (partie != null && maintenant < prochaineRecherche) return;
            prochaineRecherche = maintenant + 1f;
            if (partie == null) partie = FindAnyObjectByType<PartieCookies>();
            if (torgnoles == null) torgnoles = FindAnyObjectByType<PartieTorgnoles>();
            if (objets == null) objets = FindAnyObjectByType<PartieObjets>();
            if (sacre == null) sacre = FindAnyObjectByType<PartieCookieSacre>();
            if (deroulement == null) deroulement = FindAnyObjectByType<DeroulementPartie>();
        }

        /// <summary>Ce qu'il voit de la partie, recopié dans « situation ».</summary>
        private void Lire()
        {
            var s = situation;
            s.Vider();
            s.position = corps.position;
            s.avant = transform.forward;
            var moi = GetComponent<PorteurCookies>();
            if (moi != null) s.cookies = moi.Nombre;

            if (deroulement != null && deroulement.Regles != null)
            {
                var phase = deroulement.Regles.Phase;
                s.enJeu = phase == Phase.EnJeu || phase == Phase.Prolongation;
                s.porteSacre = moi != null && deroulement.PorteurCookieSacre == moi;
            }

            if (partie != null)
            {
                foreach (var p in partie.Porteurs)
                {
                    if (p == null || p == moi || !p.isActiveAndEnabled) continue;
                    s.rivaux.Add(new RivalVu
                    {
                        joueur = p.Joueur, position = p.transform.position, cookies = p.Nombre, intouchable = p.Intouchable,
                        porteSacre = deroulement != null && deroulement.PorteurCookieSacre == p,
                    });
                }
                var regles = partie.Regles;
                if (regles != null)
                {
                    for (int i = 0; i < regles.NombrePoints; i++)
                        if (regles.Present(i)) s.cookiesVisibles.Add(regles.Point(i));
                    for (int i = 0; i < regles.NombreAuSol; i++) s.cookiesVisibles.Add(regles.CookieAuSol(i).position);
                }
            }

            if (moi != null && torgnoles != null && torgnoles.Regles != null) s.torgnolePrete = torgnoles.Regles.Prete(moi.Joueur);
            s.dashPret = PartieDash.Actuel != null && PartieDash.Actuel.Pret(deplacement);

            var objet = GetComponent<PorteurObjet>();
            if (objet != null)
            {
                s.aUnObjet = objet.Fiche != null;
                s.objetPret = objet.Pret;
            }
            if (objets != null && objets.Regles != null)
                for (int i = 0; i < objets.Regles.NombrePoints; i++)
                    if (objets.Regles.Present(i)) s.boites.Add(objets.Regles.Point(i));

            if (sacre != null && sacre.Regles != null)
            {
                var etat = sacre.Regles.Etat;
                if (etat == EtatCookieSacre.Gobelin && sacre.Gobelin != null) s.gobelin = sacre.Gobelin.transform.position;
                else if (etat == EtatCookieSacre.AuSol) s.sacreAuSol = sacre.Regles.PositionAuSol;
            }
        }

        private void Agir(float dt, float maintenant)
        {
            var intention = Intention;
            var entrees = new DeplacementWow.Entrees();
            Vector3 position = corps.position;
            bool face = false;

            if (intention.sorte != IntentionBot.Sorte.Attendre)
            {
                Vector3 but = intention.sorte == IntentionBot.Sorte.Fuir ? ReglesBot.PointDeFuite(position, intention.but) : intention.but;
                Vector3 vers = Suivre(position, but, maintenant, out float hauteur);
                vers.y = 0f;
                if (vers.sqrMagnitude > 0.09f)
                {
                    float voulu = Mathf.Atan2(vers.x, vers.z) * Mathf.Rad2Deg;
                    float ecart = Mathf.DeltaAngle(corps.rotation.eulerAngles.y, voulu);
                    float tourne = Mathf.Clamp(ecart, -reglages.vitesseRotation * dt, reglages.vitesseRotation * dt);
                    corps.rotation = Quaternion.Euler(0f, corps.rotation.eulerAngles.y + tourne, 0f);
                    entrees.deplacement = new Vector2(0f, Mathf.Abs(ecart) < 60f ? 1f : 0.2f);   // il ralentit pour tourner
                    face = Mathf.Abs(ecart) < 25f;

                    // sauter : le chemin monte juste devant, ou il est bloqué (rebord, rive, nénuphar)
                    float vitesse = new Vector3(corps.linearVelocity.x, 0f, corps.linearVelocity.z).magnitude;
                    bloque = vitesse < 0.6f ? bloque + dt : 0f;
                    bool monte = hauteur > 0.4f && vers.magnitude < 2f;
                    if ((monte || bloque > 0.5f) && (deplacement.AuSol || (nage != null && nage.DansLEau)))
                    {
                        entrees.sautAppuye = true;
                        sautTenu = 0.3f;
                        bloque = 0f;
                    }
                }
            }
            sautTenu -= dt;
            entrees.sautTenu = sautTenu > 0f;
            deplacement.Imposer(entrees);

            if (face) Dasher(intention);
            Frapper(intention);
            if (intention.utiliserObjet) UtiliserObjet();
        }

        // le dash (#134) part devant lui, vers où il va : seulement une fois tourné vers son chemin, un par décision
        private void Dasher(IntentionBot intention)
        {
            if (!intention.dash || PartieDash.Actuel == null) return;
            if (!PartieDash.Actuel.Dasher(deplacement)) return;
            Dashs++;
            var i = Intention;
            i.dash = false;
            Intention = i;
        }

        private void Frapper(IntentionBot intention)
        {
            if (!intention.frapper || torgnoles == null) return;
            var moi = GetComponent<PorteurCookies>();
            if (moi == null) return;
            // il se tourne d'abord vers sa cible : la torgnole part devant lui
            Vector3 vers = intention.but - corps.position;
            vers.y = 0f;
            if (vers.sqrMagnitude > 1e-4f && Vector3.Angle(transform.forward, vers) > 45f) return;
            if (torgnoles.Regles != null && !torgnoles.Regles.Prete(moi.Joueur)) return;
            torgnoles.Frapper(moi);
            Torgnoles++;
            var i = Intention;
            i.frapper = false;   // une torgnole par décision
            Intention = i;
        }

        private void UtiliserObjet()
        {
            var objet = GetComponent<PorteurObjet>();
            if (objets == null || objet == null || !objet.Pret) return;
            objets.Utiliser(objet);
        }

        /// <summary>
        /// La direction à prendre vers « but » : vers le prochain coin du chemin de la NavMesh (recalculé deux fois par
        /// seconde, ou quand le but bouge), sinon tout droit. « hauteur » : de combien ce coin est plus haut que ses pieds.
        /// </summary>
        private Vector3 Suivre(Vector3 position, Vector3 but, float maintenant, out float hauteur)
        {
            float pieds = boite != null ? boite.bounds.min.y : position.y;
            hauteur = 0f;
            if (maintenant < toutDroit) return but - position;
            if (maintenant >= prochainChemin || (but - butDuChemin).sqrMagnitude > 1f)
            {
                prochainChemin = maintenant + 0.5f;
                butDuChemin = but;
                aUnChemin = Chemin(position, but);
                coin = 1;
            }
            if (!aUnChemin) return but - position;

            var coins = chemin.corners;
            while (coin < coins.Length - 1 && ReglesBot.Distance(position, coins[coin]) < 0.5f) coin++;
            if (coin >= coins.Length) return but - position;
            // au bout du chemin sans être arrivé : le but est hors de la carte (sous un arbre, dont la carte ne garde
            // qu'une boîte) ; tout droit, la vraie physique décide, et il saute s'il bute (essai du 18 novembre 2026)
            // gardé 2 s : sinon, à peine sorti de la carte, le chemin recalculé le ramènerait à son bord (va-et-vient)
            if (coin == coins.Length - 1 && ReglesBot.Distance(position, coins[coin]) < 0.6f)
            {
                toutDroit = maintenant + 2f;
                return but - position;
            }
            hauteur = coins[coin].y - pieds;
            return coins[coin] - position;
        }

        private bool Chemin(Vector3 depuis, Vector3 vers)
        {
            if (!NavMesh.SamplePosition(depuis, out var a, 2f, NavMesh.AllAreas)) return false;
            if (!NavMesh.SamplePosition(vers, out var b, 3f, NavMesh.AllAreas)) return false;
            return NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, chemin)
                   && chemin.status != NavMeshPathStatus.PathInvalid && chemin.corners.Length > 1;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;
            Gizmos.color = Intention.sorte == IntentionBot.Sorte.Fuir ? Color.yellow : Intention.frapper ? Color.red : Color.cyan;
            Gizmos.DrawLine(transform.position, Intention.but);
            if (aUnChemin)
                for (int i = 1; i < chemin.corners.Length; i++) Gizmos.DrawLine(chemin.corners[i - 1], chemin.corners[i]);
        }
#endif
    }
}
