using System;
using System.Collections.Generic;
using Froggy.Deplacement;
using Froggy.Regles;
using Froggy.Socle;
using UnityEngine;

namespace Froggy.Objets
{
    /// <summary>
    /// L'effet du super croassement (P4-02), dans une scène, sans réseau. Quand une Froggy utilise l'objet (événement
    /// « objet utilisé » du socle), elle prend un élan (sa gorge gonfle, les autres voient venir le coup), puis une onde
    /// repousse les joueurs au sol autour d'elle : un joueur au bord de l'eau y tombe, un joueur qui saute au bon moment
    /// esquive. Un joueur intouchable (P3-04) n'est pas repoussé. Les règles sont dans ReglesSuperCroassement.
    /// L'onde est annoncée par « Onde » (centre, rayon) : le camouflage (P4-07) l'écoutera pour révéler les camouflés.
    /// Le visuel (gorge, cercle) est PROVISOIRE, en attendant les effets du poste 6 ; pas encore de son.
    /// </summary>
    public class EffetSuperCroassement : MonoBehaviour
    {
        [SerializeField] private ReglagesSuperCroassement reglages;
        [Tooltip("Une boule qui gonfle à la gorge pendant l'élan et un cercle qui s'élargit avec l'onde. PROVISOIRE.")]
        [SerializeField] private bool visuelProvisoire = true;

        private const float DureeCercle = 0.3f;

        private class Elan { public GameObject auteur; public float reste; public Transform gorge; }

        private readonly List<Elan> elans = new List<Elan>();
        private readonly List<(LineRenderer cercle, Vector3 centre, float age)> cercles = new List<(LineRenderer, Vector3, float)>();
        private readonly List<(int, Vector3, bool)> autres = new List<(int, Vector3, bool)>();
        private readonly List<Rigidbody> corps = new List<Rigidbody>();
        private readonly List<ReglesSuperCroassement.Pousse> pousses = new List<ReglesSuperCroassement.Pousse>();
        private readonly RaycastHit[] touches = new RaycastHit[8];
        private Material matiere;
        private IEnumerable<Rigidbody> ciblesImposees;

        /// <summary>L'onde vient de partir : son centre et son rayon.</summary>
        public event Action<Vector3, float> Onde;

        /// <summary>Les élans en cours (un test ou un HUD peut les lire).</summary>
        public int ElansEnCours => elans.Count;

        /// <summary>Pour un test : des réglages à soi, sans visuel, et les cibles des ondes (sinon tous les joueurs de la scène).</summary>
        public void Regler(ReglagesSuperCroassement r, bool visuel = false, IEnumerable<Rigidbody> cibles = null)
        {
            reglages = r;
            visuelProvisoire = visuel;
            ciblesImposees = cibles;
        }

        private void OnEnable() => EvenementsPartie.Emis += Ecouter;

        private void OnDisable()
        {
            EvenementsPartie.Emis -= Ecouter;
            foreach (var e in elans) if (e.gorge != null) Destroy(e.gorge.gameObject);
            elans.Clear();
            foreach (var c in cercles) if (c.cercle != null) Destroy(c.cercle.gameObject);
            cercles.Clear();
        }

        private void Ecouter(EvenementPartie e)
        {
            if (e.Type != TypeEvenement.ObjetUtilise || e.Auteur == null || reglages == null || reglages.fiche == null) return;
            var porteur = e.Auteur.GetComponent<PorteurObjet>();
            if (porteur != null && porteur.DernierUtilise == reglages.fiche) Lancer(e.Auteur);
        }

        /// <summary>Lance l'élan de cette Froggy ; l'onde part au bout de « elan » secondes.</summary>
        public void Lancer(GameObject auteur)
        {
            if (auteur == null || reglages == null) return;
            var elan = new Elan { auteur = auteur, reste = reglages.elan };
            if (visuelProvisoire) elan.gorge = Gorge(auteur);
            elans.Add(elan);
            if (elan.reste <= 0f) Pas(0f);
        }

        private void FixedUpdate() => Pas(Time.fixedDeltaTime);

        /// <summary>Un pas, au pas physique (un test l'appelle hors jeu) : les élans avancent, les ondes partent.</summary>
        public void Pas(float dt)
        {
            for (int i = elans.Count - 1; i >= 0; i--)
            {
                var e = elans[i];
                e.reste -= dt;
                if (e.gorge != null && reglages.elan > 0f)
                    e.gorge.localScale = Vector3.one * Mathf.Lerp(0.15f, 0.55f, 1f - Mathf.Clamp01(e.reste / reglages.elan));
                if (e.reste > 0f) continue;
                elans.RemoveAt(i);
                if (e.gorge != null) Destroy(e.gorge.gameObject);
                if (e.auteur != null) Croasser(e.auteur, ciblesImposees ?? Cibles());
            }
        }

