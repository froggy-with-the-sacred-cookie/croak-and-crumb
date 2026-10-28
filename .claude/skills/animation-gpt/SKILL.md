---
name: animation-gpt
description: Faire produire et animer proprement des images 2D par GPT dans Codex (icônes, HUD, flipbooks d'effets, sprites d'interface, portraits) — choisir entre planche de geste et retouches d'une pose clé, écrire les prompts d'image, monter sans faire glisser, vérifier la provenance. À charger AVANT d'écrire ou de relire un prompt d'image ou d'animation pour Codex ou GPT, de monter des poses générées, ou de juger une animation livrée par Codex. Aussi quand quelqu'un dit qu'une animation de GPT « n'est pas propre », « bout », « grouille », « change d'objet d'une image à l'autre », ou que « ça marchait la semaine dernière ». Charger aussi production-assets.
---

# Images et animations 2D par GPT (Codex)

Leçons d'Eku (21-22 octobre 2026, plusieurs jours perdus puis retrouvés) et de Glouton (banques d'effets et sprites de
HUD livrés par Codex). Pour Froggy, jeu 3D, cela sert aux **icônes, au HUD, aux flipbooks d'effets** (gouttes,
éclaboussures, étincelles) et aux portraits.

## 1. Ce qu'est GPT dans Codex

- L'agent de Codex **ne dessine pas** : il écrit les prompts, appelle l'outil d'image de Codex (modèle GPT Image, ni
  choisi ni affiché : issue openai/codex #43965), juge les tirages et monte. Ne jamais promettre un modèle d'image précis
  ni attribuer une régression au modèle sans preuve.
- L'outil accepte `prompt`, `referenced_image_paths` et `transparent_background`. Pas de masque, pas de modèle à
  choisir (cela demande l'API, payante : à annoncer avant).
- Les journaux de Codex (`~/.codex/sessions/AAAA/MM/JJ/*.jsonl`) gardent chaque appel d'image avec son prompt exact :
  c'est là qu'on trouve pourquoi une livraison a dérivé.
- Pour une commande d'art, lancer Codex en effort de raisonnement **élevé** (meilleurs prompts, meilleur tri) ; ne pas
  modifier sa configuration sans accord.
- Codex livre **dans un dossier à part** (`proposals/<lot>/`) avec un `POUR_CLAUDE.md` (prompts exacts, images jointes,
  ce qui est fait) ; l'intégration dans le jeu vient après, une pièce à la fois (Glouton).

## 2. Deux méthodes, selon ce qui bouge

| Mouvement | Exemples | Méthode |
|---|---|---|
| **Geste** : la forme change beaucoup | explosion, éclaboussure, saut d'une icône, coup | toutes les poses **tirées ensemble sur une planche**, la pose clé jointe ; puis découpées et montées |
| **Sur place** : l'essentiel ne bouge pas | icône qui respire, flamme, plante, objet qui scintille | **une pose clé**, puis chaque pose est une **retouche de la clé** ; le pied (la base) est repris de la clé au montage |

Pourquoi : tirer des poses ensemble redessine tout. Pour un geste, ça ne se voit pas ; sur place, chaque pose devient un
autre objet et l'animation **bout** (Eku : pied changé de 15 à 19 %, silhouette à 0,55).

Dans les deux cas : **d'abord une pose clé** validée ; **peu de dessins bien tenus** valent mieux que beaucoup qui
dérivent ; **jamais une pose fabriquée en déformant les pixels d'une autre** (cisaillement, morceaux glissés,
étirement : « les jambes se mélangent »).

## 3. Écrire le prompt d'image

- **Références numérotées, avec leur rôle** : « Image 1 is the exact identity: this same <sujet>. Image 2 is the style
  reference only, not its scenery or light. »
- **Une retouche = une seule consigne**, puis les invariants : « Change only: <ce qui bouge, de combien, dans quel
  sens>. Keep everything else identical: <base, nombre et place des éléments, couleurs, échelle, position, coup de
  pinceau>. » Les invariants se **répètent à chaque appel**.
- Une retouche part **toujours de la clé**, jamais de la retouche précédente : la dérive s'accumule.
- **Gauche et droite du point de vue du sujet**, avec « No mirroring ».
- **Ne jamais écrire** « paint each cel freshly », « new brushwork », « not copied pieces » pour une pose sur place :
  c'est la phrase qui a fait bouillir les plantes d'Eku.
- Lumière neutre venue du dessus, pas d'ombre portée ni de halo, pas de texte.
- Un tirage qui sort du modèle **se retire 2 ou 3 fois** avant toute retouche corrective.
- **Fond** : transparent demandé ; s'il revient coloré, le détourer localement, **jamais** par une retouche (elle
  redessine tout).

## 4. Monter

- **Un seul cadrage pour toutes les poses**, jamais chacune centrée sur sa propre boîte (la base glisserait).
- **Un seul facteur d'échelle**, le même en x et en y et pour toutes les poses. Un dessin redimensionné à la boîte de
  l'ancien est écrasé.
- **Objet ancré** : recaler chaque retouche sur la base de la clé (l'outil d'image ne replace jamais au pixel : jusqu'à
  30 px d'écart mesurés) et y recoller cette base.
- **Ne jamais juger la stabilité à la couleur exacte** : une illustration lissée change presque toutes ses couleurs
  exactes d'une image à l'autre même quand elle est stable ; comparer à 16 niveaux par canal.

## 5. Vérifier une livraison

Eku avait une porte automatique (`tools/art/porte.py`, non reprise ici) qui mesurait : poses vraiment dessinées (pas
plus de 20 % de blocs repris d'une autre pose), base fixe pour un objet ancré, silhouette stable (≥ 0,70), et
**provenance** (chaque image montée sort bien de l'outil d'image de Codex, vérifiée par l'empreinte du fichier dans
`~/.codex/generated_images`). À recréer pour Froggy si les flipbooks se multiplient. Leçon : quand Codex a vu la porte
refuser à tort une bonne livraison, il l'a **signalé sans assouplir la porte** ; c'est le bon réflexe.

À l'œil : le même objet à chaque pose (même nombre d'éléments, à la même place) ; le mouvement se juge en jeu.

Sources de la recherche d'Eku : guides d'OpenAI « Image prompting » et « Image generation tool » ; issue openai/codex
#43965 ; forum des développeurs OpenAI, « Developing sprite sheets with gpt-image-2 ».
