using System.Collections.Generic;
using UnityEngine;

namespace Froggy.Meteo
{
    /// <summary>Où en est la météo de la partie (P5-01).</summary>
    public enum PhaseMeteo
    {
        /// <summary>Rien ne se passe ; le prochain tirage approche.</summary>
        Calme,
        /// <summary>Un événement est tiré et annoncé : il commence bientôt.</summary>
        Annonce,
        /// <summary>L'événement est en cours : ses effets s'appliquent.</summary>
        Actif,
    }

    /// <summary>Ce qui vient de changer pendant un pas.</summary>
    public enum ChangementMeteo { Annonce, Debut, Fin }

    /// <summary>
    /// Les règles du gestionnaire de météo et d'événements (P5-01), en C# pur, sans scène ni réseau : c'est ce que l'hôte
    /// fera tourner. Après un calme de durée tirée entre « pauseMin » et « pauseMax », un événement est tiré selon les
    /// poids de l'arène, annoncé « annonce » secondes, actif pendant sa durée, puis le calme revient.
    /// Le tirage suit une graine : avec la même graine, la même suite d'événements partout (l'hôte la choisit).
    /// </summary>
    public class ReglesMeteo
    {
        private readonly List<float> poids, durees;
        private readonly float annonce, pauseMin, pauseMax;
        private readonly System.Random alea;

        public PhaseMeteo Phase { get; private set; } = PhaseMeteo.Calme;

        /// <summary>L'événement annoncé ou en cours (son rang dans les réglages), -1 au calme.</summary>
        public int Courant { get; private set; } = -1;

        /// <summary>Le temps avant le prochain changement de phase.</summary>
        public float Reste { get; private set; }

        /// <param name="poids">Le poids de chaque événement : 0 ne sort jamais, 2 sort deux fois plus souvent que 1.</param>
        /// <param name="durees">La durée de chaque événement, en s.</param>
        public ReglesMeteo(IList<float> poids, IList<float> durees, float annonce, float pauseMin, float pauseMax, int graine)
        {
            this.poids = new List<float>(poids);
            this.durees = new List<float>(durees);
            this.annonce = Mathf.Max(0f, annonce);
            this.pauseMin = Mathf.Max(0f, pauseMin);
            this.pauseMax = Mathf.Max(this.pauseMin, pauseMax);
            alea = new System.Random(graine);
            Reste = Pause();
        }

        /// <summary>Un pas ; les changements du pas (annonce, début, fin, avec le rang de l'événement) vont dans « changements ».</summary>
        public void Pas(float dt, List<(ChangementMeteo, int)> changements)
        {
            Reste -= dt;
            // une annonce nulle enchaîne annonce et début dans le même pas ; un calme sans événement possible reste calme
            for (int garde = 0; Reste <= 0f && garde < 4; garde++)
            {
                switch (Phase)
                {
                    case PhaseMeteo.Calme:
                        int tire = Tirer();
                        if (tire < 0) { Reste += Mathf.Max(Pause(), 1f); break; }
                        Courant = tire;
                        Phase = PhaseMeteo.Annonce;
                        Reste += annonce;
                        changements.Add((ChangementMeteo.Annonce, tire));
                        break;
                    case PhaseMeteo.Annonce:
                        Phase = PhaseMeteo.Actif;
                        Reste += Duree(Courant);
                        changements.Add((ChangementMeteo.Debut, Courant));
                        break;
                    case PhaseMeteo.Actif:
                        changements.Add((ChangementMeteo.Fin, Courant));
                        Courant = -1;
                        Phase = PhaseMeteo.Calme;
                        Reste += Pause();
                        break;
                }
            }
        }

        /// <summary>
        /// Lance tout de suite l'annonce de cet événement (un essai, ou une règle qui le déclenche). Sans effet si un
        /// événement est déjà annoncé ou en cours.
        /// </summary>
        public bool Annoncer(int evenement, List<(ChangementMeteo, int)> changements)
        {
            if (Phase != PhaseMeteo.Calme || evenement < 0 || evenement >= durees.Count) return false;
            Courant = evenement;
            Phase = PhaseMeteo.Annonce;
            Reste = annonce;
            changements.Add((ChangementMeteo.Annonce, evenement));
            return true;
        }

        /// <summary>
        /// En réseau, chez un joueur qui n'est pas l'hôte : prend l'état reçu de l'hôte (#128). Ce qui change par rapport
        /// à l'état d'avant (fin de l'ancien événement, annonce, début) va dans « changements », comme pendant un pas ;
        /// une annonce manquée (message arrivé tard) passe directement au début.
        /// </summary>
        public void Imposer(PhaseMeteo phase, int courant, float reste, List<(ChangementMeteo, int)> changements)
        {
            if (phase == PhaseMeteo.Calme || courant < 0 || courant >= durees.Count) { phase = PhaseMeteo.Calme; courant = -1; }
            bool autre = courant != Courant;
            if (Phase == PhaseMeteo.Actif && (phase != PhaseMeteo.Actif || autre)) changements.Add((ChangementMeteo.Fin, Courant));
            if (phase == PhaseMeteo.Annonce && (Phase != PhaseMeteo.Annonce || autre)) changements.Add((ChangementMeteo.Annonce, courant));
            if (phase == PhaseMeteo.Actif && (Phase != PhaseMeteo.Actif || autre)) changements.Add((ChangementMeteo.Debut, courant));
            Phase = phase;
            Courant = courant;
            Reste = Mathf.Max(0f, reste);
        }

        /// <summary>Tire un événement selon les poids ; -1 si aucun ne peut sortir.</summary>
        public int Tirer()
        {
            float total = 0f;
            foreach (float p in poids) total += Mathf.Max(0f, p);
            if (total <= 0f) return -1;
            double t = alea.NextDouble() * total;
            for (int i = 0; i < poids.Count; i++)
            {
                float p = Mathf.Max(0f, poids[i]);
                if (p <= 0f) continue;
                if (t < p) return i;
                t -= p;
            }
            for (int i = poids.Count - 1; i >= 0; i--) if (poids[i] > 0f) return i;   // arrondi
            return -1;
        }

        private float Duree(int i) => i >= 0 && i < durees.Count ? Mathf.Max(0.01f, durees[i]) : 0.01f;

        private float Pause() => pauseMin + (float)alea.NextDouble() * (pauseMax - pauseMin);
    }
}
