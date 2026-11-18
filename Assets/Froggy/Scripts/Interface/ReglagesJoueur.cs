using System;
using System.IO;
using UnityEngine;

namespace Froggy.Interface
{
    /// <summary>
    /// Les réglages du joueur (début de P6-05) : volumes par famille et intensité des effets forts. Chaque valeur va de
    /// 0 à 1 ; 0 coupe, 1 joue tel que réglé (skill accessibilite). Enregistrés à part, dans
    /// <c>reglages_joueur.json</c> du dossier de données du jeu, jamais dans une sauvegarde de partie.
    ///
    /// Pour un poste qui joue un effet ou un son : lire <c>ReglagesJoueur.Actuels.secousses</c> (par exemple) au moment
    /// où l'effet part, et écouter <c>ReglagesJoueur.Change</c> pour un son qui dure. Les tests passent un chemin de test
    /// à Lire / Ecrire et ne touchent jamais au vrai fichier. Testé dans MenusTests.
    /// </summary>
    [Serializable]
    public sealed class ReglagesJoueur
    {
        public const string NomFichier = "reglages_joueur.json";

        [Range(0f, 1f)] public float volumeGeneral = 1f;
        [Range(0f, 1f)] public float volumeMusique = 1f;
        [Range(0f, 1f)] public float volumeEffets = 1f;
        [Range(0f, 1f)] public float volumeAmbiance = 1f;
        [Range(0f, 1f)] public float secousses = 1f;
        [Range(0f, 1f)] public float flashs = 1f;
        [Range(0f, 1f)] public float vibrations = 1f;

        private static ReglagesJoueur actuels;

        /// <summary>Un réglage a changé (menu Paramètres) : les sons qui durent relisent leur volume.</summary>
        public static event Action Change;

        /// <summary>Les réglages de ce joueur, lus une fois dans le vrai fichier.</summary>
        public static ReglagesJoueur Actuels => actuels ??= Lire(CheminReel);

        /// <summary>Le vrai fichier du joueur. Les tests ne s'en servent pas.</summary>
        public static string CheminReel => Path.Combine(Application.persistentDataPath, NomFichier);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reinitialiser()
        {
            actuels = null;
            Change = null;
        }

        /// <summary>Ramène chaque valeur entre 0 et 1 (une valeur illisible redevient 1).</summary>
        public void Borner()
        {
            volumeGeneral = Borne(volumeGeneral);
            volumeMusique = Borne(volumeMusique);
            volumeEffets = Borne(volumeEffets);
            volumeAmbiance = Borne(volumeAmbiance);
            secousses = Borne(secousses);
            flashs = Borne(flashs);
            vibrations = Borne(vibrations);
        }

        private static float Borne(float v) => float.IsNaN(v) || float.IsInfinity(v) ? 1f : Mathf.Clamp01(v);

        /// <summary>Lit un fichier de réglages ; absent ou abîmé : les réglages par défaut (tout à 1).</summary>
        public static ReglagesJoueur Lire(string chemin)
        {
            var reglages = new ReglagesJoueur();
            try
            {
                if (File.Exists(chemin)) JsonUtility.FromJsonOverwrite(File.ReadAllText(chemin), reglages);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            {
                Debug.LogWarning($"[Réglages] Fichier illisible ({e.Message}) : réglages par défaut.");
                reglages = new ReglagesJoueur();
            }
            reglages.Borner();
            return reglages;
        }

        /// <summary>Écrit ces réglages (bornés) dans un fichier.</summary>
        public void Ecrire(string chemin)
        {
            Borner();
            string dossier = Path.GetDirectoryName(chemin);
            if (!string.IsNullOrEmpty(dossier)) Directory.CreateDirectory(dossier);
            File.WriteAllText(chemin, JsonUtility.ToJson(this, true));
        }

        /// <summary>Le menu a changé une valeur des réglages actuels : la prévenir à ceux qui écoutent.</summary>
        public static void Prevenir()
        {
            Actuels.Borner();
            Change?.Invoke();
        }

        /// <summary>Enregistre les réglages actuels dans le vrai fichier du joueur (en quittant Paramètres).</summary>
        public static void Enregistrer()
        {
            try
            {
                Actuels.Ecrire(CheminReel);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[Réglages] Impossible d'enregistrer ({e.Message}).");
            }
        }
    }
}
