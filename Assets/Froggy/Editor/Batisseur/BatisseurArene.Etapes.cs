using System;
using System.Collections.Generic;
using System.Linq;
using Froggy.Arene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Sorte = Froggy.Arene.RepereArene.Sorte;

namespace Froggy.Outils
{
    /// <summary>
    /// La fenêtre rangée par ce qu'on veut faire, pas par outil (2026-10-29, demandé par Florent pour une équipe qui n'a
    /// jamais fait de jeu) : cinq étapes, ① Terrain, ② Jeu, ③ Équilibre, ④ Décor, ⑤ Tester, chacune avec son but en une
    /// phrase et seulement ses outils. En tête, la liste « Prête pour un test » (les critères de P8-02) et la prochaine
    /// chose à faire. Les réglages rares sont repliés (« Plus de réglages », ou le mode avancé qui montre tout). Dans la vue
    /// Scène : un bandeau près du curseur dit ce que fera le clic, une notification confirme chaque geste (avec Ctrl+Z),
    /// et une légende explique les couleurs de la lecture de l'arène.
    /// Méthodes : parcours utilisateur, divulgation progressive, état du système visible, reconnaissance plutôt que
    /// rappel, états vides qui disent quoi faire, prévention des erreurs (heuristiques de Nielsen).
    /// </summary>
    public partial class BatisseurArene
    {
        public enum Etape { Terrain, Jeu, Equilibre, Decor, Tester }

        private static readonly string[] NomsEtapes = { "① Terrain", "② Jeu", "③ Équilibre", "④ Décor", "⑤ Tester" };
        private static readonly string[] ButsEtapes =
        {
            "Le sol, l'eau, les nénuphars, les murs : là où l'on court et où l'on saute. Commence simple, le décor viendra après.",
            "Où apparaissent les joueurs, les cookies, les boîtes à objets et le gobelin : ce qui fait une partie.",
            "Est-ce juste pour chacun ? Le Bâtisseur mesure les trajets de chaque joueur et dit quoi corriger.",
            "Les arbres, les lanternes, les buissons : l'habillage. Il ne change pas les règles, mais il peut gêner les trajets.",
            "Jouer tout de suite là où tu regardes, garder une version pour comparer, chercher les erreurs.",
        };
        private static readonly Outil[][] OutilsEtapes =
        {
            new[] { Outil.Poser, Outil.Tracer }, new[] { Outil.Jeu }, new Outil[0],
            new[] { Outil.Poser, Outil.Semer, Outil.Tracer, Outil.Tampons }, new Outil[0],
        };
        private static readonly string[] NomsOnglets = { "Poser", "Semer (pinceau)", "Tracer (courbe)", "Tampons", "Repères" };
        private static readonly string[] IconesEtapes = { "TerrainInspector.TerrainToolRaise", "AvatarPivot", "Search Icon", "TerrainInspector.TerrainToolTrees", "PlayButton" };
        private static readonly string[] IconesOutils = { "Prefab Icon", "TerrainInspector.TerrainToolSplat", "UnityEditor.AnimationWindow", "TreeEditor.Duplicate", "AvatarPivot" };

        /// <summary>Une icône intégrée d'Unity (version sombre ou claire selon l'éditeur), ou rien si elle n'existe pas.</summary>
        private static Texture Icone(string nom)
        {
            var t = EditorGUIUtility.isProSkin ? EditorGUIUtility.FindTexture("d_" + nom) : null;
            return t != null ? t : EditorGUIUtility.FindTexture(nom);
        }

        public static Etape EtapeChoisie
        {
            get => (Etape)Mathf.Clamp(EditorPrefs.GetInt(Pref + "etape", 0), 0, 4);
            set => EditorPrefs.SetInt(Pref + "etape", (int)value);
        }

        /// <summary>Le mode avancé montre tous les réglages ; sinon ils sont repliés sous « Plus de réglages ».</summary>
        public static bool ModeAvance { get => EditorPrefs.GetBool(Pref + "avance", false); set => EditorPrefs.SetBool(Pref + "avance", value); }

        /// <summary>Les réglages rares d'un outil : montrés en mode avancé, sinon dans un petit tiroir.</summary>
        private static bool Plus(string cle) => ModeAvance || SousTiroir("plus." + cle, "Plus de réglages");

