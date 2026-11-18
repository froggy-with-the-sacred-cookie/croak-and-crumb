using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Froggy.Outils
{
    /// <summary>
    /// L'outil Semer : un mélange (Assets/Froggy/Batisseur/Semis.asset) répandu au pinceau sur le sol visé, en massifs et
    /// clairières. On ne sème que sur l'objet visé au début du coup (le sol, un talus), jamais dans un autre objet, avec un
    /// espacement minimal entre les objets du mélange. Un coup de pinceau = un Ctrl+Z.
    /// </summary>
    public partial class BatisseurArene
    {
        private static readonly string[] ModesSemis = { "Semer", "Clairsemer", "Effacer" };

        private SemisArene semis; private SerializedObject semisSO;
        private int modeSemis;
        private bool enTrait; private Vector3 dernierPoint; private int groupeSemis;
        private GameObject solVise; private List<GameObject> instancesMelange; private string rapportSemis = "";
        private int semes, retires;
        private Vector3 pinceau, pinceauNormale; private bool pinceauOk;

        private static int MelangeChoisi { get => EditorPrefs.GetInt(Pref + "melange", 0); set => EditorPrefs.SetInt(Pref + "melange", value); }
        private static float Rayon { get => EditorPrefs.GetFloat(Pref + "rayon", 3); set => EditorPrefs.SetFloat(Pref + "rayon", value); }

        public static SemisArene Semis()
        {
            var chemin = Dossier + "/Semis.asset";
            var s = AssetDatabase.LoadAssetAtPath<SemisArene>(chemin);
            if (s != null) return s;
            if (!AssetDatabase.IsValidFolder(Dossier)) AssetDatabase.CreateFolder("Assets/Froggy", "Batisseur");
            s = CreateInstance<SemisArene>();
            MelangesDeDepart(s);
            AssetDatabase.CreateAsset(s, chemin);
            AssetDatabase.SaveAssets();
            return s;
        }

        private const string Jardin = "Assets/Froggy/Prefabs/JardinJaponais/";

        /// <summary>Les mélanges de départ, d'après le kit du jardin japonais (des propositions, à régler en jeu).</summary>
        private static void MelangesDeDepart(SemisArene s)
        {
            SemisArene.Element El(string nom, float poids, bool pente = false, float enfonce = 0)
                => new SemisArene.Element { modele = AssetDatabase.LoadAssetAtPath<GameObject>(Jardin + nom + ".prefab"), poids = poids, suivreLaPente = pente, enfoncement = enfonce };
            void Ajouter(SemisArene.Melange m) { m.elements.RemoveAll(e => e.modele == null); if (m.elements.Count > 0) s.melanges.Add(m); }
            Ajouter(new SemisArene.Melange
            {
                nom = "Sous-bois du jardin", note = "Fougères, hostas et iris, en touffes, au pied des arbres et le long des berges",
                elements = { El("Fougere", 1), El("Hosta", 1), El("Iris", .5f) }, densite = .4f, espacement = .8f, massifs = .5f,
            });
            Ajouter(new SemisArene.Melange
            {
                nom = "Pierres et mousse", note = "Rochers moussus enfoncés, pierres de gué et plaques de mousse",
                elements = { El("Rocher_Mousse", 1, true, .1f), El("PierreGue_2", .5f, true, .05f), El("Mousse_Plaque", 1, true) },
                densite = .15f, espacement = 1.5f, massifs = .6f, tailleMin = .7f, tailleMax = 1.3f,
            });
            Ajouter(new SemisArene.Melange
            {
                nom = "Massif de karikomi", note = "Buissons taillés en boule, serrés en massifs",
                elements = { El("Karikomi_Petit", 2), El("Karikomi_Moyen", 1), El("Karikomi_Fleuri", 1) }, densite = .2f, espacement = 1.4f, massifs = .5f,
            });
            Ajouter(new SemisArene.Melange
            {
                nom = "Bosquet d'érables et de cerisiers", note = "Des arbres espacés, quelques pins niwaki",
                elements = { El("Erable_Momiji", 1), El("Cerisier_1", 1), El("Cerisier_2", 1), El("Pin_Niwaki", .5f) },
                densite = .03f, espacement = 5, massifs = .3f, tailleMin = .9f, tailleMax = 1.1f, penteMax = 25,
            });
        }

        private SemisArene.Melange Melange()
        {
            if (semis == null) semis = Semis();
            if (semis.melanges.Count == 0) return null;
            return semis.melanges[Mathf.Clamp(MelangeChoisi, 0, semis.melanges.Count - 1)];
        }

        private void SemerGUI()
        {
            if (semis == null) semis = Semis();
            if (semisSO == null || semisSO.targetObject != semis) semisSO = new SerializedObject(semis);
            semisSO.Update();
            var noms = semis.melanges.Select((m, i) => $"{i + 1}. {m.nom}").ToArray();
            if (noms.Length == 0) EditorGUILayout.HelpBox("Aucun mélange : crée-en un dans « Plus de réglages ».", MessageType.Info);
            else
            {
                MelangeChoisi = EditorGUILayout.Popup(new GUIContent("Quoi semer", "Un mélange d'objets répandus au pinceau"), Mathf.Clamp(MelangeChoisi, 0, noms.Length - 1), noms);
                var mel = Melange();
                if (!string.IsNullOrEmpty(mel.note)) EditorGUILayout.LabelField(mel.note, EditorStyles.wordWrappedMiniLabel);
            }
            modeSemis = GUILayout.Toolbar(modeSemis, new[]
            {
                new GUIContent("Semer", "Ajoute des objets du mélange sous le pinceau ; repasser épaissit"),
                new GUIContent("Clairsemer", "Retire une partie de ce que le mélange a semé sous le pinceau"),
                new GUIContent("Effacer", "Retire tout ce que le mélange a semé sous le pinceau (aussi : Maj+glisser)"),
            });
            Rayon = EditorGUILayout.Slider(new GUIContent("Taille du pinceau", "Son rayon, en mètres"), Rayon, .5f, 20);
            if (rapportSemis != "") EditorGUILayout.HelpBox(rapportSemis, MessageType.None);
            if (!Plus("semer")) return;

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Les mélanges", EditorStyles.miniBoldLabel);
                if (GUILayout.Button(new GUIContent("Nouveau", "Un mélange vide"), EditorStyles.miniButtonLeft, GUILayout.Width(62)))
                {
                    Undo.RecordObject(semis, "Nouveau mélange");
                    semis.melanges.Add(new SemisArene.Melange()); MelangeChoisi = semis.melanges.Count - 1; EditorUtility.SetDirty(semis);
                }
                using (new EditorGUI.DisabledScope(noms.Length == 0))
                {
                    if (GUILayout.Button(new GUIContent("Copier", "Un nouveau mélange à partir de celui-ci"), EditorStyles.miniButtonMid, GUILayout.Width(52)))
                    {
                        Undo.RecordObject(semis, "Copier le mélange");
                        var copie = JsonUtility.FromJson<SemisArene.Melange>(JsonUtility.ToJson(Melange()));
                        copie.nom += " (copie)"; semis.melanges.Add(copie); MelangeChoisi = semis.melanges.Count - 1; EditorUtility.SetDirty(semis);
                    }
                    if (GUILayout.Button(new GUIContent("Suppr.", "Supprimer ce mélange (ce qui a été semé reste)"), EditorStyles.miniButtonRight, GUILayout.Width(48))
                        && EditorUtility.DisplayDialog("Supprimer le mélange", $"Supprimer « {Melange().nom} » ? Ce qui a été semé reste.", "Supprimer", "Annuler"))
                    {
                        Undo.RecordObject(semis, "Supprimer le mélange");
                        semis.melanges.RemoveAt(Mathf.Clamp(MelangeChoisi, 0, semis.melanges.Count - 1)); EditorUtility.SetDirty(semis);
                        semisSO.Update();
                    }
                }
            }
            if (semis.melanges.Count > 0)
            {
                var p = semisSO.FindProperty("melanges").GetArrayElementAtIndex(Mathf.Clamp(MelangeChoisi, 0, semis.melanges.Count - 1));
                var it = p.Copy(); var fin = p.GetEndProperty(); bool entrer = true;
                while (it.NextVisible(entrer) && !SerializedProperty.EqualContents(it, fin))
                {
                    EditorGUILayout.PropertyField(it, true); entrer = false;
                }
                if (semisSO.ApplyModifiedProperties()) AssetDatabase.SaveAssetIfDirty(semis);
            }
            EditorGUILayout.HelpBox("On sème sur l'objet visé au début du coup (le sol, un talus) : il lui faut une collision. " +
                "L'eau n'en a pas : règle l'altitude minimale pour ne pas semer au fond d'un étang.", MessageType.None);
        }

        private void SemerScene(SceneView sv, Event e)
        {
            var mel = Melange();
            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag || e.type == EventType.MouseDown)
            {
                pinceauOk = ViserCollision(e.mousePosition, out var hit);
                if (pinceauOk) { pinceau = hit.point; pinceauNormale = hit.normal; }
                if (pinceauOk && e.type == EventType.MouseDown) solVise = ObjetDe(hit.collider.gameObject);
            }
            int mode = e.shift ? 2 : modeSemis;
            if (mel != null && pinceauOk && e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                Undo.IncrementCurrentGroup(); groupeSemis = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName($"{ModesSemis[mode]} : {mel.nom}");
                instancesMelange = Instances(new HashSet<GameObject>(mel.elements.Where(x => x.modele != null).Select(x => x.modele)));
                semes = retires = 0; enTrait = true; dernierPoint = pinceau;
                Passer(mel, pinceau, mode); e.Use();
            }
            else if (enTrait && e.type == EventType.MouseDrag && e.button == 0)
            {
                if (pinceauOk && Vector3.Distance(pinceau, dernierPoint) > Rayon * .3f) { dernierPoint = pinceau; Passer(mel, pinceau, mode); }
                e.Use();
            }
            else if (enTrait && (e.type == EventType.MouseUp || e.rawType == EventType.MouseUp))
            {
                enTrait = false; Undo.CollapseUndoOperations(groupeSemis);
                rapportSemis = $"{mel?.nom} : {semes} semé(s), {retires} retiré(s). Ctrl+Z annule tout le coup.";
                Notifier($"{mel?.nom} : {(semes > 0 ? $"{semes} semé(s)" : "")}{(semes > 0 && retires > 0 ? ", " : "")}{(retires > 0 ? $"{retires} retiré(s)" : "")}{(semes + retires == 0 ? "rien de plus (déjà assez serré, ou pas sur ce sol)" : "")} · Ctrl+Z pour annuler");
                e.Use(); Repaint();
            }

            if (e.type == EventType.Repaint && pinceauOk)
            {
                var couleur = mode == 0 ? new Color(.4f, 1, .5f) : mode == 1 ? new Color(1, .85f, .3f) : new Color(1, .35f, .3f);
                using (new Handles.DrawingScope(couleur))
                {
                    Handles.DrawWireDisc(pinceau, pinceauNormale, Rayon, 2);
                    Handles.DrawWireDisc(pinceau, pinceauNormale, .15f);
                }
            }
        }

        /// <summary>Un passage du pinceau en centre.</summary>
        private void Passer(SemisArene.Melange mel, Vector3 centre, int mode)
        {
            float r = Rayon;
            if (mode > 0)
            {
                // clairsemer : une partie (la densité du mélange, bornée) ; effacer : tout ce que le mélange a semé sous le pinceau
                float part = mode == 1 ? Mathf.Clamp(mel.densite, .15f, .6f) : 1;
                for (int i = instancesMelange.Count - 1; i >= 0; i--)
                {
                    var go = instancesMelange[i];
                    if (go == null) { instancesMelange.RemoveAt(i); continue; }
                    var p = go.transform.position;
                    if (new Vector2(p.x - centre.x, p.z - centre.z).magnitude > r || Random.value > part) continue;
                    Undo.DestroyObjectImmediate(go); instancesMelange.RemoveAt(i); retires++;
                }
                return;
            }
            var elements = mel.elements.Where(x => x.modele != null).ToList();
            if (elements.Count == 0 || solVise == null) return;
            var poids = elements.Select(x => x.poids).ToList();
            Physics.SyncTransforms();
            int essais = Mathf.Max(1, Mathf.CeilToInt(mel.densite * Mathf.PI * r * r * .3f));
            float graineX = mel.nom.GetHashCode() % 1000 * .37f, graineZ = mel.nom.Length * 13.1f;
            Transform groupe = null;
            for (int k = 0; k < essais; k++)
            {
                var d = Random.insideUnitCircle * r;
                var p = new Vector3(centre.x + d.x, centre.y, centre.z + d.y);
                // le sol visé seulement : sous la couronne d'un arbre, on sème à son pied, pas dans ses branches
                if (!SolSous(p, r + 2, c => ObjetDe(c.gameObject) == solVise, out var sol)) continue;
                if (Vector3.Angle(sol.normal, Vector3.up) > mel.penteMax) continue;
                if (sol.point.y < mel.altitudeMin || sol.point.y > mel.altitudeMax) continue;
                if (mel.massifs > 0)
                {
                    float bruit = Mathf.PerlinNoise(sol.point.x * .12f + graineX, sol.point.z * .12f + graineZ);
                    if (bruit < mel.massifs * .6f + (Random.value - .5f) * .08f) continue;
                }
                var ici = sol.point;
                if (instancesMelange.Any(g => g != null && new Vector2(g.transform.position.x - ici.x, g.transform.position.z - ici.z).magnitude < mel.espacement)) continue;
                if (mel.eviterLesObjets && Physics.OverlapSphere(ici + Vector3.up * .45f, .35f, ~0, QueryTriggerInteraction.Ignore)
                        .Any(c => ObjetDe(c.gameObject) != solVise)) continue;
                var el = elements[Mathf.Max(0, CourbeArene.Tirer(poids, Random.value))];
                var rot = (el.suivreLaPente ? Quaternion.FromToRotation(Vector3.up, sol.normal) : Quaternion.identity) * Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                if (groupe == null) groupe = Groupe("Semis", mel.nom);
                var go = Instancier(el.modele, ici - Vector3.up * el.enfoncement, rot, Vector3.one * Random.Range(mel.tailleMin, Mathf.Max(mel.tailleMin, mel.tailleMax)), groupe);
                instancesMelange.Add(go); semes++;
                Physics.SyncTransforms();
            }
        }
    }
}
