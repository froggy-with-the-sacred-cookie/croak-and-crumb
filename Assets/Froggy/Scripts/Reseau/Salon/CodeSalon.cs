using System;
using System.Text;

namespace Froggy.Reseau
{
    /// <summary>
    /// Code d'un salon (P2-03) : 4 lettres faciles à dicter et à taper, par exemple « KRBF ».
    /// Pas de O ni de I (confondus avec 0 et 1), pas de chiffres. 24 lettres, 4 places : 331 776 codes.
    /// </summary>
    public static class CodeSalon
    {
        /// <summary>PROVISOIRE : longueur du code.</summary>
        public const int Longueur = 4;

        /// <summary>Lettres permises.</summary>
        public const string Lettres = "ABCDEFGHJKLMNPQRSTUVWXYZ";

        /// <summary>Tire un code au hasard.</summary>
        public static string Generer(Random hasard)
        {
            if (hasard == null) throw new ArgumentNullException(nameof(hasard));
            var code = new StringBuilder(Longueur);
            for (int i = 0; i < Longueur; i++)
                code.Append(Lettres[hasard.Next(Lettres.Length)]);
            return code.ToString();
        }

        /// <summary>Ce que le joueur a tapé, en majuscules, sans espaces ni tirets (« kr-bf » → « KRBF »).</summary>
        public static string Normaliser(string saisie)
        {
            if (string.IsNullOrEmpty(saisie)) return "";
            var code = new StringBuilder(saisie.Length);
            foreach (char c in saisie)
            {
                if (char.IsWhiteSpace(c) || c == '-') continue;
                code.Append(char.ToUpperInvariant(c));
            }
            return code.ToString();
        }

        /// <summary>Vrai si le code (déjà normalisé) a la bonne longueur et seulement des lettres permises.</summary>
        public static bool EstValide(string code)
        {
            if (code == null || code.Length != Longueur) return false;
            foreach (char c in code)
                if (Lettres.IndexOf(c) < 0) return false;
            return true;
        }
    }
}
