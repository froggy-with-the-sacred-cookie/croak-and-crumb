using UnityEngine;
using UnityEngine.InputSystem;

namespace Froggy.Reseau
{
    /// <summary>
    /// Panneau des tests réseau (P2-07), en haut à droite : rôle de la fenêtre, joueurs connectés, ping mesuré,
    /// préréglage de connexion, état. F8 passe au préréglage suivant dans cette fenêtre seulement.
    /// Visible en éditeur et en build de développement, comme le menu de debug de la caméra.
    /// </summary>
    [RequireComponent(typeof(TestReseau))]
    public class PanneauTestReseau : MonoBehaviour
    {
        [SerializeField] private bool visible = true;
        [SerializeField] private int largeur = 330;

        private TestReseau test;
        private GUIStyle style;

        private void Awake() => test = GetComponent<TestReseau>();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void Update()
        {
            Keyboard clavier = Keyboard.current;
            if (clavier == null) return;
            if (clavier.f8Key.wasPressedThisFrame) test.PrereglageSuivant();
            if (clavier.f9Key.wasPressedThisFrame) visible = !visible;
        }

        private void OnGUI()
        {
            if (!visible) return;
            if (style == null)
            {
                // Fond sombre opaque : le panneau doit rester lisible sur le gravier clair du jardin.
                var fond = new Texture2D(1, 1);
                fond.SetPixel(0, 0, new Color(0.08f, 0.1f, 0.08f, 0.88f));
                fond.Apply();
                style = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.UpperLeft, fontSize = 14, wordWrap = true, richText = true,
                    padding = new RectOffset(10, 10, 8, 8),
                };
                style.normal.background = fond;
                style.normal.textColor = Color.white;
            }

            var reseau = test.Reseau;
            string role = test.RoleActuel switch
            {
                TestReseau.Role.Hote => "Hôte (fenêtre principale)",
                TestReseau.Role.Client => reseau.ClientManager.Connection.IsValid
                    ? $"Client n° {reseau.ClientManager.Connection.ClientId}"
                    : "Client (pas encore connecté)",
                _ => reseau.IsHostStarted ? "Hôte" : reseau.IsClientOnlyStarted ? "Client" : reseau.IsServerOnlyStarted ? "Serveur seul" : "Hors ligne",
            };

            // Seulement les connexions authentifiées : une tentative avortée d'un client (arrivée avant que l'hôte
            // écoute) reste quelques secondes dans la liste sans être un joueur.
            int joueurs = 0;
            if (reseau.IsServerStarted)
            {
                foreach (var client in reseau.ServerManager.Clients.Values)
                    if (client.IsAuthenticated) joueurs++;
            }
            else if (reseau.IsClientStarted)
            {
                joueurs = reseau.ClientManager.Clients.Count;
            }
            string ping = reseau.IsClientStarted ? $"{reseau.TimeManager.RoundTripTime} ms" : "—";
            string connexion = test.Reglages != null ? test.Reglages.Lire(test.Prereglage).Resume : "pas de réglages";

            string texte =
                $"<b>Test réseau</b>\n" +
                $"Rôle : {role}\n" +
                $"Joueurs connectés : {joueurs}\n" +
                $"Ping : {ping}\n" +
                $"Connexion simulée : {connexion}\n" +
                (string.IsNullOrEmpty(test.Etat) ? "" : $"{test.Etat}\n") +
                "<size=11>F8 : connexion suivante (cette fenêtre) · F9 : cacher</size>";

            float hauteur = style.CalcHeight(new GUIContent(texte), largeur);
            GUI.Box(new Rect(Screen.width - largeur - 10, 10, largeur, hauteur), texte, style);
        }
#endif
    }
}
