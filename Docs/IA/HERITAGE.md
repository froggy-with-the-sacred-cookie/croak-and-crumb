# D'où viennent les skills de Froggy

Florent a monté un workflow à plusieurs IA (Claude Code, Codex / GPT, Gemini, Meshy, Blender, Sound Forge, Pixel Forge)
sur trois prototypes, en septembre-octobre 2026. Les skills de `.claude/skills/` en reprennent les leçons, **réécrites
pour Froggy** (Unity 3D, multijoueur, FishNet, arènes) : les originaux pointaient vers des fichiers de ces autres jeux.

## Les sources

| Projet | Où | Moteur | Ce qui en vient |
|---|---|---|---|
| **Eku** | `~/Dev/Eku` (local) | Godot, 2D, metroidvania roguelite | 16 skills (`.claude/skills/`, copie Codex dans `.agents/skills/`), skill audio du GDD, notes mémoire de Claude |
| **Lovecraft** | github.com/zharrow/lovecraft, github.com/zharrow/lovecraft-atelier | Unity 6, 3D low-poly | skill `modele-3d-icone`, `CLAUDE.md` (313 lignes de pratiques Unity), Tech Art Bible, ~45 notes mémoire |
| **Glouton** | `~/Dev/game` (local) | Unity 6, 3D, multijoueur 2-6 | aucun skill : `README.md`, `DESIGN.md`, outils `tools/`, notes mémoire de Claude |

## Correspondance

| Skill de Froggy | Repris de |
|---|---|
| `session-froggy` | Eku `session-eku` ; règles git et mode test de Lovecraft ; Glouton (originalité, Codex qui livre à part) |
| `unity-mcp` | Lovecraft `CLAUDE.md` (« Tester dans Unity », « Pièges ») et mémoires `unity-mcp-setup`, `lovecraft-recompilation-en-jeu` ; build par copie de Glouton |
| `multijoueur-equilibrage` | Glouton `DESIGN.md` (en ligne, équilibrage, rétention, performance, objets qui tombaient) |
| `ressenti-mouvement` | Eku `ressenti-mouvement` |
| `camera` | Eku `camera-eku` ; caméra troisième personne de Lovecraft |
| `impact-feedback` | Eku `combat-impact` ; « juice » de Glouton |
| `rendu-vivant` | Eku `rendu-vivant` |
| `accessibilite` | Eku `accessibilite-eku` |
| `production-assets` | Eku `production-assets` ; mémoires Lovecraft (validation Meshy, gratuit d'abord, fidélité, comparer avant/après, assets riches) et Glouton (crédits Meshy, direction des créatures) |
| `route-3d-meshy` | Eku `route-3d` ; « La 3D par Meshy » de Lovecraft ; outils de Glouton |
| `modele-3d-blender` | Lovecraft `modele-3d-icone` (et ses `references/`) |
| `direction-artistique` | Eku `direction-artistique` ; mémoires Eku sur les goûts de Florent |
| `animation-gpt` | Eku `animation-gpt` ; livraisons de Codex sur Glouton |
| `son-integration` | Eku `son-eku` ; pas et sons de Lovecraft ; musique de Glouton |
| `composition-audio` | Eku `composition-eku` et `eku-game-audio-director` (GDD) |
| `recherche-reference` | Eku `recherche-environnement` ; mémoire `feedback-recherche-avant-prompt` |
| `tech-art` | Tech Art Bible (`references/tech_art_bible.md`) ; rendu de Lovecraft (météo, traces, eau, style, LOD) ; performance de Glouton |

## Outils repris dans `Tools/`

| Outil | Origine | Rôle |
|---|---|---|
| `Tools/meshy/trim_base.py` | Glouton | retire le socle que Meshy ajoute parfois sous un modèle |
| `Tools/meshy/unpack_glb.py` | Glouton | GLB vers glTF avec textures externes réduites (build de 399 à 99 Mo) |
| `Tools/blender/blender_lib.py` | Lovecraft | briques Blender des objets taillés et peints |
| `Tools/unity_mcp.py` | Lovecraft | client HTTP de secours du MCP de Unity |
| `Tools/art/carte_palette.py`, `Tools/art/juger_vision.py` | Eku | carte de palette à joindre à Gemini, mesure des tirages |
| `Tools/sync_skills.py` | nouveau | recopie `.claude/skills/` vers `.agents/skills/` pour Codex |

## Ce qui n'a pas été repris, et pourquoi

- **Eku `salles-expedition`** (assemblage de salles façon Dead Cells, boucle roguelite) et **`monde-et-recit`** (récit
  réactif, faune à la Rain World) : propres à un metroidvania solo narratif. À relire si Froggy gagne un mode histoire
  ou de la faune autonome dans les arènes.
- **Eku `capture-en-jeu`** : outil Godot ; sa méthode Unity est dans `unity-mcp`.
- **Les outils de la porte d'Eku** (`porte.py`, `commande.py`, `bible.py`, `pied_fixe.py`) : écrits pour des planches de
  sprites 2D et leur contrat JSON. Décrits dans `animation-gpt`, à recréer si Froggy produit beaucoup de flipbooks.
- **Les outils de Lovecraft liés à son code** (`unity_asset.py`, `test_recolte.py`, `fiches.py`, `refs_reelles.py`,
  `reconstruire_rig.py`, `integrer_objet.py`, `hiver_meshy.py`) : ils supposent son catalogue et ses chemins. À
  adapter au besoin depuis le dépôt `zharrow/lovecraft` (`Outils/`).
- **Les 200 fiches de la Tech Art Bible** : gardées dans le fichier, mais à chercher, pas à lire (voir `tech-art`).
- **Les états et décisions de chaque jeu** (personnages, biomes, quêtes, réglages chiffrés de leurs contrôleurs) : ils
  ne valent que pour leur jeu. Les chiffres cités dans les skills sont des points de départ, marqués comme tels.

## Outils qui ne sont pas encore partagés avec l'équipe

Sound Forge et Pixel Forge (outils locaux de Florent), l'abonnement Gemini Pro et les crédits Meshy de Florent, le
serveur MCP de Blender. Un skill qui s'en sert le dit ; sur une autre machine, garder la méthode et changer d'outil.
