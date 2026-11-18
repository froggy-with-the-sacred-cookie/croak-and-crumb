using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Froggy.Outils.Tests
{
    /// <summary>
    /// Les gestes de Froggy P, faits par script Blender (Tools/blender/froggy_p.py et froggy_p_animations.py) : chaque
    /// clip est importé en humanoïde avec un avatar valide, boucle quand il le doit et a son état dans le contrôleur ; le
    /// croassement gonfle le sac vocal (forme « Gorge ») pendant le chant, de 0,30 à 0,78 s, et aucun autre geste ne le
    /// gonfle (un sac resté gonflé d'un clip à l'autre se verrait de loin).
    /// </summary>
    public class GestesFroggyPTests
    {
        private const string Dossier = "Assets/Froggy/Personnages/FroggyP/";

        public static readonly string[] Clips =
        {
            "Repos", "Marche", "Course", "Saut", "Victoire", "Atterrissage", "Torgnole", "Touche", "Repousse",
            "Croassement", "Lancer", "Gele", "Porter", "PorterBras", "Nage", "Plongeon", "SortieEau", "DefaiteDecue",
            "DefaiteComique",
        };

        private static Object[] Contenu(string nom) => AssetDatabase.LoadAllAssetsAtPath($"{Dossier}Froggy_P_{nom}.fbx");

        private static AnimationClip Clip(string nom) => Contenu(nom).OfType<AnimationClip>().FirstOrDefault(c => c.name == nom);

        private static AnimationCurve Courbe(AnimationClip clip, string propriete)
        {
            foreach (var b in AnimationUtility.GetCurveBindings(clip))
                if (b.propertyName == propriete) return AnimationUtility.GetEditorCurve(clip, b);
            return null;
        }

        [TestCaseSource(nameof(Clips))]
        public void LeClipEstHumanoideEtBouclesQuandIlLeDoit(string nom)
        {
            var clip = Clip(nom);
            Assert.That(clip, Is.Not.Null, $"pas de clip {nom} dans Froggy_P_{nom}.fbx");
            Assert.That(clip.isHumanMotion, Is.True, nom + " n'est pas humanoïde");
            var avatar = Contenu(nom).OfType<Avatar>().FirstOrDefault();
            Assert.That(avatar != null && avatar.isValid && avatar.isHuman, Is.True, "avatar de " + nom);
            Assert.That(clip.isLooping, Is.EqualTo(ImportPersonnagesFroggy.Boucles.Contains(nom)), "boucle de " + nom);
        }

        [Test]
        public void LeCroassementGonfleLaGorgePendantLeChant()
        {
            var gorge = Courbe(Clip("Croassement"), "blendShape.Gorge");
            Assert.That(gorge, Is.Not.Null, "le croassement n'anime pas la gorge");
            Assert.That(gorge.Evaluate(0.20f), Is.LessThan(1f), "gonflée avant le chant");
            Assert.That(gorge.Evaluate(0.56f), Is.GreaterThan(95f), "pas gonflée au milieu du chant");   // poids de 0 à 100
            Assert.That(gorge.Evaluate(0.90f), Is.LessThan(1f), "encore gonflée après le chant");
        }

        [TestCaseSource(nameof(Clips))]
        public void AucunAutreGesteNeGonfleLaGorge(string nom)
        {
            if (nom == "Croassement") return;
            var gorge = Courbe(Clip(nom), "blendShape.Gorge");
            float max = gorge == null ? 0f : gorge.keys.Max(k => k.value);
            Assert.That(max, Is.LessThan(1f), nom + " gonfle la gorge");
        }

        /// <summary>
        /// Les clips gardent la hauteur de leur fichier (ImportPersonnagesFroggy) : à l'atterrissage, les chevilles partent
        /// de haut, ne passent jamais sous leur hauteur de repos et y reviennent. Calées sur les pieds au départ, elles
        /// s'enfonceraient de 9 cm au contact.
        /// </summary>
        [Test]
        public void LAtterrissageNEnfoncePasLesPieds()
        {
            float repos = Echantillonner("Repos", 0f, (an, smr) => Chevilles(an))[0];
            float[] h = Echantillonner("Atterrissage", 0f, (an, smr) => Chevilles(an), .08f, .17f, .30f, .60f);
            Assert.That(h[0], Is.GreaterThan(repos + .05f), "l'atterrissage ne part pas des pieds repliés");
            Assert.That(h.Min(), Is.GreaterThan(repos - .005f), "les pieds passent sous le sol");
            Assert.That(h.Last(), Is.EqualTo(repos).Within(.005f), "l'atterrissage ne finit pas au repos");
        }

        /// <summary>Sur le modèle lui-même (la courbe retrouve son maillage), la gorge se gonfle au milieu du chant.</summary>
        [Test]
        public void LaGorgeGonfleSurLeModele()
        {
            float[] gorge = Echantillonner("Croassement", .20f,
                (an, smr) => smr.GetBlendShapeWeight(smr.sharedMesh.GetBlendShapeIndex("Gorge")), .56f, .95f);
            Assert.That(gorge[0], Is.LessThan(1f));
            Assert.That(gorge[1], Is.GreaterThan(95f));
            Assert.That(gorge[2], Is.LessThan(1f));
        }

        private static float Chevilles(Animator an) =>
            Mathf.Min(an.GetBoneTransform(HumanBodyBones.LeftFoot).position.y, an.GetBoneTransform(HumanBodyBones.RightFoot).position.y);

        /// <summary>
        /// Joue un clip sur Froggy P (le modèle du Repos) dans une scène d'aperçu et mesure à chaque instant. En mode
        /// édition, l'Animator n'écrit la pose que dans le mode animation de l'éditeur (AnimationClip.SampleAnimation et
        /// un PlayableGraph laissent un clip humanoïde au repos) : on l'ouvre et on le referme quoi qu'il arrive.
        /// </summary>
        private static float[] Echantillonner(string nom, float premier, System.Func<Animator, SkinnedMeshRenderer, float> mesure,
                                              params float[] suivants)
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            AnimationMode.StartAnimationMode();
            try
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(
                    AssetDatabase.LoadAssetAtPath<GameObject>(Dossier + "Froggy_P_Repos.fbx"), scene);
                var an = go.GetComponent<Animator>();
                var smr = go.GetComponentInChildren<SkinnedMeshRenderer>();
                var clip = Clip(nom);
                return new[] { premier }.Concat(suivants).Select(t =>
                {
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(go, clip, t);
                    AnimationMode.EndSampling();
                    return mesure(an, smr);
                }).ToArray();
            }
            finally
            {
                AnimationMode.StopAnimationMode();
                UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void LeControleurDeFroggyPAUnEtatParGeste()
        {
            var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(Dossier + "FroggyP.controller");
            Assert.That(ac, Is.Not.Null);
            var etats = ac.layers[0].stateMachine.states.Select(s => s.state.name).ToArray();
            foreach (var nom in Clips.Except(new[] { "Repos", "Marche", "Course" }))
                Assert.That(etats, Has.Member(nom), "pas d'état " + nom);
            // l'atterrissage, joué au retour au sol, rend la main à la course dès qu'on bouge (pas de pieds qui glissent)
            var atterrissage = ac.layers[0].stateMachine.states.First(s => s.state.name == "Atterrissage").state;
            Assert.That(atterrissage.transitions.Any(t => t.conditions.Any(c => c.parameter == "Vitesse")), Is.True,
                "l'atterrissage ne rend pas la main à la course");
        }
    }
}
