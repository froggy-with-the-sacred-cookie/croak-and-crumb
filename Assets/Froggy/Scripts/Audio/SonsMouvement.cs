using System;
using Froggy.Deplacement;
using Froggy.Personnages;
using UnityEngine;

namespace Froggy.Audio
{
    /// <summary>Lit les contacts du contrôleur et les appuis du modèle humain animé.</summary>
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(DeplacementWow), typeof(Rigidbody))]
    public sealed class SonsMouvement : MonoBehaviour
    {
        private const float IntervallePas = .20f;
        private const float DistanceParPas = .82f;

        private DeplacementWow mouvement;
        private Rigidbody corps;
        private FroggyApparence apparence;
        private Animator animateur;
        private Transform piedGauche, piedDroit;
        private Appui appuiGauche, appuiDroit;
        private float chute, distancePas, prochainPas;

        private struct Appui
        {
            private bool initialise, descend;
            private float hauteur;

            public bool Arrive(float nouvelleHauteur)
            {
                float ecart = nouvelleHauteur - hauteur;
                bool arrive = initialise && descend && ecart >= 0f;
                if (arrive || ecart > .0002f) descend = false;
                else if (initialise && ecart < -.0002f) descend = true;
                hauteur = nouvelleHauteur;
                initialise = true;
                return arrive;
            }
        }

        private void Awake()
        {
            mouvement = GetComponent<DeplacementWow>();
            corps = GetComponent<Rigidbody>();
            apparence = GetComponent<FroggyApparence>();
        }

        private void OnEnable()
        {
            mouvement.ImpulsionSaut += Sauter;
            mouvement.Atterrissage += Atterrir;
        }

        private void OnDisable()
        {
            mouvement.ImpulsionSaut -= Sauter;
            mouvement.Atterrissage -= Atterrir;
        }

        private void Sauter()
        {
            prochainPas = Time.time + IntervallePas;
            SonsPartie.Actif?.Jouer(IdSon.Saut, transform.position);
        }

        private void Atterrir(Collider sol)
        {
            if (chute < 1.35f) return;
            prochainPas = Time.time + IntervallePas;
            SonsPartie.Actif?.Jouer(IdSon.Atterrissage, transform.position,
                Mathf.Lerp(.36f, 1f, Mathf.InverseLerp(1.35f, 7f, chute)));
        }

        private void FixedUpdate()
        {
            if (!mouvement.isActiveAndEnabled || mouvement.AuSol) chute = 0f;
            else chute = Mathf.Max(chute, -corps.linearVelocity.y);
        }

        private void LateUpdate()
        {
            var modele = apparence != null ? apparence.Animateur : null;
            if (modele != animateur)
            {
                animateur = modele;
                piedGauche = animateur != null && animateur.isHuman ? animateur.GetBoneTransform(HumanBodyBones.LeftFoot) : null;
                piedDroit = animateur != null && animateur.isHuman ? animateur.GetBoneTransform(HumanBodyBones.RightFoot) : null;
                appuiGauche = appuiDroit = default;
            }

            var vitesse = corps.linearVelocity;
            float allure = new Vector2(vitesse.x, vitesse.z).magnitude;
            if (!mouvement.isActiveAndEnabled || !mouvement.AuSol || allure < .45f)
            {
                appuiGauche = appuiDroit = default;
                distancePas = 0f;
                return;
            }

            bool appui;
            Vector3 position = transform.position;
            if (piedGauche != null && piedDroit != null)
            {
                float gauche = piedGauche.position.y - transform.position.y;
                float droite = piedDroit.position.y - transform.position.y;
                bool contactGauche = appuiGauche.Arrive(gauche) && gauche <= droite + .013f;
                bool contactDroit = appuiDroit.Arrive(droite) && droite <= gauche + .013f;
                appui = contactGauche || contactDroit;
                if (appui) position = contactGauche ? piedGauche.position : piedDroit.position;
            }
            else
            {
                // Le cube de la vitrine n'a pas de pieds animés. La distance parcourue règle sa cadence.
                distancePas = Mathf.Min(DistanceParPas, distancePas + allure * Time.deltaTime);
                appui = distancePas >= DistanceParPas;
            }
            if (!appui || Time.time < prochainPas) return;
            distancePas = 0f;
            prochainPas = Time.time + IntervallePas;
            SonsPartie.Actif?.Jouer(SonDuSol(mouvement.SolTouche), position, Mathf.Clamp(allure / 5f, .55f, 1f));
        }

        private static IdSon SonDuSol(Collider sol)
        {
            for (Transform t = sol != null ? sol.transform : null; t != null; t = t.parent)
            {
                string nom = t.name;
                if (nom.StartsWith("Nenuphar_", StringComparison.OrdinalIgnoreCase)) return IdSon.PasNenuphar;
                if (nom.StartsWith("Gravier_", StringComparison.OrdinalIgnoreCase)) return IdSon.PasGravier;
                if (nom.StartsWith("Pont_Taiko", StringComparison.OrdinalIgnoreCase) ||
                    nom.StartsWith("Yatsuhashi_", StringComparison.OrdinalIgnoreCase)) return IdSon.PasBois;
                if (nom.StartsWith("Ishibashi_", StringComparison.OrdinalIgnoreCase) ||
                    nom.StartsWith("PierreGue_", StringComparison.OrdinalIgnoreCase) ||
                    nom.StartsWith("Rocher_", StringComparison.OrdinalIgnoreCase) ||
                    nom.StartsWith("Galets", StringComparison.OrdinalIgnoreCase)) return IdSon.PasPierre;
            }
            return IdSon.PasHerbe;
        }
    }
}
