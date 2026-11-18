using System.Collections.Generic;
using Froggy.Socle;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Froggy.Regles
{
    /// <summary>
    /// La torgnole dans une scène (P4-10), sans réseau : le coup de poing de base, sans objet. Le joueur local frappe avec V
    /// (ou le bouton ouest de la manette) ; le joueur touché lâche un cookie par PartieCookies.FaireLacher, à l'opposé du
    /// frappeur, et recule. Un joueur intouchable (P3-04) ne lâche rien et ne recule pas. Les règles sont dans ReglesTorgnole.
    /// Le coup joue le geste « Torgnole » du modèle affiché (FroggyApparence.Jouer) et émet « coup donné ». Le cookie sacré
    /// écoute cet événement : le porteur touché le lâche aussi, même sans autre cookie (PartieCookieSacre, P5-08).
    /// Pas encore : le gel d'image et la secousse (il n'y a pas encore de chemin unique pour eux), le son. Les touches
    /// viennent d'ici en attendant les options (P6-05) : V (F est à l'objet, le clic gauche à la caméra).
    /// </summary>
    public class PartieTorgnoles : MonoBehaviour
    {
        [SerializeField] private ReglagesTorgnole reglages;
        [SerializeField] private PartieCookies partie;
        [Tooltip("Affiche la recharge du joueur local, en attendant le HUD (P6-02).")]
        [SerializeField] private bool affichageProvisoire = true;

        private readonly List<(int, Vector3)> autres = new List<(int, Vector3)>();
        private InputAction frappe;
        private GUIStyle style;
        private string dernier = "";

        public ReglesTorgnole Regles { get; private set; }

        /// <summary>Pour un test : des réglages et une partie de cookies à soi.</summary>
        public void Regler(ReglagesTorgnole r, PartieCookies p)
        {
            reglages = r;
            partie = p;
            Regles = null;
        }

        private void OnEnable()
        {
            frappe = new InputAction("Torgnole", InputActionType.Button);
            frappe.AddBinding("<Keyboard>/v");
            frappe.AddBinding("<Gamepad>/buttonWest");
            frappe.Enable();
        }

        private void OnDisable()
        {
            frappe?.Disable();
            frappe?.Dispose();
            frappe = null;
        }

        private void Preparer()
        {
            if (Regles != null) return;
            if (reglages == null) reglages = ScriptableObject.CreateInstance<ReglagesTorgnole>();
            if (partie == null) partie = FindAnyObjectByType<PartieCookies>();
            Regles = new ReglesTorgnole(reglages.recharge, reglages.portee, reglages.demiAngle, reglages.hauteur);
        }

        private void Update()
        {
            if (frappe != null && frappe.WasPressedThisFrame())
            {
                var local = JoueurLocal();
                if (local != null) Frapper(local);
            }
        }

        private void FixedUpdate() => Pas(Time.fixedDeltaTime);

        /// <summary>Un pas, au pas physique (un test l'appelle hors jeu) : les recharges avancent.</summary>
        public void Pas(float dt)
        {
            Preparer();
            Regles.Pas(dt);
        }

        /// <summary>
        /// Le joueur de cette machine : le premier qui a un PlayerInput allumé (en réseau, celui qu'on possède). Les bots
        /// (P3-05) en gardent un éteint, exigé par FroggyApparence : sans « enabled », V faisait frapper un bot.
        /// </summary>
        public PorteurCookies JoueurLocal()
        {
            if (partie == null) return null;
            foreach (var p in partie.Porteurs)
            {
                var entree = p != null ? p.GetComponent<PlayerInput>() : null;
                if (entree != null && entree.enabled) return p;
            }
            return null;
        }

        /// <summary>
        /// « frappeur » donne une torgnole. Rend la cible touchée, ou null (raté, en recharge, partie arrêtée).
        /// C'est ce qu'un bot (P3-05) ou l'hôte en réseau appellera.
        /// </summary>
        public PorteurCookies Frapper(PorteurCookies frappeur)
        {
            Preparer();
            if (frappeur == null || partie == null || partie.EnPause) return null;

            autres.Clear();
            foreach (var p in partie.Porteurs)
                if (p != null && p != frappeur && p.isActiveAndEnabled) autres.Add((p.Joueur, p.transform.position));
            Transform t = frappeur.transform;
            if (!Regles.Frapper(frappeur.Joueur, t.position, t.forward, autres, out int numero))
            {
                dernier = "Torgnole en recharge.";
                return null;
            }

            frappeur.SendMessage("Jouer", "Torgnole", SendMessageOptions.DontRequireReceiver);   // le geste du modèle, s'il en a un
            PorteurCookies cible = null;
            foreach (var p in partie.Porteurs)
                if (p != null && p.Joueur == numero) cible = p;
            EvenementsPartie.Emettre(TypeEvenement.CoupDonne, frappeur.gameObject, cible != null ? cible.gameObject : null,
                cible != null ? cible.transform.position : t.position + t.forward);

            if (cible == null) { dernier = "Torgnole dans le vide."; return null; }
            if (cible.Intouchable) { dernier = $"Joueur {cible.Joueur} est intouchable."; return cible; }

            Vector3 direction = cible.transform.position - t.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 1e-4f) direction = t.forward;
            direction.Normalize();
            bool lache = partie.FaireLacher(cible, direction);
            Reculer(cible, direction);
            dernier = lache ? $"Joueur {cible.Joueur} touché : il lâche un cookie." : $"Joueur {cible.Joueur} touché (il n'a pas de cookie).";
            return cible;
        }

        // le recul dit « touché » (skill impact-feedback) ; un mannequin sans Rigidbody ne bouge pas
        private void Reculer(PorteurCookies cible, Vector3 direction)
        {
            var corps = cible.GetComponent<Rigidbody>();
            if (corps == null || corps.isKinematic || reglages.recul <= 0f) return;
            Vector3 v = corps.linearVelocity;
            corps.linearVelocity = new Vector3(direction.x * reglages.recul, Mathf.Max(v.y, 0f), direction.z * reglages.recul);
        }

        private void OnGUI()
        {
            if (!affichageProvisoire || Regles == null) return;
            var local = JoueurLocal();
            if (local == null) return;
            if (style == null) style = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.UpperRight };
            float reste = Regles.Reste(local.Joueur);
            string texte = (reste > 0f ? $"V : torgnole dans {reste:0.0} s" : "V : torgnole prête") + (dernier.Length > 0 ? " — " + dernier : "");
            var zone = new Rect(Screen.width - 914f, Screen.height - 86f, 900f, 26f);
            style.normal.textColor = Color.black;
            GUI.Label(new Rect(zone.x + 1f, zone.y + 1f, zone.width, zone.height), texte, style);
            style.normal.textColor = Color.white;
            GUI.Label(zone, texte, style);
        }
    }
}