        /// <summary>Ouvre une étape et reprend son dernier outil (aucun pour Équilibre et Tester : la vue Scène revient à Unity).</summary>
        public void OuvrirEtape(Etape e)
        {
            EtapeChoisie = e;
            var outils = OutilsEtapes[(int)e];
            if (outils.Length == 0) { if (actif) Lacher(); }
            else
            {
                var dernier = (Outil)EditorPrefs.GetInt(Pref + "outil." + e, (int)outils[0]);
                Prendre(outils.Contains(dernier) ? dernier : outils[0]);
            }
            if (e == Etape.Equilibre && grille == null) analysePerimee = 0;   // mesurée tout de suite
            Repaint(); SceneView.RepaintAll();
        }

        private void BarreEtapes()
        {
            int e = (int)EtapeChoisie;
            var contenus = NomsEtapes.Select((nom, i) => new GUIContent(" " + nom, Icone(IconesEtapes[i]), ButsEtapes[i] + $" (Maj+{i + 1})")).ToArray();
            // étroite (contre l'Inspecteur) : deux rangées plutôt que des noms coupés
            // avec les icônes, une rangée demande environ 680 points : en dessous, deux rangées (sinon tout déborde à droite)
            int n = position.width < 700 ? GUILayout.SelectionGrid(e, contenus, 3, GUILayout.Height(48)) : GUILayout.Toolbar(e, contenus, GUILayout.Height(26));
            if (n != e) { OuvrirEtape((Etape)n); ClavierALaScene(); }
            EditorGUILayout.LabelField(ButsEtapes[(int)EtapeChoisie], EditorStyles.wordWrappedMiniLabel);
        }

        /// <summary>Les outils de l'étape, en onglets, et ce que fait la souris avec celui qui est pris.</summary>
        private void OngletsOutils()
        {
            var outils = OutilsEtapes[(int)EtapeChoisie];
            using (new EditorGUILayout.HorizontalScope())
                foreach (var o in outils)
                {
                    bool pris = actif && outil == o;
                    var avant = GUI.backgroundColor;
                    if (pris) GUI.backgroundColor = new Color(.55f, .85f, .55f);
                    if (GUILayout.Button(new GUIContent(" " + NomsOnglets[(int)o], Icone(IconesOutils[(int)o]), AidesOutils[(int)o]), outil == o ? BoutonMontre : BoutonOutil))
                    {
                        if (pris) Lacher(); else { Prendre(o); ClavierALaScene(); }
                    }
                    GUI.backgroundColor = avant;
                }
            EditorGUILayout.LabelField(actif ? Gestes() : "L'outil est posé : clique son onglet pour le reprendre.", EditorStyles.wordWrappedMiniLabel);
        }

        // ---------- Prête pour un test ----------

        private class Critere { public string texte, prochaine; public bool? ok; public string action; public Action faire; }
        private List<Critere> liste; private List<VerificationArene.Constat> gravesListe;

