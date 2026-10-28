---
name: direction-artistique
description: Explorer et choisir la direction artistique de Froggy et de chaque arène par des illustrations complètes (visions), avant de produire les assets — méthode en trois messages (valeurs, couleur, finition), principes de peinture (valeur, gamme 60/30/10, lumière, hiérarchie du détail), anatomie d'un prompt Gemini, plan de masse au lieu d'une capture, jugement mesuré des tirages, goûts déjà connus. À charger AVANT d'écrire un prompt d'illustration d'arène, de concept art, de « vision », de key art ou d'image-maîtresse, de juger des tirages Gemini, ou de parler de style, de palette ou d'ambiance. Aussi quand quelqu'un dit qu'une image « fait IA », « manque de luminosité », est « monotone », « fade », ou demande « différentes visions ». Charger aussi production-assets.
---

# Direction artistique : les visions

Méthode d'Eku (19-21 octobre 2026, visions de biome), adaptée aux arènes de Froggy (jungle, marécage, désert, oasis ;
Notion). Une vision complète d'une arène, choisie par l'équipe, devient son **image-maîtresse** : la référence de style
de tous les assets de l'arène (skill `production-assets`).

## 1. Méthode

1. **Le style d'abord, puis les arènes** : la même scène dans plusieurs visions pour choisir le **rendu du jeu
   entier** ; puis chaque arène en quelques ambiances dans ce rendu.
2. **Tirages sur l'abonnement Gemini** (zéro crédit) : l'IA écrit les prompts et prépare les images à joindre ; un humain
   colle, tire et dépose ; l'IA mesure et donne son avis ; l'équipe choisit.
3. **Cadrage d'un écran de jeu** : l'angle et la hauteur de la vraie caméra (1re et 3e personne), 16:9, le sol jouable
   lisible.
4. **Trois messages par vision**, dans une même conversation : étude de valeurs, esquisse couleur, finition. Le
   générateur ne peint plus tout à la fois.
5. **Le personnage n'est pas dans les visions** : la scène laisse libre une zone calme, en valeur moyenne, où l'on posera
   Froggy à l'échelle pour juger l'accord.

## 2. Ce que le premier tour d'Eku a coûté (à ne pas refaire)

Cinq noms de style sur une capture couleur : treize tirages jugés « monotones, sans luminosité, trop IA ». Mesure : une
seule tranche de teinte pour 32 à 56 % des pixels, 0 à 6 % de chaud. Causes :
- **Une capture jointe impose sa palette et ses valeurs**, même « pour la composition seulement », même en gris →
  joindre un **plan de masse** (section 5).
- **Un prompt qui ne décrit que des froids** donne une image froide → toujours une gamme avec un chaud et un froid.
- **Un nom de style n'est pas une intention** → chaque vision est une thèse complète.
- **Une mise en page imposée au pixel près** donne une image raide → seul le sol jouable est gardé.

## 3. Principes de peinture

- **Valeur avant couleur** : trois ou quatre grandes masses ; le point focal porte le plus fort contraste ; le plan de
  jeu a sa bande de valeur à part ; le fond ne gagne son détail qu'après avoir été repoussé.
- **Une gamme 60 / 30 / 10** choisie d'avance ; **lumière chaude, ombre froide** (ou l'inverse), partout ; les chauds
  avancent, les froids reculent ; la saturation est un budget.
- **Bords nets au point focal, perdus ailleurs ; détail à deux ou trois endroits ; zones de repos** ; grandes, moyennes,
  petites formes, jamais un motif répété régulièrement.
- **Une histoire de lumière** : principale, renvoyée, liseré, lumières locales qui racontent le jeu (pour Froggy :
  la base de chaque équipe, le cookie sacré).
- **Ce qui fait humain** : des choix et des sacrifices visibles, un médium et un geste nommés.
- **Lisibilité multijoueur** : les couleurs d'équipe et celle du cookie sont réservées ; le décor ne les emploie pas.

## 4. Anatomie des trois messages (Gemini, en anglais)

Guide Google pour Nano Banana : phrases et non mots-clés, verbe fort en tête, rôle de chaque image jointe, consignes en
positif, matières nommées, corrections par la conversation.

- **Message 1, étude de valeurs** (+ plan de masse) : « Paint a quick value study… grayscale thumbnail, 16:9, four flat
  values, big simple shapes » ; rôle du plan de masse (sol jouable imposé, cadres = repères nommés, le reste libre) ; le
  lieu en une phrase ; scène vide ; contraste maximal au point focal ; zones de repos.
- **Message 2, esquisse couleur** (+ carte de palette) : « follows your value study exactly » ; la carte est la seule
  référence de couleur, dans ses proportions ; gamme 60 / 30 / 10 avec la relation chaud/froid ; histoire de lumière ;
  « large areas of color, no detail yet ».