        /// <summary>
        /// L'onde part de « auteur » : chaque cible au sol et à portée est repoussée. Rend les cibles repoussées.
        /// Un test donne ses cibles ; en jeu, ce sont tous les joueurs de la scène (Cibles()).
        /// </summary>
        public List<Rigidbody> Croasser(GameObject auteur, IEnumerable<Rigidbody> cibles)
        {
            var repoussees = new List<Rigidbody>();
            if (auteur == null || reglages == null) return repoussees;
            corps.Clear();
            autres.Clear();
            foreach (var c in cibles)
            {
                if (c == null || c.gameObject == auteur || c.isKinematic) continue;
                var cookies = c.GetComponent<PorteurCookies>();
                if (cookies != null && cookies.Intouchable) continue;
                corps.Add(c);
                autres.Add((corps.Count, c.position, AuSol(c)));
            }
            pousses.Clear();
            Vector3 centre = auteur.transform.position;
            new ReglesSuperCroassement(reglages.rayon).Cibles(0, centre, auteur.transform.forward, autres, pousses);
            foreach (var p in pousses)
            {
                var c = corps[p.Joueur - 1];
                c.linearVelocity = p.Direction * reglages.poussee + Vector3.up * reglages.montee;
                repoussees.Add(c);
            }
            Onde?.Invoke(centre, reglages.rayon);
            if (visuelProvisoire) cercles.Add((Cercle(), centre, 0f));
            return repoussees;
        }

        /// <summary>Les joueurs de la scène : tout corps physique qui porte un contrôleur de Froggy, des cookies ou un objet.</summary>
        public static List<Rigidbody> Cibles()
        {
            var liste = new List<Rigidbody>();
            foreach (var c in FindObjectsByType<Rigidbody>())
                if (c.GetComponent<DeplacementWow>() != null || c.GetComponent<PorteurCookies>() != null || c.GetComponent<PorteurObjet>() != null)
                    liste.Add(c);
            return liste;
        }

        // le contrôleur sait s'il touche le sol ; sinon (un mannequin), un rayon sous ses pieds
        private bool AuSol(Rigidbody c)
        {
            var deplacement = c.GetComponent<DeplacementWow>();
            if (deplacement != null && deplacement.isActiveAndEnabled) return deplacement.AuSol;
            var forme = c.GetComponent<Collider>();
            if (forme == null) return true;
            Bounds b = forme.bounds;
            float longueur = b.extents.y + 0.15f;
            int n = c.gameObject.scene.GetPhysicsScene().Raycast(b.center, Vector3.down, touches, longueur, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
                if (touches[i].collider != forme && !touches[i].transform.IsChildOf(c.transform)) return true;
            return false;
        }

        private void Update()
        {
            for (int i = cercles.Count - 1; i >= 0; i--)
            {
                var (cercle, centre, age) = cercles[i];
                age += Time.deltaTime;
                if (cercle == null || age >= DureeCercle)
                {
                    if (cercle != null) Destroy(cercle.gameObject);
                    cercles.RemoveAt(i);
                    continue;
                }
                cercles[i] = (cercle, centre, age);
                Tracer(cercle, centre, reglages.rayon * Mathf.Sqrt(age / DureeCercle));
            }
        }

        private Transform Gorge(GameObject auteur)
        {
            var boule = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            DestroyImmediate(boule.GetComponent<Collider>());   // tout de suite : elle ne doit jamais pousser personne
            boule.name = "Gorge (provisoire)";
            boule.transform.SetParent(auteur.transform, false);
            boule.transform.localPosition = new Vector3(0f, 0.1f, 0.45f);
            boule.transform.localScale = Vector3.one * 0.15f;
            var rendu = boule.GetComponent<Renderer>();
            rendu.material.color = reglages.fiche != null ? reglages.fiche.couleur : new Color(1f, .8f, .3f);
            return boule.transform;
        }

        private LineRenderer Cercle()
        {
            var go = new GameObject("Onde (provisoire)");
            var ligne = go.AddComponent<LineRenderer>();
            ligne.loop = true;
            ligne.useWorldSpace = true;
            ligne.positionCount = 48;
            ligne.widthMultiplier = 0.15f;
            if (matiere == null)
            {
                var shader = Shader.Find("Sprites/Default");
                matiere = shader != null ? new Material(shader) : null;
            }
            if (matiere != null) ligne.sharedMaterial = matiere;
            Color c = reglages.fiche != null ? reglages.fiche.couleur : Color.white;
            ligne.startColor = ligne.endColor = c;
            return ligne;
        }

        private static void Tracer(LineRenderer ligne, Vector3 centre, float rayon)
        {
            for (int i = 0; i < ligne.positionCount; i++)
            {
                float a = i * Mathf.PI * 2f / ligne.positionCount;
                ligne.SetPosition(i, centre + new Vector3(Mathf.Cos(a) * rayon, -0.4f, Mathf.Sin(a) * rayon));
            }
        }
    }
}