        /// <summary>Les critères d'une arène prête pour un test (ceux de P8-02), pour la partie choisie.</summary>
        private List<Critere> ListePrete()
        {
            int n = PartieA;
            var reps = Reperes();
            var l = new List<Critere>();
            var joueurs = reps.Where(r => r.sorte == Sorte.Joueur && r.SertA(n)).Select(r => r.joueur).Distinct().ToList();
            l.Add(new Critere
            {
                texte = $"Les {n} apparitions de joueurs" + (joueurs.Count == n ? "" : $" (il y en a {joueurs.Count})"),
                ok = joueurs.Count == n, action = "Les poser",
                prochaine = $"pose les {n} apparitions de joueurs (il en manque {n - joueurs.Count}) à l'étape ② Jeu",
                faire = () => { OuvrirEtape(Etape.Jeu); sorteRepere = Sorte.Joueur; joueurRepere = JoueurManquant(); },
            });
            int cookies = reps.Count(r => r.sorte == Sorte.Cookie && r.SertA(n));
            l.Add(new Critere
            {
                texte = cookies > 0 ? $"Des cookies ({cookies} apparition(s))" : "Des cookies à ramasser",
                ok = cookies > 0, action = "En poser", prochaine = "pose des apparitions de cookies à l'étape ② Jeu", faire = () => { OuvrirEtape(Etape.Jeu); sorteRepere = Sorte.Cookie; },
            });
            var m = grille != null ? Mesurer(n) : null;
            l.Add(new Critere
            {
                texte = m == null ? "Tout est accessible (pas encore mesuré)" : m.inaccessibles.Count == 0 ? "Tout est accessible"
                    : $"Tout est accessible ({m.inaccessibles.Count} repère(s) hors d'atteinte)",
                ok = m == null ? (bool?)null : m.inaccessibles.Count == 0, action = m == null ? "Mesurer" : "Voir",
                prochaine = "un repère est hors d'atteinte : l'étape ③ Équilibre dit lequel et comment le rejoindre",
                faire = () => OuvrirEtape(Etape.Equilibre),
            });
            float ecart = m == null ? float.NaN : m.Ecart(0);
            l.Add(new Critere
            {
                texte = float.IsNaN(ecart) ? "Équitable (pas encore mesuré)" : $"Équitable : {ecart:P0} d'écart pour le premier cookie",
                ok = float.IsNaN(ecart) ? (bool?)null : ecart < .2f, action = float.IsNaN(ecart) ? "Mesurer" : "Comment corriger",
                prochaine = $"les joueurs ne sont pas à égalité ({ecart:P0} d'écart) : l'étape ③ Équilibre dit quoi déplacer",
                faire = () => OuvrirEtape(Etape.Equilibre),
            });
            int graves = gravesListe?.Count ?? -1;
            l.Add(new Critere
            {
                texte = graves < 0 ? "Aucune erreur grave (pas encore vérifié)" : graves == 0 ? "Aucune erreur grave" : $"Aucune erreur grave ({graves} trouvée(s))",
                ok = graves < 0 ? (bool?)null : graves == 0, action = "Vérifier", prochaine = $"corrige les erreurs graves ({graves}) trouvées à l'étape ⑤ Tester",
                faire = () => { OuvrirEtape(Etape.Tester); VerifierMaintenant(); },
            });
            return l;
        }

        private void VerifierMaintenant()
        {
            constats = VerificationArene.Verifier(SeuilFlotte, Ignorer.Split(';').Select(s => s.Trim()).ToArray(), out bilanVerif);
            gravesListe = constats.Where(c => c.grave).ToList();
        }

        private static GUIStyle titreArene;
        private static GUIStyle TitreArene => titreArene ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };

