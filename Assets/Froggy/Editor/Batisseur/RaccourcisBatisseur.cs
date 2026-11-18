using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace Froggy.Outils
{
    /// <summary>
    /// Les touches du Bâtisseur d'arène dans la vue Scène. Maj+1 à Maj+5 ouvrent les étapes ① à ⑤ ; A tourne à gauche, E à droite
    /// (ce qu'on tient, sinon la sélection si l'option est cochée) ; M met le tampon en miroir ; T tire une autre variante ;
    /// Retour arrière retire le dernier point de la courbe.
    /// Ce sont des raccourcis à contexte, repris de Lovecraft (2026-10-08) : une touche lue par KeyDown dans la vue Scène
    /// arrive après les raccourcis d'Unity (E y prenait l'outil de rotation et lâchait le Bâtisseur). Ils ne passent devant
    /// ceux d'Unity que si la souris est sur la vue Scène (ou que la vue Scène ou le Bâtisseur a le clavier : après un clic
    /// sur une vignette, c'est la fenêtre qui l'a) et qu'un outil est pris, jamais pendant qu'on navigue (clic droit, Alt).
    /// Ils se changent dans Edit ▸ Shortcuts ▸ Froggy.
    /// </summary>
    [InitializeOnLoad]
    internal static class RaccourcisBatisseur
    {
        static RaccourcisBatisseur()
        {
            ShortcutManager.RegisterContext(new ContexteScene());
            ShortcutManager.RegisterContext(new ContexteOrienter());
            ShortcutManager.RegisterContext(new ContexteOutil());
            ShortcutManager.RegisterContext(new ContexteTracer());
        }

        /// <summary>Pour les tests (Unity n'est alors pas au premier plan : aucune fenêtre n'a le clavier ni la souris).</summary>
        internal static bool essai;

        private static bool SceneVisee => !Tools.viewToolActive && (essai || EditorWindow.mouseOverWindow is SceneView
            || EditorWindow.focusedWindow is SceneView || EditorWindow.focusedWindow is BatisseurArene);
        private static bool OutilPris => BatisseurArene.Instance != null && BatisseurArene.Instance.UnOutilPris;
        private static bool SelectionATourner => BatisseurArene.TournerLaSelection && Selection.gameObjects.Length > 0;

        internal class ContexteScene : IShortcutContext { public bool active => SceneVisee; }
        internal class ContexteOrienter : IShortcutContext { public bool active => SceneVisee && (OutilPris || SelectionATourner); }
        internal class ContexteOutil : IShortcutContext { public bool active => SceneVisee && OutilPris; }
        internal class ContexteTracer : IShortcutContext
        {
            public bool active => SceneVisee && OutilPris && BatisseurArene.Instance.OutilCourant == BatisseurArene.Outil.Tracer;
        }

        private static void Ouvrir(BatisseurArene.Etape e)
        {
            var w = BatisseurArene.Instance != null ? BatisseurArene.Instance : BatisseurArene.Fenetre();
            w.OuvrirEtape(e);
        }

        [Shortcut("Froggy/Bâtisseur/① Terrain", typeof(ContexteScene), KeyCode.Alpha1, ShortcutModifiers.Shift)] private static void Terrain() => Ouvrir(BatisseurArene.Etape.Terrain);
        [Shortcut("Froggy/Bâtisseur/② Jeu", typeof(ContexteScene), KeyCode.Alpha2, ShortcutModifiers.Shift)] private static void Jeu() => Ouvrir(BatisseurArene.Etape.Jeu);
        [Shortcut("Froggy/Bâtisseur/③ Équilibre", typeof(ContexteScene), KeyCode.Alpha3, ShortcutModifiers.Shift)] private static void Equilibre() => Ouvrir(BatisseurArene.Etape.Equilibre);
        [Shortcut("Froggy/Bâtisseur/④ Décor", typeof(ContexteScene), KeyCode.Alpha4, ShortcutModifiers.Shift)] private static void Decor() => Ouvrir(BatisseurArene.Etape.Decor);
        [Shortcut("Froggy/Bâtisseur/⑤ Tester", typeof(ContexteScene), KeyCode.Alpha5, ShortcutModifiers.Shift)] private static void Tester() => Ouvrir(BatisseurArene.Etape.Tester);

        [Shortcut("Froggy/Bâtisseur/Tourner à gauche", typeof(ContexteOrienter), KeyCode.A)] private static void TournerAGauche() => Tourner(-1);
        [Shortcut("Froggy/Bâtisseur/Tourner à droite", typeof(ContexteOrienter), KeyCode.E)] private static void TournerADroite() => Tourner(1);
        [Shortcut("Froggy/Bâtisseur/Miroir du tampon", typeof(ContexteOutil), KeyCode.M)] private static void Miroir() => BatisseurArene.Instance?.MiroirEnMain();
        [Shortcut("Froggy/Bâtisseur/Autre tirage", typeof(ContexteOutil), KeyCode.T)] private static void Autre() => BatisseurArene.Instance?.AutreEnMain();
        [Shortcut("Froggy/Bâtisseur/Retirer le dernier point", typeof(ContexteTracer), KeyCode.Backspace)] private static void DernierPoint() => BatisseurArene.Instance?.RetirerDernierPoint();

        private static void Tourner(int sens)
        {
            var w = BatisseurArene.Instance;
            if (w != null && w.TournerEnMain(sens)) return;
            if (BatisseurArene.TournerLaSelection) BatisseurArene.TournerSelection(sens);
        }
    }
}
