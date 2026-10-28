---
name: modele-3d-blender
description: Modéliser par script Blender (gratuit) un objet de Froggy qui se reconnaît d'un coup d'œil et qui a de vraies animations (cookie sacré, cookie, donut, ramassable, bonus, décor simple), puis le brancher dans Unity et le vérifier en jeu. Méthode rodée sur le filon de cuivre de Lovecraft : quatre concepts Gemini, planche de référence à 6 vues, modèle par script Blender, préfabriqué en variante du FBX, test en jeu. À utiliser quand on veut faire ou refaire un objet en 3D sans crédits, qu'un modèle est « trop simpliste » ou « pas reconnaissable », qu'un objet doit ressembler à son icône, ou qu'on parle de modéliser dans Blender ou d'animer un objet pièce par pièce.
---

# Objets taillés et peints, par script Blender

Le but (Lovecraft, 29 septembre 2026) : qu'un joueur reconnaisse l'objet **de loin, sans lire son nom**, et que chaque
interaction soit un petit spectacle (une pièce saute à chaque coup, l'objet s'épuise, puis revient). Le premier essai,
une roche lisse avec trois boules, a été jugé « trop simpliste, on a besoin de vraiment bien identifier un élément ».
Pour Froggy, c'est la route des **cookies**, des ramassables et des bonus : un objet de jeu se lit avant d'être joli.

Les briques Blender sont dans `Tools/blender/blender_lib.py`. Ce qui demande un modèle organique complexe (la grenouille,
un personnage) passe plutôt par Meshy (skill `route-3d-meshy`).

## Le déroulé

À chaque étape visuelle, **ouvrir l'image** (`open <fichier>`) et attendre l'avis de l'équipe quand un choix esthétique
se joue. Si l'équipe délègue (« choisis »), choisir et dire pourquoi.

### 1. Cadrer l'objet

Avant toute image, établir en lisant le code (pas en supposant) : comment le jeu le charge (préfabriqué, catalogue,
clé), son icône s'il en a une (référence de style n° 1), où il est dans une arène (il en faut un pour le test), sa vie
(combien d'interactions, donc combien de pièces animées ; ses états : plein, entamé, épuisé, qui revient), et **ce qui le
rend reconnaissable** : matériau signature, silhouette, contraste avec l'arène (un cookie doré sur un marécage vert).

### 2. Quatre concepts (Gemini)

Quatre pistes **vraiment différentes**, avec deux images de référence : l'icône, et une vraie capture du jeu à
l'endroit où l'objet se trouve (lumière, échelle, angle). Le prompt insiste sur l'identification : silhouette forte,
contraste du matériau signature, **exactement N pièces distinctes du côté visible**. Modèles de prompts :
`references/prompts.md`. L'équipe choisit, ou mélange (« F2 avec les pépites de F1 »).

### 3. La planche de référence (6 vues)

Face, profil, dos, dessus, trois-quarts, et **l'état épuisé** (pièces retirées, creux visibles), en 2K. C'est elle qui
guide la modélisation : la regarder à chaque rendu.

### 4. Le modèle, par script Blender

Un script par objet, qui s'exécute **dans Blender** (MCP `blender`, ou `Blender -b -P <script>`) et commence par
`exec(open(os.environ["FROGGY_ROOT"] + "/Tools/blender/blender_lib.py").read())`. Briques : `pierre` (forme taillée par
plans de coupe, arêtes biseautées), `pepite` (pièce taillée), `creuser` (creux par booléen), `tube` / `veine`,
`peindre`, `ombre`, `couleur_metal`, `couleur_roche`, `ajouter_contour`, `objet`, `exporter`, `rendre` (rendu de
contrôle sous l'angle du jeu).

Règles qui ont fait la différence :
- **Grandes facettes planes**, pas de « boule géodésique » : tailler un ellipsoïde par des plans, biseauter les arêtes
  en un peu plus clair. C'est ce qui donne l'air « peint ».
- **Couleurs dans les sommets, en sRGB 0-255**, peintes par facette avec une lumière douce en haut à gauche ; le jeu
  éclaire par-dessus, donc pas d'ombres dures. Compenser la lumière du lieu (sous une lumière chaude, un gris paraît
  brun : prendre un gris bleuté).
- **Un contour sur chaque pièce**, qui part avec elle.
- **Les pièces animées** s'appellent `Morceau_1…N`, origine au centre de la pièce, grosses, bien séparées, **du côté
  visible** (le code tourne ce côté vers la caméra). Sous chacune, un **creux** peint à part, pour que l'état vidé se lise.
- **L'état épuisé est un second modèle**, pas un reste générique.
- **Budget** : ~8 000 faces pour un objet de jeu (contours compris), ~5 000 pour un état épuisé ; les miettes en `sub=1`.

Contrôler avec `rendre()` sous l'angle du jeu, plein puis vidé, à côté de la planche. Itérer **avant** Unity.

### 5. Brancher dans Unity

Vérifier d'abord qu'aucune partie ne tourne (skill `unity-mcp`). Le préfabriqué est une **variante du FBX** : chaque
nouvel export s'y reporte, positions des pièces comprises. Rebrancher l'entrée de catalogue à chaque fois (refaire un
préfabriqué change l'identifiant de sa racine : le jeu retombe sur l'ancien modèle **sans erreur**). Vérifier la liste des
`Morceau_*` trouvés.

### 6. Tester en jeu

Sur une sauvegarde de test : capturer tout le cycle (plein, chaque interaction, dernier coup, épuisé, retour) et la
**vue de loin** au zoom normal du jeu. Questions : se reconnaît-il sans lire son nom ? Les pièces partent-elles de leurs
creux ? Les couleurs tiennent-elles sous la lumière de chaque arène et sous la pluie ?

### 7. Finir

Ouvrir la planche finale, résumer ce qui a changé, noter les pièges nouveaux dans `references/pieges.md`, proposer un
commit local (« Sujet : ce qui change »). Jamais de push sans accord.

## Critères d'un bon objet

Reconnaissable de loin ; fidèle à la planche ; pièces animées visibles qui laissent une trace ; chaque état raconte
quelque chose ; léger.

## À lire selon le besoin

- `references/prompts.md` : prompts Gemini des concepts et de la planche.
- `references/pieges.md` : ce qui a coûté du temps sur le filon de cuivre. À relire avant le premier test en jeu.