- **Message 3, finition** (sans image) : garder composition, valeurs et couleurs ; médium et geste ; détail au seul point
  focal ; **la vie du lieu** (pour un marécage : libellules, rides, roseaux, brume basse) ; clôture : « The image
  contains only the scene: no character, no text, no interface, no frame, no color swatches. »

Une vision par conversation (un style vu déteint sur le suivant). Les visions d'un tour partagent la base de valeurs et
diffèrent par la palette, la température et le rendu.

## 5. Les images à joindre

- **Plan de masse** plutôt que toute capture : une vue de l'arène en blocs gris (sol jouable, nénuphars, eau, obstacles)
  sous l'angle de la caméra, sans matière ni lumière. À fabriquer dans Unity (matériau gris unique, capture : skill
  `unity-mcp`) ; un cadre au trait marque un élément imposé (base, cookie).
- **Carte de palette** (une par vision, jointe au message 2) :
  `uv run --with pillow python -I Tools/art/carte_palette.py <sortie.png> F4E9D3:20 BFEDE3:20 "|" E9C88A:10 "|" F08A7E:4`.
- **Notre propre étude de valeurs** est le meilleur moyen de fixer le terrain : Gemini peint par-dessus sans bouger les
  formes. Le mode retouche de Gemini, lui, **ne déplace rien** (Eku : « déplace les plateformes » a laissé l'image
  identique, 0 % de pixels changés).

## 6. Juger les tirages

`uv run --with pillow --with numpy python -I Tools/art/juger_vision.py <dossier>` donne la part de la teinte dominante,
la part de chaud, le nombre de tranches, saturation et valeurs, et écrit les versions **en gris** et **en flou** dans
`_tests/`. Puis regarder chaque tirage : lisibilité en jeu, identité, déclinable à toutes les arènes, productible asset
par asset, place laissée à la lumière du moteur. L'avis dit ce qui est bon à garder même dans un tirage raté.

Leçons :
- Le déroulé en trois messages marche (Eku : 16 à 66 % de chaud, 6 à 9 tranches, contre 0 à 6 % et 3 à 7).
- **Contrôler le sol jouable** contre le plan de masse à chaque étape : une relance de finition peut changer le jeu
  (combler une fosse, déplacer un nénuphar).
- **Poser le personnage à l'échelle** dans chaque finition aide à juger l'accord ; une dominante proche de ses couleurs
  le noie.
- **S'inspirer, pas recopier** : une image jointe « pour la couleur » se fait recopier (objets collés avec leur lumière
  d'origine). Reprendre un **principe**, le traduire dans la lumière de la scène ; ne joindre qu'un élément précis, découpé.
- **Penser en joueur** : chaque prompt décrit le terrain par sa fonction (surfaces plates et assez larges pour
  atterrir, chemin lisible, rien de posé là où l'on marche, objets à leur vraie taille).
- **Retoucher une seule chose à la fois**, en disant chaque fois ce qui reste.
- **Une conversation par couche** si l'on décompose une image : dans une même conversation, chaque message devient une
  retouche de la précédente.
- **La finition dépend du modèle** : avec un modèle, la finition a recopié les objets de la référence ; relancée avec un
  autre en réflexion maximale, elle a respecté l'esquisse. Noter quel modèle a donné quoi.
- **L'ambiance du moteur suit l'image-maîtresse** : brume, lumière, ombres et ambiante se règlent sur elle en même temps
  que les assets arrivent.

## 7. Goûts déjà connus de Florent (Eku, Lovecraft)

Lumineux et coloré, jamais une seule teinte ; profondeur sombre devant, claire au fond, mais **colorée à chaque plan**
(un premier plan sombre en prune, sarcelle ou violet, jamais en noir brun) ; la lumière qui traverse (rayons, brume) ; un
monde **vivant et détaillé** (le détail plaît quand il est vivant : la hiérarchie le place, elle ne le supprime pas) ;
peinture douce et chaude plutôt que sombre et précieuse (sur Eku, il a choisi les visions lumineuses, pas celle que
Claude classait première). Lovecraft : « naturel, humain, vivant, organique ». Trop de lueurs d'une même couleur nuisent
à la lecture : une couleur vive signale **un seul état**. Ces goûts guident sans remplacer ceux de l'équipe de Froggy.

## 8. Consigner

**Le guide se donne dans le chat** : quelle conversation, quels fichiers joindre, chaque prompt dans son bloc à copier,
où enregistrer le résultat. Le même guide s'écrit dans un `.md` du tour (images hors git, dans `Captures/directions/`).
