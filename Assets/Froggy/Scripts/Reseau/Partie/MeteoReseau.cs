using FishNet.Object;
using FishNet.Object.Synchronizing;
using Froggy.Meteo;
using UnityEngine;

namespace Froggy.Reseau
{
    /// <summary>
    /// La météo en réseau (#128) : <b>seul l'hôte tire</b> les événements (GestionnaireMeteo, comme hors ligne). Chacun
    /// reçoit la phase, l'événement et le temps qui reste, et son gestionnaire en Miroir les montre : annonce, début et
    /// fin partent pareil partout (effets sur les Froggy, rendu, son, HUD). Sur le préfabriqué de la partie en ligne, à
    /// côté de PartieReseau.
    /// </summary>
    [RequireComponent(typeof(GestionnaireMeteo))]
    public class MeteoReseau : NetworkBehaviour
    {
        private readonly SyncVar<PhaseMeteo> phase = new(PhaseMeteo.Calme);
        private readonly SyncVar<int> courant = new(-1);
        private readonly SyncVar<float> reste = new(0f);

        private GestionnaireMeteo meteo;
        private bool aRefleter;

        private void Awake()
        {
            meteo = GetComponent<GestionnaireMeteo>();
            phase.OnChange += (_, _, _) => aRefleter = true;
            courant.OnChange += (_, _, _) => aRefleter = true;
            reste.OnChange += (_, _, _) => aRefleter = true;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (IsServerStarted) return;   // le client de l'hôte voit déjà la vraie météo
            meteo.Miroir = true;
            aRefleter = true;
        }

        private void LateUpdate()
        {
            if (IsServerStarted) { Publier(); return; }
            // la partie peut commencer ici après le dernier message : on se recale dès qu'un écart se voit
            ReglesMeteo r = meteo.Regles;
            bool ecart = meteo.EnMarche && r != null && (r.Phase != phase.Value || r.Courant != courant.Value);
            if (!aRefleter && !ecart) return;
            aRefleter = false;
            meteo.Refleter(phase.Value, courant.Value, reste.Value);
        }

        // seules les différences partent ; le temps qui reste, à 0,2 s près (le HUD l'affiche à la seconde)
        private void Publier()
        {
            ReglesMeteo r = meteo.EnMarche ? meteo.Regles : null;
            PhaseMeteo p = r != null ? r.Phase : PhaseMeteo.Calme;
            int c = r != null ? r.Courant : -1;
            float s = r != null ? r.Reste : 0f;
            bool nouvelle = phase.Value != p || courant.Value != c;
            if (phase.Value != p) phase.Value = p;
            if (courant.Value != c) courant.Value = c;
            if (nouvelle || Mathf.Abs(reste.Value - s) > 0.2f) reste.Value = s;
        }
    }
}
