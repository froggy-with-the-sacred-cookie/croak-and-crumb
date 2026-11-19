using System.Collections.Generic;
using Froggy.Arene;
using Froggy.Regles;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Froggy.Outils.Tests
{
    /// <summary>
    /// Règles en réseau (P2-04), sans réseau : on simule deux machines dans une scène. L'« hôte » fait tourner les règles
    /// du poste 3, le « client » est en Miroir et reçoit l'état de l'hôte (ce que PartieReseau recopie). Les deux doivent
    /// montrer la même chose, et le client ne doit jamais rien décider.
    /// </summary>
    public class ReglesReseauTests
    {
        private const float Dt = 1f / 50f;
        private static readonly Vector3[] Points = { new(0f, 0f, 0f), new(10f, 0f, 0f), new(20f, 0f, 0f) };

        private readonly List<Object> aDetruire = new();
        private Scene scene;
        private ReglagesCookies reglages;

        [TearDown]
        public void Ranger()
        {
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
            foreach (var o in aDetruire) if (o != null) Object.DestroyImmediate(o);
            aDetruire.Clear();
        }

        /// <summary>Une « machine » : sa partie de cookies et ses joueurs, aux numéros donnés (ceux du salon).</summary>
        private (PartieCookies partie, PorteurCookies[] joueurs) Machine(string nom, params int[] numeros)
        {
            if (!scene.IsValid()) scene = EditorSceneManager.NewPreviewScene();
            if (reglages == null)
            {
                reglages = ScriptableObject.CreateInstance<ReglagesCookies>();
                reglages.delaiReapparition = 0f;
                aDetruire.Add(reglages);
            }
            var reperes = new List<RepereArene>();
            foreach (var p in Points)
            {
                var r = Objet($"{nom} repère", p).AddComponent<RepereArene>();
                r.sorte = RepereArene.Sorte.Cookie;
                reperes.Add(r);
            }
            var joueurs = new PorteurCookies[numeros.Length];
            for (int i = 0; i < numeros.Length; i++)
            {
                joueurs[i] = Objet($"{nom} joueur {numeros[i]}", new Vector3(-50f - 10f * i, 0f, 0f)).AddComponent<PorteurCookies>();
                joueurs[i].Numeroter(numeros[i]);
            }
            var partie = Objet($"{nom} partie", Vector3.zero).AddComponent<PartieCookies>();
            partie.Regler(reglages);
            partie.DemarrerNumerotes(reperes, joueurs);
            return (partie, joueurs);
        }

        /// <summary>Ce que PartieReseau recopie de l'hôte vers le client.</summary>
        private static void Recopier(PartieCookies hote, PartieCookies client)
        {
            var presents = new List<bool>();
            for (int i = 0; i < hote.Regles.NombrePoints; i++) presents.Add(hote.Regles.Present(i));
            var tombes = new List<(int, Vector3)>();
            for (int i = 0; i < hote.Regles.NombreAuSol; i++) tombes.Add(hote.Regles.CookieAuSol(i));
            var scores = new List<(int, int)>();
            foreach (var p in hote.Porteurs) scores.Add((p.Joueur, hote.Regles.Portes(p.Joueur)));
            client.Refleter(presents, tombes, scores);
        }

        private Vector3 Sur(int point) => Points[point] + Vector3.up * reglages.hauteur;

        [Test]
        public void LesNumerosDuSalonSontGardes()
        {
            var (partie, joueurs) = Machine("hôte", 1, 3);   // le joueur 2 est parti
            Assert.That(partie.Trouver(3), Is.SameAs(joueurs[1]));
            Assert.That(partie.Trouver(2), Is.Null);

            joueurs[1].transform.position = Sur(0);
            partie.Pas(Dt);   // avant P2-04, porteurs[3 - 1] sortait de la liste
            Assert.That(joueurs[1].Nombre, Is.EqualTo(1), "le joueur 3 a son cookie");
            Assert.That(joueurs[1].Joueur, Is.EqualTo(3), "numéro inchangé");
        }

        [Test]
        public void MemeCookieAuMemeInstantUnSeulLObtientEtLesDeuxEcransSontDAccord()
        {
            var (hote, chezHote) = Machine("hôte", 1, 2);
            var (client, chezClient) = Machine("client", 1, 2);
            client.Miroir = true;

            // les deux joueurs exactement sur le même cookie, au même pas
            chezHote[0].transform.position = Sur(1);
            chezHote[1].transform.position = Sur(1);
            hote.Pas(Dt);
            Recopier(hote, client);

            Assert.That(chezHote[0].Nombre + chezHote[1].Nombre, Is.EqualTo(1), "un seul cookie donné");
            Assert.That(chezHote[0].Nombre, Is.EqualTo(1), "à égalité : le plus petit numéro (règle du poste 3)");
            for (int i = 0; i < 2; i++)
                Assert.That(chezClient[i].Nombre, Is.EqualTo(chezHote[i].Nombre), $"joueur {i + 1} : même score chez le client");
            for (int i = 0; i < Points.Length; i++)
                Assert.That(client.transform.GetChild(i).gameObject.activeSelf, Is.EqualTo(hote.Regles.Present(i)),
                    $"cookie {i} : même présence chez le client");
        }

        [Test]
        public void EnMiroirRienNeSeDecide()
        {
            var (client, joueurs) = Machine("client", 1, 2);
            client.Miroir = true;
            joueurs[0].transform.position = Sur(0);
            for (int i = 0; i < 20; i++) client.Pas(Dt);
            Assert.That(joueurs[0].Nombre, Is.EqualTo(0), "un client ne ramasse jamais lui-même");
            Assert.That(client.Regles.Present(0), Is.True);
        }

        [Test]
        public void UnCookieLacheApparaitPuisDisparaitChezLeClient()
        {
            var (hote, chezHote) = Machine("hôte", 1, 2);
            var (client, chezClient) = Machine("client", 1, 2);
            client.Miroir = true;

            chezHote[0].transform.position = Sur(0);
            hote.Pas(Dt);
            Assert.That(hote.FaireLacher(chezHote[0], Vector3.forward), Is.True);
            Recopier(hote, client);
            Assert.That(client.transform.childCount, Is.EqualTo(Points.Length + 1), "le cookie lâché est au sol chez le client");
            Assert.That(chezClient[0].Nombre, Is.EqualTo(0));

            // le joueur 2 le ramasse chez l'hôte
            var (_, position) = hote.Regles.CookieAuSol(0);
            chezHote[1].transform.position = position;
            hote.Pas(Dt);
            Recopier(hote, client);
            Assert.That(chezClient[1].Nombre, Is.EqualTo(1), "le joueur 2 l'a, chez le client aussi");
            Assert.That(client.transform.childCount, Is.EqualTo(Points.Length), "le cookie au sol a disparu chez le client");
        }

        [Test]
        public void UnJoueurQuiPartLacheTousSesCookiesAutourDeLui()
        {
            var (hote, chezHote) = Machine("hôte", 1, 2);
            var (client, chezClient) = Machine("client", 1, 2);
            client.Miroir = true;

            for (int i = 0; i < Points.Length; i++) { chezHote[1].transform.position = Sur(i); hote.Pas(Dt); }
            Assert.That(chezHote[1].Nombre, Is.EqualTo(3));
            chezHote[1].Intouchable = true;   // il vient de réapparaître : il part quand même avec rien

            Assert.That(hote.FaireToutLacher(chezHote[1]), Is.EqualTo(3), "P2-06 : tous ses cookies tombent");
            hote.Retirer(chezHote[1]);
            Assert.That(hote.Regles.NombreAuSol, Is.EqualTo(3));
            Assert.That(hote.Regles.Portes(2), Is.EqualTo(0));
            Assert.That(hote.Trouver(2), Is.Null, "il ne fait plus partie de la partie");
            var places = new HashSet<Vector3>();
            for (int i = 0; i < 3; i++) places.Add(hote.Regles.CookieAuSol(i).position);
            Assert.That(places.Count, Is.EqualTo(3), "en couronne, pas les uns sur les autres");

            Recopier(hote, client);
            Assert.That(client.transform.childCount, Is.EqualTo(Points.Length + 3), "le client voit les 3 cookies au sol");

            // le joueur 1 les ramasse : la partie continue sans le joueur 2
            for (int i = 0; i < 3; i++) { chezHote[0].transform.position = hote.Regles.CookieAuSol(0).position; hote.Pas(Dt); }
            Assert.That(chezHote[0].Nombre, Is.EqualTo(3));
        }

        [Test]
        public void LeClientAfficheLeMemeTempsEtLeMemeGagnant()
        {
            var (hote, chezHote) = Machine("hôte", 1, 3);
            var (client, chezClient) = Machine("client", 1, 3);
            var reglagesPartie = ScriptableObject.CreateInstance<ReglagesPartie>();
            reglagesPartie.decompte = 0f;
            reglagesPartie.duree = 1f;
            aDetruire.Add(reglagesPartie);

            var deroulementHote = hote.gameObject.AddComponent<DeroulementPartie>();
            deroulementHote.Regler(reglagesPartie, hote);
            deroulementHote.Preparer();
            deroulementHote.Lancer();
            var deroulementClient = client.gameObject.AddComponent<DeroulementPartie>();
            deroulementClient.Regler(reglagesPartie, client);
            deroulementClient.Miroir = true;

            chezHote[1].transform.position = Sur(2);   // le joueur 3 prend un cookie
            for (int i = 0; i < 80; i++)
            {
                deroulementHote.Pas(Dt);
                hote.Pas(Dt);
                deroulementClient.Pas(Dt);   // en miroir : sans effet
                var r = deroulementHote.Regles;
                deroulementClient.Refleter(r.Phase, r.Reste, r.Prolonge, r.Gagnant);
            }

            Assert.That(deroulementHote.Regles.Phase, Is.EqualTo(Phase.Finie));
            Assert.That(deroulementClient.Regles.Phase, Is.EqualTo(Phase.Finie));
            Assert.That(deroulementHote.Gagnant, Is.SameAs(chezHote[1]), "le joueur 3 gagne chez l'hôte");
            Assert.That(deroulementClient.Gagnant, Is.SameAs(chezClient[1]), "et chez le client, sur son propre joueur 3");
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
