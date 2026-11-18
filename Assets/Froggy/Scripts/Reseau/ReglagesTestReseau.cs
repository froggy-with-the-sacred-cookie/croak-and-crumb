using System;
using System.Collections.Generic;
using UnityEngine;

namespace Froggy.Reseau
{
    /// <summary>
    /// Réglages des tests à plusieurs sur une machine (P2-07) : démarrage automatique et préréglages de connexion.
    /// Les fenêtres de Multiplayer Play Mode partagent le dossier Assets : un préréglage choisi avant Play vaut pour toutes.
    /// Valeurs PROVISOIRES, à ajuster quand on saura mieux ce que vivent les joueurs.
    /// </summary>
    [CreateAssetMenu(menuName = "Froggy/Réseau/Réglages de test", fileName = "ReglagesTestReseau")]
    public class ReglagesTestReseau : ScriptableObject
    {
        [Serializable]
        public class Prereglage
        {
            public string nom = "Parfait";

            [Tooltip("Aller-retour visé, en millisecondes (ce que le panneau affiche comme ping). " +
                     "Chaque fenêtre en retarde ses envois de la moitié.")]
            [Min(0)] public int allerRetourMs;

            [Tooltip("Pourcentage de messages non fiables perdus (les messages fiables arrivent, mais plus tard).")]
            [Range(0f, 100f)] public float pertesPourcent;

            [Tooltip("Pourcentage de messages non fiables qui arrivent dans le désordre.")]
            [Range(0f, 100f)] public float desordrePourcent;

            public bool Simule => allerRetourMs > 0 || pertesPourcent > 0f || desordrePourcent > 0f;

            public string Resume => Simule
                ? $"{nom} ({allerRetourMs} ms, {pertesPourcent:0.#} % pertes{(desordrePourcent > 0f ? $", {desordrePourcent:0.#} % désordre" : "")})"
                : $"{nom} (aucune simulation)";
        }

        [Tooltip("En éditeur : la fenêtre principale démarre l'hôte, les fenêtres Player 2-4 se connectent seules. " +
                 "Décoché : boutons Server / Client du HUD, comme avant.")]
        public bool demarrageAuto = true;

        [Tooltip("Préréglage appliqué au lancement (menu Froggy ▸ Réseau ▸ Préréglage).")]
        [Min(0)] public int prereglageActif;

        public List<Prereglage> prereglages = new()
        {
            new Prereglage { nom = "Parfait" },
            new Prereglage { nom = "Wifi moyen", allerRetourMs = 100, pertesPourcent = 1f },
            new Prereglage { nom = "Mauvaise connexion", allerRetourMs = 250, pertesPourcent = 5f, desordrePourcent = 2f },
        };

        public Prereglage Lire(int index) =>
            prereglages.Count == 0 ? new Prereglage() : prereglages[Mathf.Clamp(index, 0, prereglages.Count - 1)];

        /// <summary>Applique un préréglage au simulateur de latence de FishNet de cette fenêtre seulement.</summary>
        public void Appliquer(FishNet.Managing.NetworkManager reseau, int index)
        {
            if (reseau == null) return;
            Prereglage p = Lire(index);

            // Chaque fenêtre retarde ses propres envois ; l'hôte fait passer les deux sens par son simulateur.
            // La moitié de l'aller-retour visé dans chaque fenêtre donne donc le bon ping partout.
            var simulateur = reseau.TransportManager.LatencySimulator;
            simulateur.SetLatency(p.allerRetourMs / 2);
            simulateur.SetPacketLoss(p.pertesPourcent / 100.0);
            simulateur.SetOutOfOrder(p.desordrePourcent / 100.0);
            simulateur.SetEnabled(p.Simule);
        }
    }
}
