using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Froggy.Outils
{
    /// <summary>
    /// Les versions de l'arène : garder la disposition actuelle (le décor et les repères de jeu) avant de la retoucher, et
    /// y revenir en un clic pour comparer en test de jeu. Une version est une copie éteinte rangée dans la scène, sous
    /// « Versions de l'arène » (marqué EditorOnly : rien de tout ça n'entre dans un build). Revenir à une version garde
    /// d'abord la disposition du moment : on bascule de l'une à l'autre sans rien perdre. Règle 7 d'AGENTS.md : garder
    /// l'ancienne version avec un moyen de basculer, l'équipe compare avant de choisir.
    /// Les copies gardent leur lien aux préfabriqués (copiées comme un Ctrl+D).
    /// </summary>
    public partial class BatisseurArene
    {
        private const string RacineVersions = "Versions de l'arène";
        private string nomVersion = "";

        private static GameObject ConteneurVersions(bool creer)
        {
            var scene = EditorSceneManager.GetActiveScene();
            var c = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RacineVersions);
            if (c == null && creer)
            {
                c = new GameObject(RacineVersions) { tag = "EditorOnly" };
                c.SetActive(false);
                Undo.RegisterCreatedObjectUndo(c, "Créer " + RacineVersions);
            }
            return c;
        }

        /// <summary>Ce qu'une version garde : le groupe du décor et les repères de jeu.</summary>
        private List<GameObject> CeQuOnGarde()
        {
            var l = new List<GameObject>();
            var r = Racine(false); if (r != null) l.Add(r);
            var rep = EditorSceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == RacineReperes);
            if (rep != null) l.Add(rep);
            return l;
        }

        private void VersionsGUI(bool tiroir = true)
        {
            if (tiroir && !SousTiroir("versions", "Versions de l'arène")) return;
            var garde = CeQuOnGarde();
            if (garde.Count == 0)
            {
                // l'état vide le plus fréquent : le décor de la scène est rangé sous un autre nom (« Jardin » dans la vitrine)
                var candidat = GroupeDuDecorProbable();
                EditorGUILayout.HelpBox(candidat == null ? "Rien à garder : la scène n'a ni « Décor » ni « Repères de jeu ». Pose des objets, ou choisis le groupe du décor dans « Réglages du Bâtisseur ▸ Ranger sous »."
                    : $"Le décor de cette scène semble rangé sous « {candidat.name} », pas sous « Décor ».", MessageType.Info);
                if (candidat != null && GUILayout.Button($"Ranger sous « {candidat.name} » (et le garder dans les versions)")) ChoisirRacine(candidat);
                return;
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(new GUIContent("Nom", "Facultatif : « avant le test du 30 », « lac plus grand »…"), GUILayout.Width(32));
                nomVersion = EditorGUILayout.TextField(new GUIContent("", "Un nom pour s'y retrouver : « avant le test du 30 », « lac plus grand »…"), nomVersion);
                using (new EditorGUI.DisabledScope(garde.Count == 0))
                    if (GUILayout.Button(new GUIContent("Garder cette version", "Copie éteinte du décor et des repères, rangée dans la scène"), GUILayout.Width(150)))
                    {
                        var v = GarderVersion(string.IsNullOrWhiteSpace(nomVersion) ? "Version" : nomVersion.Trim()); nomVersion = "";
                        if (v != null) Notifier($"Version « {v.name} » gardée · Ctrl+Z pour annuler");
                    }
            }
            var c = ConteneurVersions(false);
            if (c == null || c.transform.childCount == 0)
            {
                EditorGUILayout.LabelField($"Garde : {string.Join(" et ", garde.Select(g => "« " + g.name + " »"))}. Aucune version pour l'instant.", EditorStyles.wordWrappedMiniLabel);
                return;
            }
            foreach (Transform v in c.transform.Cast<Transform>().ToList())
                using (new EditorGUILayout.HorizontalScope())
                {
                    int n = v.GetComponentsInChildren<Transform>(true).Count(t => PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject));
                    EditorGUILayout.LabelField(new GUIContent(v.name, $"{n} objet(s) posé(s)"));
                    if (GUILayout.Button(new GUIContent("Y revenir", "Garde d'abord la disposition du moment, puis remet celle-ci"), GUILayout.Width(70)))
                    {
                        var nom = v.name;
                        RevenirA(v.gameObject);
                        Notifier($"Retour à « {nom} » (la disposition d'avant est gardée) · Ctrl+Z pour annuler");
                    }
                    if (GUILayout.Button("Suppr.", GUILayout.Width(48))
                        && EditorUtility.DisplayDialog("Supprimer la version", $"Supprimer « {v.name} » ? (Ctrl+Z la rend)", "Supprimer", "Annuler"))
                    { Undo.IncrementCurrentGroup(); Undo.DestroyObjectImmediate(v.gameObject); }
                }
        }

        /// <summary>Le groupe de la scène qui tient le plus d'objets posés (hors repères et versions) : sans doute le décor.</summary>
        private static GameObject GroupeDuDecorProbable()
        {
            return EditorSceneManager.GetActiveScene().GetRootGameObjects()
                .Where(g => g.name != RacineReperes && g.name != RacineVersions && !PrefabUtility.IsAnyPrefabInstanceRoot(g))
                .Select(g => (g, n: g.GetComponentsInChildren<Transform>().Count(t => t != g.transform && PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject))))
                .Where(x => x.n >= 3).OrderByDescending(x => x.n).Select(x => x.g).FirstOrDefault();
        }

        /// <summary>Range une copie éteinte du décor et des repères sous « Versions de l'arène ▸ nom · date ». Un Ctrl+Z.</summary>
        internal GameObject GarderVersion(string nom)
        {
            var garde = CeQuOnGarde(); if (garde.Count == 0) return null;
            Undo.IncrementCurrentGroup(); int g = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Garder une version de l'arène");
            var version = new GameObject($"{nom} · {System.DateTime.Now:dd/MM HH:mm}");
            Undo.RegisterCreatedObjectUndo(version, "Garder une version");
            Undo.SetTransformParent(version.transform, ConteneurVersions(true).transform, "Garder une version");
            // copier comme un Ctrl+D : les liens aux préfabriqués restent
            var avant = Selection.objects;
            Selection.objects = garde.ToArray();
            Unsupported.DuplicateGameObjectsUsingPasteboard();
            foreach (var copie in Selection.gameObjects)
            {
                Undo.SetTransformParent(copie.transform, version.transform, "Garder une version");
                copie.name = copie.name.Replace(" (1)", "");
            }
            Selection.objects = avant;
            Undo.CollapseUndoOperations(g);
            return version;
        }

        /// <summary>Garde la disposition du moment, puis remet celle de la version (qui quitte la liste). Un Ctrl+Z.</summary>
        internal void RevenirA(GameObject version)
        {
            Undo.IncrementCurrentGroup(); int g = Undo.GetCurrentGroup();
            var nom = version.name.Split('·')[0].Trim();
            GarderVersion("Avant de revenir à " + nom);
            Undo.SetCurrentGroupName("Revenir à " + nom);
            var nomRacine = Racine(false)?.name;
            foreach (var actuel in CeQuOnGarde()) Undo.DestroyObjectImmediate(actuel);
            GameObject nouvelleRacine = null;
            foreach (Transform t in version.transform.Cast<Transform>().ToList())
            {
                Undo.SetTransformParent(t, null, "Revenir à une version");
                t.SetAsLastSibling();
                if (t.name == nomRacine) nouvelleRacine = t.gameObject;
            }
            Undo.DestroyObjectImmediate(version);
            Undo.CollapseUndoOperations(g);
            // le groupe « Ranger sous » choisi pour la scène est désormais la copie remise
            ChoisirRacine(nouvelleRacine);
            PerimerAnalyse(); Repaint();
        }
    }
}