        private void EnTeteArene()
        {
            if (gravesListe == null) VerifierMaintenant();   // la première fois : la liste ne reste pas sur « pas encore vérifié »
            liste = ListePrete();
            int faits = liste.Count(c => c.ok == true);
            var scene = EditorSceneManager.GetActiveScene();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(string.IsNullOrEmpty(scene.name) ? "Arène sans nom" : scene.name, TitreArene);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent("Apprendre", "Le bac à sable : 10 minutes, cinq missions guidées, dans une scène d'essai (ta scène est rouverte à la fin)"),
                    EditorStyles.miniButton, GUILayout.Width(70))) Apprendre();
                int n = EditorGUILayout.IntPopup(PartieA, new[] { "à 2 joueurs", "à 3 joueurs", "à 4 joueurs" }, new[] { 2, 3, 4 }, GUILayout.Width(95));
                if (n != PartieA) { PartieA = n; calqueAJour = false; SceneView.RepaintAll(); }
            }
            // la jauge : un carré par critère
            var r = GUILayoutUtility.GetRect(10, 18, GUILayout.ExpandWidth(true));
            var rl = new Rect(r.x, r.y + 2, 130, 14);
            GUI.Label(rl, $"Prête pour un test : {faits} / {liste.Count}", EditorStyles.miniBoldLabel);
            for (int i = 0; i < liste.Count; i++)
            {
                var c = liste[i].ok == true ? new Color(.35f, .8f, .4f) : liste[i].ok == false ? new Color(.9f, .45f, .3f) : new Color(.5f, .5f, .5f);
                EditorGUI.DrawRect(new Rect(rl.xMax + 4 + i * 18, r.y + 3, 14, 12), c);
            }
            if (SousTiroir("liste", "Ce qu'il reste à faire"))
                foreach (var c in liste)
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label(c.ok == true ? "✓" : c.ok == false ? "✗" : "?", c.ok == false ? Rouge : EditorStyles.miniBoldLabel, GUILayout.Width(14));
                        EditorGUILayout.LabelField(c.texte, EditorStyles.wordWrappedMiniLabel);
                        if (c.ok != true && GUILayout.Button(c.action, EditorStyles.miniButton, GUILayout.Width(110))) c.faire();
                    }
        }

        /// <summary>La prochaine chose à faire : le premier critère qui manque, avec son bouton.</summary>
        private void ProchaineEtape()
        {
            var c = liste?.FirstOrDefault(x => x.ok == false);
            if (c == null) return;
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Prochaine étape : " + (c.prochaine ?? c.texte) + ".", EditorStyles.wordWrappedLabel);
                if (GUILayout.Button(c.action, GUILayout.Width(110), GUILayout.Height(20))) c.faire();
            }
        }

        // ---------- Les réglages du Bâtisseur ----------

        private void ReglagesGUI()
        {
            if (!SousTiroir("reglages", "Réglages du Bâtisseur")) return;
            ModeAvance = EditorGUILayout.ToggleLeft(new GUIContent("Mode avancé : montrer tous les réglages",
                "Décoché, les réglages rares de chaque outil sont rangés sous « Plus de réglages »"), ModeAvance);
            EditorGUI.BeginChangeCheck();
            var r = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Ranger sous",
                "Le groupe de la scène où le Bâtisseur range ce qu'il pose (vide : « Décor », créé au besoin)"), Racine(false), typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck()) ChoisirRacine(r);
            int i = EditorGUILayout.Popup(new GUIContent("A / E tournent de", "Le pas de rotation des touches A et E"),
                Mathf.Max(0, Array.IndexOf(Pas, PasRotation)), Pas.Select(p => p + "°").ToArray());
            PasRotation = Pas[i];
            TournerLaSelection = EditorGUILayout.ToggleLeft(new GUIContent("A / E tournent aussi la sélection (sans outil pris)",
                "Décoché : sans outil pris, E reste l'outil de rotation d'Unity"), TournerLaSelection);
            EditorGUILayout.LabelField("Touches : Maj+1 à Maj+5 ouvrent les étapes ; elles se changent dans Edit ▸ Shortcuts ▸ Froggy.", EditorStyles.wordWrappedMiniLabel);
        }

        // ---------- Tester ----------

        private static GUIStyle grosBouton;
        private static GUIStyle GrosBouton => grosBouton ??= new GUIStyle(GUI.skin.button) { fontSize = 13, fontStyle = FontStyle.Bold, fixedHeight = 32 };

        private void TesterGUI()
        {
            if (GUILayout.Button(new GUIContent("▶  Jouer ici (F5)", "Lance la scène, Froggy au centre de la vue Scène"), GrosBouton)) JouerIci.LancerAuCentre();
            EditorGUILayout.LabelField("Astuce : la souris sur la vue Scène, F5 fait apparaître Froggy juste sous la souris. F5 en jeu arrête.", EditorStyles.wordWrappedMiniLabel);
            if (JouerIci.OuJetais != null && GUILayout.Button("Aller là où je me suis arrêté en jeu")) JouerIci.AllerOuJetais();
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Garder une version pour comparer", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Avant de retoucher, garde la disposition actuelle ; après un test, reviens à celle qui marchait le mieux.", EditorStyles.wordWrappedMiniLabel);
            VersionsGUI(false);
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Chercher les erreurs", EditorStyles.boldLabel);
            VerifierGUI(false);
        }

        // ---------- Dans la vue Scène : le bandeau, les notifications, la légende ----------

        /// <summary>Confirme un geste dans la vue Scène (et dit comment l'annuler).</summary>
        internal static void Notifier(string message)
        {
            var sv = SceneView.lastActiveSceneView;
            if (sv != null) sv.ShowNotification(new GUIContent(message), 2.5);
        }

        private static GUIStyle bandeau;
        private static GUIStyle Bandeau => bandeau ??= new GUIStyle(EditorStyles.helpBox) { fontSize = 11, richText = true, padding = new RectOffset(8, 8, 5, 5) };

        /// <summary>Près du curseur : l'outil pris et ce que fera le clic.</summary>
        private void DessinerBandeau(SceneView sv, Event e)
        {
            if (e.type != EventType.Repaint) return;
            var texte = AideCurseur(e);
            if (string.IsNullOrEmpty(texte)) return;
            Handles.BeginGUI();
            var contenu = new GUIContent(texte);
            var taille = Bandeau.CalcSize(contenu);
            var p = e.mousePosition + new Vector2(18, 18);
            p.x = Mathf.Min(p.x, sv.position.width - taille.x - 6); p.y = Mathf.Min(p.y, sv.position.height - taille.y - 30);
            var avant = GUI.backgroundColor; GUI.backgroundColor = new Color(0, 0, 0, .75f);
            GUI.Label(new Rect(p, taille), contenu, Bandeau);
            GUI.backgroundColor = avant;
            Handles.EndGUI();
        }

        private string AideCurseur(Event e)
        {
            const string arreter = "Échap : arrêter";
            switch (outil)
            {
                case Outil.Poser:
                    if (e.shift) return viseRetrait != null ? $"<b>Clic : retirer « {viseRetrait.name} »</b>" : "Vise l'objet à retirer";
                    if (enMain == null) return "Choisis un objet dans la palette (fenêtre Bâtisseur)";
                    int n = CourbeArene.NombreImages(SymetrieChoisie);
                    return $"<b>Clic : poser « {enMain.name} »</b>{(n > 1 ? $" (et {n - 1} copie(s) par symétrie)" : "")}\nA / E : tourner · Maj+clic : retirer · {arreter}";
                case Outil.Semer:
                    var mel = Melange(); if (mel == null) return "Crée un mélange dans la fenêtre";
                    return e.shift ? $"<b>Glisser : effacer « {mel.nom} »</b>" : $"<b>Glisser : {ModesSemis[modeSemis].ToLowerInvariant()} « {mel.nom} »</b>\nMaj+glisser : effacer · {arreter}";
                case Outil.Tracer:
                    var t = Trace(); string nom = t != null ? t.nom : "le tracé";
                    if (points.Count == 0) return $"<b>Clic : premier point de la courbe</b> ({nom})\n{arreter}";
                    if (points.Count == 1) return "<b>Clic : point suivant</b>\nRetour arrière : retirer le dernier · Échap : effacer la courbe";
                    return $"<b>Entrée : poser « {nom} »</b> ({planTrace?.Count ?? 0} pièces)\nClic : un point de plus · glisser un point · Maj+clic : le retirer · Échap : effacer";
                case Outil.Tampons:
                    if (tamponEnMain == null) return "Choisis un tampon dans la fenêtre";
                    return $"<b>Clic : poser « {tamponEnMain.name} »</b>\nA / E : tourner · M : miroir · T : autre variante · {arreter}";
                default:
                    if (e.shift) return repereVise != null ? $"<b>Clic : retirer « {repereVise.Nom} »</b>" : "Vise le repère à retirer";
                    if (problemeRepere != null)
                        return sorteRepere == Sorte.Joueur ? $"<b>Pas ici : {problemeRepere}</b>\nUn joueur doit apparaître sur un sol où il tient debout."
                            : $"<b>Attention : {problemeRepere}</b>\nTu peux le poser quand même ; l'étape ③ Équilibre dira si on l'atteint.";
                    string quoi = sorteRepere == Sorte.Joueur ? $"l'apparition de J{joueurRepere}" : sorteRepere == Sorte.Cookie ? "une apparition de cookies"
                        : sorteRepere == Sorte.Boite ? "une boîte à objets" : sorteRepere == Sorte.Gobelin ? "le départ du gobelin" : "le centre de l'arène";
                    int k = CourbeArene.NombreImages(SymetrieChoisie);
                    return $"<b>Clic : poser {quoi}</b>{(k > 1 ? $" (et {k - 1} autre(s) par symétrie)" : "")}\nA / E : tourner · Maj+clic : retirer · {arreter}";
            }
        }

        public static bool LegendeVisible { get => EditorPrefs.GetBool(Pref + "legende", true); set => EditorPrefs.SetBool(Pref + "legende", value); }

        private static bool LegendeOuverte { get => EditorPrefs.GetBool(Pref + "legendeOuverte", true); set => EditorPrefs.SetBool(Pref + "legendeOuverte", value); }

        /// <summary>
        /// La légende des couleurs de la lecture de l'arène, en haut à droite de la vue Scène : compacte (deux colonnes) et
        /// repliable d'un clic en une pastille « Légende ▸ ». Un coin fixe finit toujours par cacher une apparition quand on
        /// zoome (2026-10-29 : en bas à gauche J3, en haut à droite J1).
        /// </summary>
        private void DessinerLegende(SceneView sv)
        {
            var e = Event.current;
            if (grille == null || CalqueChoisi == Calque.Aucun || !LegendeVisible) return;
            if (e.type != EventType.Repaint && e.type != EventType.MouseDown) return;
            var gauche = new List<(Color c, string t)>(); var droite = new List<(Color c, string t)>();
            string titre;
            if (CalqueChoisi == Calque.Territoires)
            {
                titre = $"Qui arrive le premier (à {PartieA})";
                foreach (var j in reperes.Where(r => r != null && r.sorte == Sorte.Joueur && r.SertA(PartieA)).OrderBy(r => r.joueur))
                    gauche.Add((RepereArene.CouleursJoueurs[j.joueur - 1], $"{"●▲■◆"[j.joueur - 1]} J{j.joueur}"));
                droite.Add((Color.white, "disputé (< 0,5 s)")); droite.Add((new Color(.4f, .4f, .4f), "personne n'y arrive"));
            }
            else if (CalqueChoisi == Calque.Temps)
            {
                titre = $"Temps depuis J{JoueurCalque}";
                gauche.Add((new Color(.25f, .85f, .35f), "tout près")); gauche.Add((new Color(1, .9f, .25f), "à mi-chemin"));
                gauche.Add((new Color(.95f, .25f, .2f), "le plus loin")); droite.Add((new Color(.4f, .4f, .4f), "inaccessible"));
            }
            else titre = "Les sauts";
            droite.Add((new Color(.35f, .9f, .4f), "saut facile")); droite.Add((new Color(1, .7f, .2f), "saut juste (< 50 cm)"));
            droite.Add((new Color(1, .3f, .25f), "saut raté de peu"));
            string meteo = MeteoAnalyse == GabaritFroggy.Meteo.Pluie ? "pluie" : MeteoAnalyse == GabaritFroggy.Meteo.Neige ? "neige" : "temps normal";
            var entete = $"{titre} · {meteo}  ▾";

            Handles.BeginGUI();
            var style = EditorStyles.whiteMiniLabel;
            if (!LegendeOuverte)
            {
                var pastille = new Rect(sv.position.width - 92, 150, 80, 18);
                if (e.type == EventType.MouseDown && pastille.Contains(e.mousePosition)) { LegendeOuverte = true; e.Use(); sv.Repaint(); }
                else if (e.type == EventType.Repaint) { EditorGUI.DrawRect(pastille, new Color(0, 0, 0, .55f)); GUI.Label(new Rect(pastille.x + 8, pastille.y + 2, 80, 16), "Légende ▸", style); }
                Handles.EndGUI(); return;
            }
            float col = 130, lignes = Mathf.Max(gauche.Count, droite.Count);
            float w = Mathf.Max(col * 2 + 16, style.CalcSize(new GUIContent(entete)).x + 16), h = 22 + lignes * 16;
            var cadre = new Rect(sv.position.width - w - 12, 150, w, h);
            if (e.type == EventType.MouseDown && new Rect(cadre.x, cadre.y, cadre.width, 20).Contains(e.mousePosition)) { LegendeOuverte = false; e.Use(); sv.Repaint(); Handles.EndGUI(); return; }
            if (e.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(cadre, new Color(.1f, .1f, .1f, .85f));
                GUI.Label(new Rect(cadre.x + 8, cadre.y + 3, w, 16), entete, style);
                void Colonne(List<(Color c, string t)> l, float x)
                {
                    for (int i = 0; i < l.Count; i++)
                    {
                        float y = cadre.y + 21 + i * 16;
                        EditorGUI.DrawRect(new Rect(x, y + 2, 12, 10), l[i].c);
                        GUI.Label(new Rect(x + 16, y, col - 18, 16), l[i].t, style);
                    }
                }
                Colonne(gauche, cadre.x + 8); Colonne(droite, cadre.x + 8 + col);
            }
            Handles.EndGUI();
        }
    }
}
