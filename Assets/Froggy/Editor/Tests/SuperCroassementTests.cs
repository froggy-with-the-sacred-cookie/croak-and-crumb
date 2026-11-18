using System.Collections.Generic;
using Froggy.Objets;
using Froggy.Regles;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Froggy.Outils.Tests
{
    /// <summary>
    /// Super croassement (P4-02). Les règles seules : l'onde repousse vers l'extérieur les joueurs au sol et à portée, pas
    /// ceux qui sautent, jamais celui qui croasse. Puis l'effet dans une scène : élan, puis poussée ; un joueur en l'air
    /// esquive ; un joueur intouchable n'est pas repoussé ; l'onde est annoncée (pour le camouflage).
    /// </summary>
    public class SuperCroassementTests
    {
        private const float Dt = 1f / 50f;
        private readonly List<Object> aDetruire = new List<Object>();
        private Scene scene;

        [TearDown]
        public void Ranger()
        {
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
            foreach (var o in aDetruire) if (o != null) Object.DestroyImmediate(o);
            aDetruire.Clear();
        }

        [Test]
        public void LOndeRepousseLesJoueursAuSolEtAPortee()
        {
            var regles = new ReglesSuperCroassement(4f);
            var sortie = new List<ReglesSuperCroassement.Pousse>();
            var autres = new List<(int, Vector3, bool)>
            {
                (1, Vector3.zero, true),                    // celui qui croasse
                (2, new Vector3(3f, 0f, 0f), true),         // à portée, au sol
                (3, new Vector3(0f, 2f, -2f), false),       // à portée, en l'air : il esquive
                (4, new Vector3(5f, 0f, 0f), true),         // trop loin
            };
            regles.Cibles(1, Vector3.zero, Vector3.forward, autres, sortie);
            Assert.That(sortie.Count, Is.EqualTo(1));
            Assert.That(sortie[0].Joueur, Is.EqualTo(2));
            Assert.That(Vector3.Distance(sortie[0].Direction, Vector3.right), Is.LessThan(1e-4f), "vers l'extérieur");
        }

        [Test]
        public void DansUneSceneLElanPuisLaPoussee()
        {
            var reglages = ScriptableObject.CreateInstance<ReglagesSuperCroassement>();
            aDetruire.Add(reglages);
            scene = EditorSceneManager.NewPreviewScene();
            var sol = Objet("Sol", new Vector3(0f, -0.5f, 0f));
            sol.AddComponent<BoxCollider>().size = new Vector3(40f, 1f, 40f);
            var froggy = Objet("Froggy", new Vector3(0f, 0.5f, 0f));
            var voisin = Corps("Voisin", new Vector3(2f, 0.5f, 0f));          // au sol, à 2 m
            var sauteur = Corps("Sauteur", new Vector3(-2f, 2.5f, 0f));       // à 2 m, mais en l'air
            var protege = Corps("Protégé", new Vector3(0f, 0.5f, 2f));        // vient de réapparaître
            protege.gameObject.AddComponent<PorteurCookies>().Intouchable = true;
            var loin = Corps("Loin", new Vector3(10f, 0.5f, 0f));
            var cibles = new[] { voisin, sauteur, protege, loin };

            var effet = Objet("Effet", Vector3.zero).AddComponent<EffetSuperCroassement>();
            effet.Regler(reglages);
            Vector3 centre = Vector3.one * 99f;
            effet.Onde += (c, r) => centre = c;

            var repoussees = effet.Croasser(froggy, cibles);
            Assert.That(repoussees, Is.EqualTo(new[] { voisin }), "seul le voisin au sol est repoussé");
            Assert.That(voisin.linearVelocity.x, Is.EqualTo(reglages.poussee).Within(1e-3f), "vers l'extérieur");
            Assert.That(voisin.linearVelocity.y, Is.EqualTo(reglages.montee).Within(1e-3f), "et décollé du sol");
            Assert.That(sauteur.linearVelocity, Is.EqualTo(Vector3.zero), "sauter au bon moment esquive");
            Assert.That(protege.linearVelocity, Is.EqualTo(Vector3.zero), "intouchable");
            Assert.That(loin.linearVelocity, Is.EqualTo(Vector3.zero));
            Assert.That(centre, Is.EqualTo(froggy.transform.position), "l'onde est annoncée, pour le camouflage");
        }

        [Test]
        public void LOndeNePartQuApresLElan()
        {
            var reglages = ScriptableObject.CreateInstance<ReglagesSuperCroassement>();
            aDetruire.Add(reglages);
            scene = EditorSceneManager.NewPreviewScene();
            var froggy = Objet("Froggy", Vector3.zero);
            var effet = Objet("Effet", Vector3.zero).AddComponent<EffetSuperCroassement>();
            effet.Regler(reglages, false, new Rigidbody[0]);
            int ondes = 0;
            effet.Onde += (c, r) => ondes++;

            effet.Lancer(froggy);
            int pas = Mathf.RoundToInt(reglages.elan / Dt);
            for (int i = 0; i < pas - 2; i++) effet.Pas(Dt);
            Assert.That(ondes, Is.EqualTo(0), "pendant l'élan, rien ne part : le temps de sauter");
            Assert.That(effet.ElansEnCours, Is.EqualTo(1));
            for (int i = 0; i < 4; i++) effet.Pas(Dt);
            Assert.That(ondes, Is.EqualTo(1), "puis l'onde part, une fois");
            Assert.That(effet.ElansEnCours, Is.EqualTo(0));
        }

        private Rigidbody Corps(string nom, Vector3 position)
        {
            var go = Objet(nom, position);
            go.AddComponent<BoxCollider>();
            return go.AddComponent<Rigidbody>();
        }

        private GameObject Objet(string nom, Vector3 position)
        {
            var go = new GameObject(nom);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.position = position;
            return go;
        }
    }
}
