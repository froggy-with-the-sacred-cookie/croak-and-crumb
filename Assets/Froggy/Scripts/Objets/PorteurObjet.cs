using System;
using UnityEngine;

namespace Froggy.Objets
{
    /// <summary>
    /// L'emplacement d'objet d'une Froggy (P4-01). PartieObjets le pose sur chaque Froggy et le tient à jour ; le HUD
    /// lit « Fiche » et « Recharge » ou écoute « Change » ; l'effet d'un objet (P4-02, P4-07, P4-09) écoute « Utilise »,
    /// ou l'événement « objet utilisé » du socle puis lit « DernierUtilise » sur l'auteur.
    /// </summary>
    [DisallowMultipleComponent]
    public class PorteurObjet : MonoBehaviour
    {
        /// <summary>Le numéro du joueur dans la partie (donné par PartieObjets ; en réseau, par l'hôte).</summary>
        public int Joueur { get; private set; }

        /// <summary>L'objet dans l'emplacement. Vide : pas d'objet.</summary>
        public FicheObjet Fiche { get; private set; }

        /// <summary>Le temps de recharge qui reste, en s. 0 : prêt.</summary>
        public float Recharge { get; private set; }

        /// <summary>Le dernier objet utilisé par cette Froggy. Vide : aucun depuis le début de la partie.</summary>
        public FicheObjet DernierUtilise { get; private set; }

        /// <summary>Peut-elle utiliser son objet maintenant ?</summary>
        public bool Pret => Fiche != null && Recharge <= 0f;

        /// <summary>Appelé quand l'objet de l'emplacement change (pris, utilisé), pas à chaque image de la recharge.</summary>
        public event Action Change;

        /// <summary>Appelé quand cette Froggy utilise un objet : c'est là que son effet se déclenche.</summary>
        public event Action<FicheObjet> Utilise;

        public void Numeroter(int joueur) => Joueur = joueur;

        /// <summary>Fixe l'objet de l'emplacement et la recharge qui reste.</summary>
        public void Fixer(FicheObjet fiche, float recharge)
        {
            Recharge = Mathf.Max(0f, recharge);
            if (fiche == Fiche) return;
            Fiche = fiche;
            Change?.Invoke();
        }

        /// <summary>Signale que cette Froggy vient d'utiliser « fiche » (appelé par PartieObjets, après Fixer).</summary>
        public void Signaler(FicheObjet fiche)
        {
            DernierUtilise = fiche;
            Utilise?.Invoke(fiche);
        }

        /// <summary>Repart de zéro (nouvelle partie).</summary>
        public void Vider()
        {
            DernierUtilise = null;
            Fixer(null, 0f);
        }
    }
}
