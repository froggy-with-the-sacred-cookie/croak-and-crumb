using System;
using System.Collections.Generic;
using Froggy.Meteo;
using Froggy.Socle;
using UnityEngine;
using UnityEngine.Rendering;

namespace Froggy.Objets
{
    /// <summary>
    /// L'effet du camouflage caméléon (P4-07), dans une scène, sans réseau. Quand une Froggy utilise l'objet (événement
    /// « objet utilisé » du socle), elle devient presque invisible pendant « duree » secondes : immobile, on ne la voit
    /// plus à 10 m ; en mouvement, elle chatoie un peu (plus elle court, plus on la devine). Elle disparaît et réapparaît
    /// en fondu. L'onde du super croassement (EffetSuperCroassement.Onde) et le début de la pluie la révèlent : le
    /// camouflage s'arrête (choix de Lucas, 18 novembre 2026) ; pendant la pluie, il ne prend pas. Ses ombres sont
    /// coupées. Un rendu dont le nom contient « Cookie » reste visible (un cookie porté se voit, Notion).
    /// Seul le dessin change : aucune règle ne lit l'opacité. Les bots voient encore un camouflé (à faire côté bots).
    /// En réseau, l'état des objets n'est pas encore copié chez les autres joueurs : à faire avec le poste 2.
    /// </summary>
    public class EffetCamouflage : MonoBehaviour
    {
        [SerializeField] private ReglagesCamouflage reglages;

        private class Habit
        {
            public GameObject joueur;
            public readonly List<(Renderer rendu, Material[] avant, ShadowCastingMode ombre)> rendus = new List<(Renderer, Material[], ShadowCastingMode)>();
            public readonly List<Material> transparents = new List<Material>();
            public float age, ageSortie, opacite = 1f, depuis, phase;
            public bool sortie;
        }

        private readonly Dictionary<GameObject, int> numeros = new Dictionary<GameObject, int>();
        private readonly Dictionary<int, Habit> habits = new Dictionary<int, Habit>();
        private readonly List<int> finis = new List<int>();
        private readonly List<int> aRetirer = new List<int>();
        private readonly List<EffetSuperCroassement> croassements = new List<EffetSuperCroassement>();
        private readonly List<GestionnaireMeteo> meteos = new List<GestionnaireMeteo>();
        private Shader lit;
        private bool pluie;

        public ReglesCamouflage Regles { get; } = new ReglesCamouflage();

        /// <summary>Un joueur vient de se camoufler, ou d'être révélé (fin ou révélation) : sons, HUD, bots.</summary>
        public event Action<GameObject> Camoufle, Revele;

        /// <summary>Vrai pendant la pluie (ou un autre événement qui révèle) : le camouflage ne prend pas.</summary>
        public bool Pluie => pluie;

        /// <summary>Pour un test : des réglages à soi.</summary>
        public void Regler(ReglagesCamouflage r) => reglages = r;

        /// <summary>Ce joueur est-il camouflé (fondus compris, sauf celui de sortie) ?</summary>
        public bool EstCamoufle(GameObject joueur) => joueur != null && Regles.EstCamoufle(Numero(joueur));

        /// <summary>L'opacité dessinée de ce joueur (1 s'il n'est pas camouflé).</summary>
        public float Opacite(GameObject joueur) =>
            joueur != null && habits.TryGetValue(Numero(joueur), out var h) ? h.opacite : 1f;

        private void OnEnable()
        {
            EvenementsPartie.Emis += Reagir;
            Ecouter();
        }

        private void Start() => Ecouter();   // le décor et les autres effets arrivent parfois après nous

        private void OnDisable()
        {
            EvenementsPartie.Emis -= Reagir;
            foreach (var c in croassements) if (c != null) c.Onde -= Onde;
            croassements.Clear();
            foreach (var m in meteos) if (m != null) { m.Debut -= DebutMeteo; m.Fin -= FinMeteo; }
            meteos.Clear();
            foreach (var h in habits.Values) Restaurer(h);
            habits.Clear();
            Regles.Vider();
        }

        private void Ecouter()
        {
            foreach (var c in FindObjectsByType<EffetSuperCroassement>())
                if (!croassements.Contains(c)) { croassements.Add(c); c.Onde += Onde; }
            foreach (var m in FindObjectsByType<GestionnaireMeteo>())
                if (!meteos.Contains(m)) { meteos.Add(m); m.Debut += DebutMeteo; m.Fin += FinMeteo; }
        }

        /// <summary>Écoute les événements du socle (un test l'appelle directement : hors jeu, OnEnable ne tourne pas).</summary>
        public void Reagir(EvenementPartie e)
        {
            if (e.Type != TypeEvenement.ObjetUtilise || e.Auteur == null || reglages == null || reglages.fiche == null) return;
            var porteur = e.Auteur.GetComponent<PorteurObjet>();
            if (porteur != null && porteur.DernierUtilise == reglages.fiche) Camoufler(e.Auteur);
        }

        /// <summary>Camoufle ce joueur. Rend false pendant la pluie, ou s'il l'est déjà.</summary>
        public bool Camoufler(GameObject joueur)
        {
            if (joueur == null || reglages == null) return false;
            int n = Numero(joueur);
            if (habits.ContainsKey(n)) return false;   // encore dans son fondu de sortie
            if (!Regles.Camoufler(n, reglages.duree, pluie)) return false;
            var h = new Habit { joueur = joueur, phase = (n * 0.618034f) % 1f };   // jamais en phase avec un autre
            Habiller(h);
            habits[n] = h;
            Camoufle?.Invoke(joueur);
            return true;
        }

