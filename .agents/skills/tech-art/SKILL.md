---
name: tech-art
description: Tech art de Froggy (météo et pluie qui changent le jeu, vent, sol mouillé, flaques, eau des lacs et marécages, traces de pas, rendu stylisé URP, niveaux de détail, performance du rendu), avec la « Tech Art Bible » de Florent en référence. À charger AVANT d'écrire ou de modifier un shader, la météo, l'eau, le vent, la végétation qui bouge, un post-traitement, les LOD, ou de chercher pourquoi le rendu est lent. Aussi quand quelqu'un parle de « faire plus beau », de « style », de « pluie », d'« eau », de « perf » ou de « FPS ».
---

# Tech art

La référence générique est `references/tech_art_bible.md` (Tech Art Bible de Florent, v3, 14 400 lignes) : **ne pas la
lire en entier**. Les parties utiles, par ligne :

| Lignes | Contenu |
|---|---|
| 128-2140 | 16 volumes en listes et recettes : 0 fondamentaux, 1 eau (207), 2 végétation et vent (334), 3 terrain (462), 4 atmosphère et météo (590), 5 VFX (718), 6 animation procédurale et IK (854), 7 PCG (983), 8 éclairage et rendu stylisé (1115), 9 surfaces dynamiques (1243), 10 destruction (1371), 11 audio systémique (1499), 12 caméra et game feel (1633), 13 optimisation (1761), 14 architecture shader (1889), 15 outils et CI (2016) |
| 2141-3728 | Suppléments : mode d'emploi pour une IA (2353), index par intention (2442), contrats de données (2725), anti-patterns (2952), arbres de décision (3083), correspondance Godot / Unity / Unreal (3180), checklists de production (3295), questions à poser avant d'implémenter (3619), résumé ultra-court (3684) |
| 3729-5302 | Registre global des techniques |
| 5303-13772 | 200 fiches au gabarit répété : **ne pas les relire**, chercher (`grep`) une technique précise |
| 13773-fin | Stacks Minimal → Hero, comparatifs, arbres de décision, dépannage (13950), tables de ROI (13995), tests et invariants (14083) |

Commencer par le résumé ultra-court (l. 3684) et les questions à poser (l. 3619), puis le volume du sujet.

## 1. La météo : une cause, plusieurs conséquences (Lovecraft, 5 octobre 2026)

Dans Froggy, la pluie **change le jeu** (bonus de vitesse des grenouilles, Notion) : elle est un état de la partie,
décidé par l'autorité et identique pour tous (skill `multijoueur-equilibrage`). Son rendu suit la leçon de Lovecraft :

- **Une seule source** pour le vent et le sol mouillé à l'écran : un composant calcule chaque image direction et force
  du vent, pluie et « mouillé », et les pose en **variables globales de shader** (Lovecraft : `_LCWind` xy direction,
  z force, w horloge ; `_LCMouille`). Tous les lecteurs lisent ces globales : herbe et arbres, eau (rides, ronds de
  pluie), particules, nuages, son du vent. **Toute nouvelle chose qui bouge au vent lit cette source, jamais une
  direction en dur.**
- **L'horloge du vent** est la sienne (plus rapide par grand vent), jamais le temps global du shader pour les rafales.
- **Les flaques** : l'alpha des couleurs de sommets dit où l'eau stagne ; un réglage de matériau coupe le mouillé de ce
  qui ne doit pas se mouiller.
- **Leçon des essais** : sous un ciel couvert, refléter le ciel gris sur tout le mouillé grise l'image au lieu de la
  foncer. Ce qui dit « eau », c'est le contraste sol foncé / flaque au bord net qui reflète le ciel. Juger sur une
  planche à plusieurs moments (soleil après la pluie, pluie, nuit).
- Une fonction saute les fondus de météo pour les captures.

## 2. Traces de pas (Lovecraft)

Un seul maillage refait à chaque image pour toutes les empreintes, un shader qui **multiplie** le sol
(`Blend DstColor Zero`), et **l'alpha dit de combien le creux fonce au soleil** (sinon la neige ou le sable clairs, en
HDR, ne montrent rien). Les empreintes et les sons de pas partent des appuis réels de l'animation (skill
`son-integration`). Pour la boue et le sable des arènes de Froggy.

## 3. Eau stylisée (Lovecraft)

Couleur par profondeur, motif de Voronoï déformé et d'épaisseur variable, lignes de rive sur la profondeur **lissée sur
plusieurs points d'écran** (sinon elles suivent les marches du terrain), lueur la nuit, luminosité qui suit le soleil.
Tout effet nouveau est **à zéro par défaut** : sans le réglage, le rendu d'avant (pour pouvoir comparer).

## 4. Style du monde et bascule

Un réglage de style (ScriptableObject) pose les globales ; décocher « actif » rend le rendu **et les modèles** d'avant
(un catalogue d'avant gardé). L'équipe fait des allers-retours avant de choisir (skill `production-assets`, section 4).
Une ambiance d'essai se fait sur une **copie de la scène**, hors build, sans y éditer le monde.

## 5. Niveaux de détail (Lovecraft, Unity 6)

- Le **Mesh LOD de Unity 6** : un maillage porte ses niveaux ; chaque rendu choisit seul. Règle mesurée :
  `niveau = biais − pente × log2(diagonale des bornes à l'écran, en pixels absolus)`.
- Unity simplifie mal les feuillages (le chêne devenait chauve) : une partie qui perdrait plus de 45 % de sa surface
  devient des touffes (icosaèdres aux normales presque sphériques).
- Un maillage déjà rangé se remplit sur place : `EditorUtility.CopySerialized` laissait l'ancien sur la carte graphique.
- Les dessins en instances ne choisissent pas de niveau seuls.
- Mesures : caméra proche −9 % seulement ; vue de haut −63 à −72 %. Forcer un niveau pour une capture :
  `MeshRenderer.forceMeshLod`.

## 6. Performance (Glouton)

Forward+, SRP Batcher et GPU Resident Drawer actifs ; distance des ombres qui suit la caméra ; post-traitement coupé
quand il ne se voit pas ; textures externes réduites et compressées (skill `route-3d-meshy`). Mesurer l'image médiane
**et** le 99e centile, à 1 vue et au pire cas (4 joueurs), sur la machine la plus faible de l'équipe.

## 7. Pièges de rendu

- La profondeur de champ d'URP est fausse en caméra orthographique.
- Une couleur d'émission dans Blender se donne en **linéaire** (un indigo sRGB passé tel quel sort lavande).
- Un build peut retirer le post-traitement (`m_StripUnusedPostProcessingVariants`) : contrôler un build.
- Écartés sur Lovecraft comme trop coûteux pour une petite équipe : destruction, océan FFT, volumétriques, GI, motion
  matching. À rouvrir seulement avec une raison.
