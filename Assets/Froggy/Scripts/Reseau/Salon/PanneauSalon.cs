using UnityEngine;

namespace Froggy.Reseau
{
    /// <summary>
    /// Panneau PROVISOIRE du salon (P2-03), en haut à gauche, pour tester sans les écrans de P6-03 : créer, taper un
    /// code et rejoindre, liste des joueurs, prêt, couleur, lancer, revenir, quitter. N'utilise que l'API de Salon :
    /// c'est aussi un exemple de branchement. Éditeur et build de développement seulement. F10 le cache.
    /// </summary>
    [RequireComponent(typeof(Salon))]
    public class PanneauSalon : MonoBehaviour
    {
        [SerializeField] private bool visible = true;
        [SerializeField] private int largeur = 320;

        private Salon salon;
        private string saisie = "";
        private GUIStyle fond, texte, erreur;

        private void Awake() => salon = GetComponent<Salon>();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void Update()
        {
            var clavier = UnityEngine.InputSystem.Keyboard.current;
            if (clavier != null && clavier.f10Key.wasPressedThisFrame) visible = !visible;
        }

        private void OnGUI()
        {
            if (!visible) return;
            PreparerStyles();

            // En partie : en bas à gauche, pour laisser le haut au score et au temps (P2-04).
            bool enPartie = salon.Connecte && salon.Phase == PhaseSalon.EnPartie;
            GUILayout.BeginArea(enPartie ? new Rect(10, Screen.height - 250, largeur, 240)
                                         : new Rect(10, 10, largeur, Screen.height - 20));
            GUILayout.BeginVertical(fond);
            GUILayout.Label("<b>Salon</b> <size=11>(provisoire, P2-03)</size>", texte);

            if (!salon.Connecte) HorsLigne();
            else DansLeSalon();

            if (!string.IsNullOrEmpty(salon.DerniereErreur))
                GUILayout.Label(salon.DerniereErreur, erreur);
            GUILayout.Label("<size=11>F10 : cacher</size>", texte);
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void HorsLigne()
        {
            GUILayout.Label(salon.Etat, texte);
            GUI.enabled = !salon.Occupe;
            if (GUILayout.Button("Créer un salon", GUILayout.Height(28))) salon.Creer();

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Code :", texte, GUILayout.Width(50));
            saisie = GUILayout.TextField(saisie, 8, GUILayout.Height(26));
            if (GUILayout.Button("Rejoindre", GUILayout.Width(90), GUILayout.Height(26))) salon.Rejoindre(saisie);
            GUILayout.EndHorizontal();
            GUI.enabled = true;
        }

        private void DansLeSalon()
        {
            bool enPartie = salon.Phase == PhaseSalon.EnPartie;
            GUILayout.Label($"Code : <size=22><b>{salon.Code}</b></size>   {(salon.EstHote ? "(vous êtes l'hôte)" : "")}", texte);
            GUILayout.Label(enPartie ? "<b>Partie en cours</b>" : "En attente des joueurs", texte);

            MembreSalon? moi = salon.Moi;
            foreach (MembreSalon m in salon.Membres)
            {
                string couleur = ColorUtility.ToHtmlStringRGB(salon.Couleur(m.Couleur));
                string etat = m.Hote ? "hôte" : m.Pret ? "prêt" : "pas prêt";
                string vous = moi.HasValue && moi.Value.ClientId == m.ClientId ? " (vous)" : "";
                GUILayout.Label($"<color=#{couleur}>■■</color>  Joueur {m.Numero}{vous} : {etat}", texte);
            }

            GUILayout.Space(6);
            if (!enPartie && moi.HasValue)
            {
                GUILayout.BeginHorizontal();
                if (!moi.Value.Hote && GUILayout.Button(moi.Value.Pret ? "Pas prêt" : "Prêt", GUILayout.Height(28)))
                    salon.SeDirePret(!moi.Value.Pret);
                if (GUILayout.Button("Couleur suivante", GUILayout.Height(28))) salon.CouleurSuivante();
                GUILayout.EndHorizontal();
            }

            if (salon.EstHote)
            {
                if (!enPartie)
                {
                    GUI.enabled = salon.PeutLancer;
                    if (GUILayout.Button("Lancer la partie", GUILayout.Height(30))) salon.Lancer();
                    GUI.enabled = true;
                    if (!salon.PeutLancer)
                        GUILayout.Label("<size=11>Il faut 2 joueurs ou plus, tous prêts.</size>", texte);
                }
                else if (GUILayout.Button("Revenir au salon", GUILayout.Height(30))) salon.RevenirAuSalon();
            }

            if (GUILayout.Button(salon.EstHote ? "Fermer le salon" : "Quitter le salon")) salon.Quitter();
        }

        private void PreparerStyles()
        {
            if (fond != null) return;
            // Fond sombre opaque, comme le panneau de test réseau : lisible sur le gravier clair du jardin.
            var image = new Texture2D(1, 1);
            image.SetPixel(0, 0, new Color(0.08f, 0.1f, 0.08f, 0.9f));
            image.Apply();
            fond = new GUIStyle(GUI.skin.box) { padding = new RectOffset(10, 10, 8, 8) };
            fond.normal.background = image;
            texte = new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true, wordWrap = true };
            texte.normal.textColor = Color.white;
            erreur = new GUIStyle(texte);
            erreur.normal.textColor = new Color(1f, 0.45f, 0.4f);
        }
#endif
    }
}