        /// <summary>Révèle ce joueur : le camouflage s'arrête, il réapparaît en fondu. Rend false s'il n'était pas camouflé.</summary>
        public bool Reveler(GameObject joueur)
        {
            if (joueur == null) return false;
            int n = Numero(joueur);
            if (!Regles.Reveler(n)) return false;
            Sortir(n);
            return true;
        }

        /// <summary>L'onde d'un croassement : les camouflés qu'elle touche sont révélés.</summary>
        public void Onde(Vector3 centre, float rayon)
        {
            foreach (var h in new List<Habit>(habits.Values))
            {
                if (h.joueur == null || h.sortie) continue;
                Vector3 d = h.joueur.transform.position - centre;
                d.y = 0f;
                if (d.magnitude <= rayon + 0.5f) Reveler(h.joueur);
            }
        }

        /// <summary>Un événement météo commence : s'il révèle (la pluie), tous les camouflés réapparaissent.</summary>
        public void DebutMeteo(DefinitionEvenement e)
        {
            if (reglages == null || e == null || !reglages.revelePar.Contains(e)) return;
            pluie = true;
            foreach (var h in new List<Habit>(habits.Values)) if (!h.sortie) Reveler(h.joueur);
        }

        public void FinMeteo(DefinitionEvenement e)
        {
            if (reglages != null && e != null && reglages.revelePar.Contains(e)) pluie = false;
        }

        private void FixedUpdate() => Pas(Time.fixedDeltaTime);

        /// <summary>Un pas, au pas physique (un test l'appelle hors jeu) : les camouflages s'usent.</summary>
        public void Pas(float dt)
        {
            Regles.Pas(dt, finis);
            foreach (int n in finis) Sortir(n);
        }

        private void Update() => Dessiner(Time.deltaTime, Time.time);

        /// <summary>Le dessin, à chaque image (un test l'appelle hors jeu) : fondus, opacité, chatoiement.</summary>
        public void Dessiner(float dt, float temps)
        {
            if (reglages == null) return;
            aRetirer.Clear();
            foreach (var kv in habits)
            {
                var h = kv.Value;
                float voulue = 1f;
                if (h.joueur != null)
                {
                    var corps = h.joueur.GetComponent<Rigidbody>();
                    float vitesse = corps != null ? new Vector2(corps.linearVelocity.x, corps.linearVelocity.z).magnitude : 0f;
                    voulue = ReglesCamouflage.Opacite(vitesse, temps, h.phase, reglages);
                }
                if (!h.sortie)
                {
                    h.age += dt;
                    h.opacite = Mathf.Lerp(1f, voulue, ReglesCamouflage.Fondu(h.age, reglages.fondu));
                }
                else
                {
                    h.ageSortie += dt;
                    float f = ReglesCamouflage.Fondu(h.ageSortie, reglages.fondu);
                    h.opacite = Mathf.Lerp(h.depuis, 1f, f);
                    if (f >= 1f) aRetirer.Add(kv.Key);
                }
                foreach (var m in h.transparents)
                {
                    if (m == null) continue;
                    Color c = m.GetColor("_BaseColor");
                    c.a = h.opacite;
                    m.SetColor("_BaseColor", c);
                }
            }
            foreach (int n in aRetirer)
            {
                var h = habits[n];
                habits.Remove(n);
                Restaurer(h);
                Revele?.Invoke(h.joueur);
            }
        }

        private void Sortir(int n)
        {
            if (!habits.TryGetValue(n, out var h) || h.sortie) return;
            h.sortie = true;
            h.depuis = h.opacite;
            h.ageSortie = 0f;
        }

        private int Numero(GameObject joueur)
        {
            if (!numeros.TryGetValue(joueur, out int n)) numeros[joueur] = n = numeros.Count + 1;
            return n;
        }

        // chaque rendu du joueur passe en transparent ; un cookie porté et les effets (particules, traits) restent tels quels
        private void Habiller(Habit h)
        {
            foreach (var r in h.joueur.GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer || PorteUnCookie(r.transform, h.joueur.transform)) continue;
                var avant = r.sharedMaterials;
                var apres = new Material[avant.Length];
                for (int i = 0; i < avant.Length; i++) h.transparents.Add(apres[i] = Transparent(avant[i]));
                h.rendus.Add((r, avant, r.shadowCastingMode));
                r.sharedMaterials = apres;
                r.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        private static void Restaurer(Habit h)
        {
            foreach (var (rendu, avant, ombre) in h.rendus)
            {
                if (rendu == null) continue;
                rendu.sharedMaterials = avant;
                rendu.shadowCastingMode = ombre;
            }
            foreach (var m in h.transparents) if (m != null) DestroyImmediate(m);
            h.rendus.Clear();
            h.transparents.Clear();
        }

        private static bool PorteUnCookie(Transform t, Transform racine)
        {
            for (; t != null && t != racine; t = t.parent)
                if (t.name.IndexOf("Cookie", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        // une copie transparente du matériau (texture et couleur gardées), à la façon d'URP
        private Material Transparent(Material avant)
        {
            if (lit == null) lit = Shader.Find("Universal Render Pipeline/Lit");
            var m = new Material(lit) { name = (avant != null ? avant.name : "Froggy") + " (camouflage)" };
            if (avant != null)
            {
                Texture tex = avant.HasProperty("_BaseMap") ? avant.GetTexture("_BaseMap") : avant.HasProperty("_MainTex") ? avant.GetTexture("_MainTex") : null;
                if (tex != null) m.SetTexture("_BaseMap", tex);
                Color c = avant.HasProperty("_BaseColor") ? avant.GetColor("_BaseColor") : avant.HasProperty("_Color") ? avant.GetColor("_Color") : Color.white;
                m.SetColor("_BaseColor", c);
            }
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            return m;
        }
    }
}
