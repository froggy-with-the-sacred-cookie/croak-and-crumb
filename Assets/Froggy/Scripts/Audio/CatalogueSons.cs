using System;
using UnityEngine;

namespace Froggy.Audio
{
    public enum IdSon
    {
        PasHerbe, PasGravier, PasPierre, PasBois, PasNenuphar,
        Saut, Atterrissage, Plouf, Nage,
        TorgnoleElan, TorgnoleImpact, CookieRamasse, CookieLache,
        BoiteMystereOuverture, GelEclair, SuperCroassement, CamouflageCameleon,
        GobelinArrivee, CookieSacreRamasse
    }

    [CreateAssetMenu(menuName = "Froggy/Audio/Catalogue de sons")]
    public sealed class CatalogueSons : ScriptableObject
    {
        [Serializable]
        public sealed class Evenement
        {
            public IdSon id;
            public AudioClip[] variantes = Array.Empty<AudioClip>();
            [Range(0f, 1f)] public float volume = 1f;
            [Range(0f, .12f)] public float variationHauteur = .035f;
            [Range(0f, 1f)] public float spatialisation = 1f;
            [Range(0, 256)] public int priorite = 128;
            [Min(0f)] public float intervalleMinimal;
            [Range(1, 8)] public int maxVoix = 3;
        }

        [SerializeField] private Evenement[] evenements = Array.Empty<Evenement>();

        public System.Collections.Generic.IReadOnlyList<Evenement> Evenements => evenements;
    }
}
