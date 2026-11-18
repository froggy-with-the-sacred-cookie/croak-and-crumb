using FishNet.Object;
using Froggy.Objets;
using Froggy.Regles;
using UnityEngine;

namespace Froggy.Reseau
{
    /// <summary>
    /// Le gel éclair en réseau (#128) : le projectile vole sur chaque machine (ObjetsReseau y annonce l'objet utilisé),
    /// mais <b>seul l'hôte décide</b> qui est touché et quand la glace se brise (un coup). Il l'envoie à tous, par le
    /// numéro du joueur dans le salon : chacun gèle ou dégèle cette Froggy chez lui. Chez son propriétaire, le gel la
    /// fige pour de vrai (Modificateurs, entrave). Sur le préfabriqué de la partie en ligne.
    /// </summary>
    [RequireComponent(typeof(EffetGelEclair))]
    public class GelReseau : NetworkBehaviour
    {
        private EffetGelEclair gel;
        private PartieCookies cookies;

        private void Awake()
        {
            gel = GetComponent<EffetGelEclair>();
            cookies = GetComponent<PartieCookies>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            gel.Gele += QuandGele;
            gel.Libere += QuandLibere;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            gel.Gele -= QuandGele;
            gel.Libere -= QuandLibere;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!IsServerStarted) gel.Miroir = true;
        }

        private void QuandGele(GameObject joueur)
        {
            int n = Numero(joueur);
            if (n > 0) GelerChezTous(n);
        }

        private void QuandLibere(GameObject joueur)
        {
            int n = Numero(joueur);
            if (n > 0) LibererChezTous(n);
        }

        private static int Numero(GameObject joueur)
        {
            var p = joueur != null ? joueur.GetComponent<PorteurCookies>() : null;
            return p != null ? p.Joueur : 0;
        }

        [ObserversRpc(ExcludeServer = true)]
        private void GelerChezTous(int joueur)
        {
            var p = cookies != null ? cookies.Trouver(joueur) : null;
            if (p != null) gel.Geler(p.gameObject, Vector3.zero);
        }

        [ObserversRpc(ExcludeServer = true)]
        private void LibererChezTous(int joueur)
        {
            var p = cookies != null ? cookies.Trouver(joueur) : null;
            if (p != null) gel.Degeler(p.gameObject);
        }
    }
}
