using Froggy.Reseau;

namespace Froggy.Interface
{
    /// <summary>
    /// Le code de salon en cours de saisie (P6-03), en C# pur : 4 cases, une case active. Au clavier on tape les lettres
    /// à la suite (Retour arrière efface) ; à la manette, haut et bas font défiler la lettre de la case active, gauche et
    /// droite changent de case. Seules les lettres de CodeSalon entrent (ni O ni I). Testé dans MenusTests.
    /// </summary>
    public sealed class CodeEnSaisie
    {
        public const char Vide = '\0';

        private readonly char[] lettres = new char[CodeSalon.Longueur];

        /// <summary>La case active, de 0 à 3.</summary>
        public int Curseur { get; private set; }

        public int Longueur => lettres.Length;

        public char this[int i] => lettres[i];

        /// <summary>Vrai quand les 4 cases sont remplies : on peut rejoindre.</summary>
        public bool Complet => CodeSalon.EstValide(Code);

        /// <summary>Les lettres tapées, sans les cases vides (« KR » pendant la saisie).</summary>
        public string Code
        {
            get
            {
                var texte = new System.Text.StringBuilder(lettres.Length);
                foreach (char c in lettres)
                    if (c != Vide) texte.Append(c);
                return texte.ToString();
            }
        }

        /// <summary>Tape une lettre dans la case active, puis passe à la suivante. Faux si la lettre n'est pas permise.</summary>
        public bool Taper(char c)
        {
            c = char.ToUpperInvariant(c);
            if (CodeSalon.Lettres.IndexOf(c) < 0) return false;
            lettres[Curseur] = c;
            if (Curseur < lettres.Length - 1) Curseur++;
            return true;
        }

        /// <summary>Retour arrière : vide la case active si elle est remplie, sinon recule et vide la précédente.</summary>
        public void Effacer()
        {
            if (lettres[Curseur] != Vide)
            {
                lettres[Curseur] = Vide;
                return;
            }
            if (Curseur == 0) return;
            Curseur--;
            lettres[Curseur] = Vide;
        }

        /// <summary>Fait défiler la lettre de la case active (manette) : +1 vers Z, -1 vers A, en boucle.</summary>
        public void Defiler(int sens)
        {
            string permises = CodeSalon.Lettres;
            int i = permises.IndexOf(lettres[Curseur]);
            if (i < 0) i = sens >= 0 ? 0 : permises.Length - 1;
            else i = ((i + (sens >= 0 ? 1 : -1)) % permises.Length + permises.Length) % permises.Length;
            lettres[Curseur] = permises[i];
        }

        /// <summary>Change de case. Faux au bord : la navigation sort alors du champ.</summary>
        public bool Deplacer(int sens)
        {
            int cible = Curseur + (sens >= 0 ? 1 : -1);
            if (cible < 0 || cible >= lettres.Length) return false;
            Curseur = cible;
            return true;
        }

        /// <summary>Met le curseur sur une case (clic sur la case).</summary>
        public void Aller(int case_)
        {
            if (case_ >= 0 && case_ < lettres.Length) Curseur = case_;
        }

        /// <summary>Colle un code (« kr-bf », « KRBF ») : les lettres permises, dans l'ordre, depuis la première case.</summary>
        public void Coller(string texte)
        {
            Vider();
            foreach (char c in CodeSalon.Normaliser(texte))
            {
                if (lettres[lettres.Length - 1] != Vide) break;
                Taper(c);
            }
        }

        public void Vider()
        {
            for (int i = 0; i < lettres.Length; i++) lettres[i] = Vide;
            Curseur = 0;
        }
    }
}
