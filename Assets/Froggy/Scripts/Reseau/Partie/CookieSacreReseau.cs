using FishNet.Object;
using FishNet.Object.Synchronizing;
using Froggy.Regles;
using UnityEngine;

namespace Froggy.Reseau
{
    /// <summary>
    /// Le cookie sacré et son gobelin en réseau (#128) : <b>seul l'hôte décide</b> (PartieCookieSacre, comme hors ligne :
    /// apparition, fuite, capture, lâcher, ramassage). Chacun reçoit où est le cookie sacré, qui le porte, et où est le
    /// gobelin ; PartieCookieSacre en Miroir le montre (Refleter). Les événements « cookie sacré » arrivent déjà par
    /// PartieReseau. Sur le préfabriqué de la partie en ligne, à côté de PartieReseau.
    /// </summary>
    [RequireComponent(typeof(PartieCookieSacre))]
    public class CookieSacreReseau : NetworkBehaviour
    {
        private readonly SyncVar<EtatCookieSacre> etat = new(EtatCookieSacre.Absent);
        private readonly SyncVar<int> porteur = new(ReglesPartie.Personne);
        private readonly SyncVar<Vector3> auSol = new(Vector3.zero);
        private readonly SyncVar<Vector3> gobelin = new(Vector3.zero);
        private readonly SyncVar<float> capGobelin = new(0f);
        private readonly SyncVar<bool> gobelinAttrape = new(false);

        private PartieCookieSacre partie;

        private void Awake() => partie = GetComponent<PartieCookieSacre>();

        private void LateUpdate()
        {
            if (IsServerStarted) Publier();
            else if (IsClientStarted)
                partie.Refleter(etat.Value, porteur.Value, auSol.Value, gobelin.Value, capGobelin.Value,
                    gobelinAttrape.Value, Time.deltaTime);
        }

        // seules les différences partent ; le gobelin, à 2 cm et 2° près
        private void Publier()
        {
            ReglesCookieSacre r = partie.Regles;
            if (r == null) return;
            if (etat.Value != r.Etat) etat.Value = r.Etat;
            if (porteur.Value != r.Porteur) porteur.Value = r.Porteur;
            if ((auSol.Value - r.PositionAuSol).sqrMagnitude > 1e-4f) auSol.Value = r.PositionAuSol;

            FuiteGobelin g = partie.Gobelin;
            if (g != null)
            {
                Vector3 ici = g.transform.position;
                if ((gobelin.Value - ici).sqrMagnitude > 4e-4f) gobelin.Value = ici;
                float cap = g.transform.eulerAngles.y;
                if (Mathf.Abs(Mathf.DeltaAngle(capGobelin.Value, cap)) > 2f) capGobelin.Value = cap;
                bool attrape = g.EtatActuel == FuiteGobelin.Etat.Attrape || g.EtatActuel == FuiteGobelin.Etat.Parti;
                if (gobelinAttrape.Value != attrape) gobelinAttrape.Value = attrape;
            }
            else if (gobelinAttrape.Value) gobelinAttrape.Value = false;

            // le porteur, aussi dans PartieReseau (le vainqueur de la fin de partie le lit chez l'hôte)
            var reseau = PartieReseau.Actuelle;
            if (reseau != null && reseau.PorteurCookieSacre != r.Porteur) reseau.PorteurCookieSacre = r.Porteur;
        }
    }
}
