using System;
using System.Collections.Generic;
using Froggy.Socle;
using UnityEngine;
using UnityEngine.Audio;

namespace Froggy.Audio
{
    /// <summary>Lecture locale des effets avec des voix spatialisées et une limite par événement.</summary>
    public sealed class SonsPartie : MonoBehaviour
    {
        [SerializeField] private CatalogueSons catalogue;
        [SerializeField] private AudioMixerGroup sortie;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        private const int MaxVoix = 24;

        private sealed class Etat
        {
            public CatalogueSons.Evenement Reglages;
            public AudioClip[] Clips;
            public VariantesSansRepetition Variantes;
            public double DerniereLecture = double.NegativeInfinity;
        }

        private sealed class Voix
        {
            public AudioSource Source;
            public IdSon Son;
            public double Debut;
            public double Fin;
        }

        private readonly Dictionary<IdSon, Etat> etats = new();
        private readonly List<Voix> voix = new();
        private readonly System.Random hasard = new();

        public static SonsPartie Actif { get; private set; }
        public float Volume { get => volume; set => volume = Mathf.Clamp01(value); }

        private void OnEnable()
        {
            if (Actif != null && Actif != this) return;
            Actif = this;
            etats.Clear();
            if (catalogue != null)
            {
                foreach (var evenement in catalogue.Evenements)
                {
                    if (evenement == null || evenement.variantes == null) continue;
                    var clips = Array.FindAll(evenement.variantes, clip => clip != null);
                    if (clips.Length == 0) continue;
                    etats.TryAdd(evenement.id, new Etat {
                        Reglages = evenement, Clips = clips,
                        Variantes = new VariantesSansRepetition(clips.Length, hasard)
                    });
                }
            }
            EvenementsPartie.Emis += Reagir;
        }

        private void OnDisable()
        {
            if (Actif != this) return;
            EvenementsPartie.Emis -= Reagir;
            foreach (var v in voix)
            {
                v.Source.Stop();
                v.Fin = double.NegativeInfinity;
            }
            Actif = null;
        }

        private void Reagir(EvenementPartie evenement)
        {
            switch (evenement.Type)
            {
                case TypeEvenement.CookieRamasse: Jouer(IdSon.CookieRamasse, evenement.Position); break;
                case TypeEvenement.CookieLache: Jouer(IdSon.CookieLache, evenement.Position); break;
                case TypeEvenement.ObjetUtilise:
                    if (evenement.Objet != null && evenement.Objet.TryGetComponent<SonObjet>(out var son))
                        Jouer(son.Utilisation, evenement.Position);
                    break;
            }
        }

        public bool Jouer(IdSon son, Vector3 position, float intensite = 1f)
        {
            if (volume <= 0f || intensite <= 0f || !etats.TryGetValue(son, out var etat)) return false;
            double maintenant = AudioSettings.dspTime;
            var reglage = etat.Reglages;
            if (maintenant - etat.DerniereLecture < reglage.intervalleMinimal) return false;
            var v = ChoisirVoix(son, reglage.maxVoix, reglage.priorite, maintenant);
            if (v == null) return false;

            var source = v.Source;
            source.Stop();
            source.transform.position = position;
            source.clip = etat.Clips[etat.Variantes.Suivante()];
            source.pitch = 1f + (float)(2d * hasard.NextDouble() - 1d) * reglage.variationHauteur;
            source.spatialBlend = reglage.spatialisation;
            source.priority = reglage.priorite;
            source.volume = volume * reglage.volume * Mathf.Clamp01(intensite);
            source.Play();
            v.Son = son;
            v.Debut = maintenant;
            v.Fin = maintenant + source.clip.length / source.pitch;
            etat.DerniereLecture = maintenant;
            return true;
        }

        private Voix ChoisirVoix(IdSon son, int maxPourLeSon, int priorite, double maintenant)
        {
            Voix libre = null, ancienneDuSon = null, remplacable = null;
            int nombre = 0;
            foreach (var v in voix)
            {
                if (v.Fin <= maintenant)
                {
                    libre ??= v;
                    continue;
                }
                if (v.Son == son)
                {
                    nombre++;
                    if (ancienneDuSon == null || v.Debut < ancienneDuSon.Debut) ancienneDuSon = v;
                }
                if (v.Source.priority >= priorite && (remplacable == null ||
                    v.Source.priority > remplacable.Source.priority ||
                    (v.Source.priority == remplacable.Source.priority && v.Debut < remplacable.Debut)))
                    remplacable = v;
            }
            if (nombre >= Mathf.Max(1, maxPourLeSon)) return ancienneDuSon;
            if (libre != null) return libre;
            if (voix.Count >= MaxVoix) return remplacable;

            var enfant = new GameObject("Voix " + (voix.Count + 1));
            enfant.transform.SetParent(transform, false);
            var source = enfant.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.dopplerLevel = 0f;
            source.minDistance = 2f;
            source.maxDistance = 22f;
            source.outputAudioMixerGroup = sortie;
            var nouvelle = new Voix { Source = source };
            voix.Add(nouvelle);
            return nouvelle;
        }
    }

    /// <summary>Joue toutes les variantes dans un ordre mélangé, en évitant la répétition entre deux tours.</summary>
    public sealed class VariantesSansRepetition
    {
        private readonly System.Random hasard;
        private readonly int[] ordre;
        private int prochain;
        private int precedent = -1;

        public VariantesSansRepetition(int nombre, System.Random hasard)
        {
            if (nombre < 1) throw new ArgumentOutOfRangeException(nameof(nombre));
            this.hasard = hasard ?? throw new ArgumentNullException(nameof(hasard));
            ordre = new int[nombre];
            prochain = nombre;
        }

        public int Suivante()
        {
            if (prochain == ordre.Length)
            {
                for (int i = 0; i < ordre.Length; i++) ordre[i] = i;
                for (int i = ordre.Length - 1; i > 0; i--)
                {
                    int j = hasard.Next(i + 1);
                    (ordre[i], ordre[j]) = (ordre[j], ordre[i]);
                }
                if (ordre.Length > 1 && ordre[0] == precedent)
                {
                    int j = hasard.Next(1, ordre.Length);
                    (ordre[0], ordre[j]) = (ordre[j], ordre[0]);
                }
                prochain = 0;
            }
            return precedent = ordre[prochain++];
        }
    }
}
