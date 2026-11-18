using System;
using System.Collections.Generic;
using UnityEngine;

namespace Froggy.Socle
{
    /// <summary>
    /// Les modificateurs de vitesse et de saut d'une Froggy (socle commun, T-01). La météo, un objet ou les cookies
    /// portés posent chacun le leur, sans se connaître ; le contrôleur lit le total. Les modificateurs se multiplient
    /// (choix du 18 novembre 2026) : pluie ×1,3 et cookies ×0,8 donnent ×1,04. Aucune valeur de jeu n'est décidée ici.
    ///
    /// Depuis un autre poste :
    ///   froggy.GetComponent&lt;Modificateurs&gt;().Poser(this, vitesse: 1.3f);   // « this » identifie qui l'a posé
    ///   froggy.GetComponent&lt;Modificateurs&gt;().Retirer(this);
    /// Poser une deuxième fois avec la même source remplace le premier, il ne s'ajoute pas.
    /// </summary>
    [DisallowMultipleComponent]
    public class Modificateurs : MonoBehaviour
    {
        private struct Pose { public object source; public float vitesse, saut; }

        // l'ordre de pose est gardé : le produit se calcule pareil chez tous les joueurs
        [NonSerialized] private readonly List<Pose> poses = new List<Pose>();

        /// <summary>Multiplie la vitesse de course. 1 : rien ne change.</summary>
        public float Vitesse { get; private set; } = 1f;

        /// <summary>Multiplie la hauteur du saut (la vitesse de départ en découle). 1 : rien ne change.</summary>
        public float Saut { get; private set; } = 1f;

        // qui empêche cette Froggy d'agir (gel éclair…) : ni torgnole, ni objet
        [NonSerialized] private readonly List<object> entraves = new List<object>();

        /// <summary>Vrai tant qu'une source l'empêche d'agir : PartieTorgnoles et PartieObjets refusent alors.</summary>
        public bool Entrave => entraves.Count > 0;

        /// <summary>« source » empêche cette Froggy de frapper et d'utiliser son objet, jusqu'à Desentraver(source).</summary>
        public void Entraver(object source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (!entraves.Contains(source)) entraves.Add(source);
        }

        /// <summary>Lève l'entrave de « source ». Sans effet si elle n'en a pas posé.</summary>
        public void Desentraver(object source) => entraves.Remove(source);

        /// <summary>Combien de modificateurs sont posés.</summary>
        public int Nombre => poses.Count;

        /// <summary>Appelé quand le total change (pour un effet, une icône du HUD…).</summary>
        public event Action Change;

        /// <summary>Pose le modificateur de « source », ou remplace celui qu'elle avait déjà posé. Un facteur négatif vaut 0.</summary>
        public void Poser(object source, float vitesse = 1f, float saut = 1f)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var pose = new Pose { source = source, vitesse = Mathf.Max(0f, vitesse), saut = Mathf.Max(0f, saut) };
            int i = Indice(source);
            if (i >= 0) poses[i] = pose;
            else poses.Add(pose);
            Recalculer();
        }

        /// <summary>Retire le modificateur de « source ». Sans effet si elle n'en a pas posé.</summary>
        public void Retirer(object source)
        {
            int i = Indice(source);
            if (i < 0) return;
            poses.RemoveAt(i);
            Recalculer();
        }

        /// <summary>Retire tout (réapparition, fin de partie).</summary>
        public void Vider()
        {
            entraves.Clear();
            if (poses.Count == 0) return;
            poses.Clear();
            Recalculer();
        }

        private int Indice(object source)
        {
            for (int i = 0; i < poses.Count; i++)
                if (ReferenceEquals(poses[i].source, source)) return i;
            return -1;
        }

        private void Recalculer()
        {
            float vitesse = 1f, saut = 1f;
            foreach (var p in poses) { vitesse *= p.vitesse; saut *= p.saut; }
            if (Mathf.Approximately(vitesse, Vitesse) && Mathf.Approximately(saut, Saut)) return;
            Vitesse = vitesse;
            Saut = saut;
            Change?.Invoke();
        }
    }
}
