namespace Froggy.Interface
{
    /// <summary>
    /// Les textes de l'écran de chargement (maquette de Julien, page Notion P6-03), en C# pur : le nom de la carte et
    /// une astuce. Les astuces ne citent aucune touche : une touche affichée se lit sur l'action de l'Input System
    /// (skill accessibilite), pas dans une phrase écrite en dur.
    /// </summary>
    public static class ContenuChargement
    {
        public const string Titre = "Chargement en cours…";

        public static readonly string[] Astuces =
        {
            "Le gobelin arrive dans la dernière minute avec le cookie sacré : attrape-le !",
            "Qui porte le cookie sacré à la fin gagne la partie.",
            "Une torgnole fait lâcher le cookie sacré au gobelin.",
            "Sous la pluie, les Froggy courent plus vite.",
            "Les boîtes à objets se remplissent à nouveau : repasse les voir.",
            "Le disque doré montre toujours où est le cookie sacré, même hors de l'écran.",
        };

        /// <summary>Une astuce, choisie par un nombre quelconque (le même nombre donne la même astuce).</summary>
        public static string Astuce(int graine)
        {
            int i = graine % Astuces.Length;
            return "Astuce : " + Astuces[i < 0 ? i + Astuces.Length : i];
        }
    }
}
