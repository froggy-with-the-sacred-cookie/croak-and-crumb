using System;
using System.Collections.Generic;
using Froggy.Socle;
using UnityEngine;

namespace Froggy.Meteo
{
    /// <summary>
    /// Le gestionnaire de météo et d'événements dans une scène (P5-01), sans réseau. Il suit la fiche de l'arène
    /// (ReglagesMeteo) : tirage, annonce, application des effets sur les Froggy par les Modificateurs du socle, retrait.
    /// Il démarre avec la partie (« partie commencée ») et s'arrête à la fin (« partie finie ») ; sans partie dans la scène,
    /// décocher « attendre la partie ». Le rendu et le son d'un événement écoutent Annonce, Debut et Fin.
    /// L'annonce à l'écran est provisoire, en attendant le HUD (P6-02). Les règles sont dans ReglesMeteo.
    /// </summary>
    public class GestionnaireMeteo : MonoBehaviour
    {
        [SerializeField] private ReglagesMeteo reglages;
        [Tooltip("Démarre au début de la partie et s'arrête à la fin. Décoché : démarre avec la scène.")]
        [SerializeField] private bool attendreLaPartie = true;
        [Tooltip("Affiche l'annonce et le temps qui reste, en attendant le HUD (P6-02).")]
        [SerializeField] private bool affichageProvisoire = true;

        private readonly List<(ChangementMeteo, int)> changements = new List<(ChangementMeteo, int)>();
        private readonly List<Modificateurs> touches = new List<Modificateurs>();
        private readonly List<DefinitionEvenement> definitions = new List<DefinitionEvenement>();
        private IEnumerable<Modificateurs> ciblesImposees;
        private GUIStyle style;

        public ReglesMeteo Regles { get; private set; }

        /// <summary>Vrai entre le début et la fin de la partie (ou dès la scène sans « attendre la partie »).</summary>
        public bool EnMarche { get; private set; }

        /// <summary>L'événement annoncé ou en cours, ou null.</summary>
        public DefinitionEvenement Courant => Regles != null && Regles.Courant >= 0 ? definitions[Regles.Courant] : null;

        public event Action<DefinitionEvenement> Annonce, Debut, Fin;

        /// <summary>L'annonce provisoire à l'écran : le HUD (P6-02) l'éteint quand il prend sa place.</summary>
        public bool AffichageProvisoire { get => affichageProvisoire; set => affichageProvisoire = value; }

        /// <summary>Pour un test : une fiche à soi, et les Froggy à toucher (sinon toutes celles de la scène).</summary>
        public void Regler(ReglagesMeteo r, IEnumerable<Modificateurs> cibles = null)
        {
            reglages = r;
            ciblesImposees = cibles;
        }

        private void OnEnable() => EvenementsPartie.Emis += Ecouter;

        private void OnDisable()
        {
            EvenementsPartie.Emis -= Ecouter;
            Arreter();
        }

        private void Start()
        {
            if (!attendreLaPartie) Demarrer();
        }

        private void Ecouter(EvenementPartie e)
        {
            if (!attendreLaPartie) return;
            if (e.Type == TypeEvenement.PartieCommencee) Demarrer();
            else if (e.Type == TypeEvenement.PartieFinie) Arreter();
        }

        /// <summary>(Re)part du calme avec la fiche de l'arène ; un événement en cours est retiré.</summary>
        public void Demarrer()
        {
            Arreter();
            if (reglages == null) reglages = ScriptableObject.CreateInstance<ReglagesMeteo>();
            definitions.Clear();
            var poids = new List<float>();
            var durees = new List<float>();
            foreach (var t in reglages.evenements)
            {
                if (t == null || t.evenement == null) continue;
                definitions.Add(t.evenement);
                poids.Add(t.poids);
                durees.Add(t.evenement.duree);
            }
            int graine = reglages.graine != 0 ? reglages.graine : Environment.TickCount;
            Regles = new ReglesMeteo(poids, durees, reglages.annonce, reglages.pauseMin, reglages.pauseMax, graine);
            EnMarche = true;
        }

        /// <summary>Arrête la météo : l'événement en cours est retiré, plus rien n'est tiré.</summary>
        public void Arreter()
        {
            if (EnMarche && Regles != null && Regles.Phase == PhaseMeteo.Actif) Fin?.Invoke(Courant);
            Retirer();
            EnMarche = false;
        }

        /// <summary>Annonce tout de suite cet événement de la fiche (un essai, une règle qui le déclenche).</summary>
        public bool Annoncer(DefinitionEvenement evenement)
        {
            if (!EnMarche || Regles == null) return false;
            changements.Clear();
            bool ok = Regles.Annoncer(definitions.IndexOf(evenement), changements);
            Appliquer();
            return ok;
        }

        private void FixedUpdate() => Pas(Time.fixedDeltaTime);

        /// <summary>Un pas, au pas physique (un test l'appelle hors jeu).</summary>
        public void Pas(float dt)
        {
            if (!EnMarche || Regles == null) return;
            changements.Clear();
            Regles.Pas(dt, changements);
            Appliquer();
        }

        private void Appliquer()
        {
            foreach (var (changement, rang) in changements)
            {
                var evenement = definitions[rang];
                switch (changement)
                {
                    case ChangementMeteo.Annonce:
                        Annonce?.Invoke(evenement);
                        break;
                    case ChangementMeteo.Debut:
                        Poser(evenement);
                        Debut?.Invoke(evenement);
                        break;
                    case ChangementMeteo.Fin:
                        Retirer();
                        Fin?.Invoke(evenement);
                        break;
                }
            }
        }

        private void Poser(DefinitionEvenement evenement)
        {
            Retirer();
            var cibles = ciblesImposees ?? FindObjectsByType<Modificateurs>();
            foreach (var m in cibles)
            {
                if (m == null) continue;
                m.Poser(this, vitesse: evenement.facteurVitesse, saut: evenement.facteurSaut);
                touches.Add(m);
            }
        }

        private void Retirer()
        {
            foreach (var m in touches)
                if (m != null) m.Retirer(this);
            touches.Clear();
        }

        private void OnGUI()
        {
            if (!affichageProvisoire || !EnMarche || Regles == null || Courant == null) return;
            if (style == null) style = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperCenter };
            // l'effet s'écrit en toutes lettres : un changement de jeu ne se devine pas (skill accessibilite)
            string effets = Courant.Effets;
            string texte = (Regles.Phase == PhaseMeteo.Annonce
                ? $"Dans {Mathf.CeilToInt(Regles.Reste)} s : {Courant.nom}"
                : $"{Courant.nom} : encore {Mathf.CeilToInt(Regles.Reste)} s") + (effets.Length > 0 ? " · " + effets : "");
            var zone = new Rect(0f, 48f, Screen.width, 32f);
            style.normal.textColor = Color.black;
            GUI.Label(new Rect(zone.x + 2f, zone.y + 2f, zone.width, zone.height), texte, style);
            style.normal.textColor = Color.white;
            GUI.Label(zone, texte, style);
        }
    }
}
