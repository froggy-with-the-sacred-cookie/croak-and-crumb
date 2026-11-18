using System.Collections.Generic;
using Froggy.Arene;
using Froggy.Regles;
using Froggy.Socle;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Froggy.Outils.Tests
{
    /// <summary>
    /// Torgnole (P4-10). Les règles seules (ReglesTorgnole) : cône et portée, la cible la plus proche, 5 s de recharge, coup
    /// raté compris. Puis dans une scène (PartieTorgnoles) : le joueur proche lâche un cookie et recule, on ne refrappe pas
    /// avant 5 s, un joueur intouchable ne lâche rien, et rien ne se passe partie arrêtée.
    /// </summary>
    public class TorgnoleTests
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

        private static ReglesTorgnole Regles() => new ReglesTorgnole(5f, 1.6f, 60f, 1.2f);

        private static List<(int, Vector3)> Autres(params (int, Vector3)[] a) => new List<(int, Vector3)>(a);

        [Test]
        public void LeCoupToucheDevantEtAPortee()
        {
            var regles = Regles();
            Vector3 avant = Vector3.forward;
            Assert.That(regles.Cible(1, Vector3.zero, avant, Autres((2, new Vector3(0f, 0f, 1.5f)))), Is.EqualTo(2), "devant, à 1,5 m");
            Assert.That(regles.Cible(1, Vector3.zero, avant, Autres((2, new Vector3(0f, 0f, 1.7f)))), Is.EqualTo(ReglesTorgnole.Personne), "trop loin");
            Assert.That(regles.Cible(1, Vector3.zero, avant, Autres((2, new Vector3(0f, 0f, -1f)))), Is.EqualTo(ReglesTorgnole.Personne), "derrière");
            Assert.That(regles.Cible(1, Vector3.zero, avant, Autres((2, new Vector3(1f, 0f, 0.2f)))), Is.EqualTo(ReglesTorgnole.Personne), "trop sur le côté");
            Assert.That(regles.Cible(1, Vector3.zero, avant, Autres((2, new Vector3(0f, 2f, 1f)))), Is.EqualTo(ReglesTorgnole.Personne), "un étage au-dessus");
            Assert.That(regles.Cible(1, Vector3.zero, avant, Autres((2, new Vector3(0.5f, 0f, 1.4f)), (3, new Vector3(0f, 0f, 0.9f)))),
                Is.EqualTo(3), "le plus proche des deux");
        }

        [Test]
        public void CinqSecondesAvantDeRefrapperMemeApresUnCoupRate()
        {
            var regles = Regles();
            var personne = Autres();
            Assert.That(regles.Frapper(1, Vector3.zero, Vector3.forward, personne, out int cible), Is.True, "le premier coup part");
            Assert.That(cible, Is.EqualTo(ReglesTorgnole.Personne), "dans le vide");
            for (int i = 0; i < 249; i++) regles.Pas(Dt);   // 4,98 s
            Assert.That(regles.Frapper(1, Vector3.zero, Vector3.forward, personne, out _), Is.False, "P4-10 : pas avant 5 s");
            Assert.That(regles.Frapper(2, Vector3.zero, Vector3.forward, personne, out _), Is.True, "chacun sa recharge");
            regles.Pas(Dt);
            regles.Pas(Dt);
            Assert.That(regles.Frapper(1, Vector3.zero, Vector3.forward, personne, out _), Is.True, "5 s après : de nouveau");
        }

        [Test]
        public void LaRechargeDesReglagesEstCelleDeLaCarte()
        {
            var reglages = ScriptableObject.CreateInstance<ReglagesTorgnole>();
            aDetruire.Add(reglages);
            Assert.That(reglages.recharge, Is.EqualTo(5f), "P4-10 : 5 s de recharge");
        }

        [Test]
        public void DansUneSceneLeJoueurProcheLacheUnCookie()
        {
            var reglagesCookies = ScriptableObject.CreateInstance<ReglagesCookies>();
            var reglages = ScriptableObject.CreateInstance<ReglagesTorgnole>();
            aDetruire.Add(reglagesCookies);
            aDetruire.Add(reglages);
            scene = EditorSceneManager.NewPreviewScene();

            var cookie = Objet("Cookie", new Vector3(5f, 0f, 1f)).AddComponent<RepereArene>();
            cookie.sorte = RepereArene.Sorte.Cookie;
            var froggy = Objet("Froggy", new Vector3(0f, 0.5f, 0f));   // tournée vers +z
            var cible = Objet("Cible", new Vector3(5f, 0.5f, 1f));     // sur le cookie
            var corps = cible.AddComponent<Rigidbody>();
            var cookies = Objet("Cookies", Vector3.zero).AddComponent<PartieCookies>();
            cookies.Regler(reglagesCookies);
            cookies.Demarrer(new[] { cookie }, new[] { froggy, cible });
            var torgnoles = Objet("Torgnoles", Vector3.zero).AddComponent<PartieTorgnoles>();
            torgnoles.Regler(reglages, cookies);
            var frappeur = froggy.GetComponent<PorteurCookies>();
            var touche = cible.GetComponent<PorteurCookies>();

            cookies.Pas(Dt);
            Assert.That(touche.Nombre, Is.EqualTo(1), "la cible a ramassé le cookie");
            cible.transform.position = new Vector3(0f, 0.5f, 1.2f);   // elle passe devant Froggy

            var recus = new List<EvenementPartie>();
            System.Action<EvenementPartie> ecoute = e => { if (e.Type == TypeEvenement.CoupDonne) recus.Add(e); };
            EvenementsPartie.Emis += ecoute;
            try
            {
                Assert.That(torgnoles.Frapper(frappeur), Is.SameAs(touche));
                Assert.That(touche.Nombre, Is.EqualTo(0), "P4-10 : le joueur touché lâche un cookie");
                Assert.That(corps.linearVelocity.z, Is.GreaterThan(1f), "et recule, à l'opposé du frappeur");
                Assert.That(recus.Count, Is.EqualTo(1));
                Assert.That(recus[0].Auteur, Is.SameAs(froggy));
                Assert.That(recus[0].Objet, Is.SameAs(cible));

                Assert.That(torgnoles.Frapper(frappeur), Is.Null, "pas de second coup tout de suite");
                Assert.That(recus.Count, Is.EqualTo(1));

                for (int i = 0; i < 251; i++) torgnoles.Pas(Dt);   // 5,02 s
                touche.Intouchable = true;
                int avant = cookies.Regles.Portes(touche.Joueur);
                Assert.That(torgnoles.Frapper(frappeur), Is.SameAs(touche), "le coup part");
                Assert.That(cookies.Regles.Portes(touche.Joueur), Is.EqualTo(avant), "intouchable : rien lâché");

                for (int i = 0; i < 251; i++) torgnoles.Pas(Dt);
                cookies.EnPause = true;   // compte à rebours ou partie finie
                Assert.That(torgnoles.Frapper(frappeur), Is.Null, "partie arrêtée : pas de coup");
                Assert.That(recus.Count, Is.EqualTo(2));
            }
            finally { EvenementsPartie.Emis -= ecoute; }
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
