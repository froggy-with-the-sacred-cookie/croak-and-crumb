---
name: camera
description: Règles de la caméra de Froggy (bascule première / troisième personne, un seul propriétaire, interpolation au pas physique, caméra qui évite les murs sans faire de demi-tour, secousses dosées). À charger AVANT d'écrire ou de modifier la caméra, son suivi, la bascule 1re/3e personne, un cadrage d'arène, un événement qui doit attirer le regard ou secouer l'écran, ou la caméra de nage. Aussi quand quelqu'un dit que la caméra « saute », « traîne », « tremble », « passe à travers les murs », « fait des 180° », « cache Froggy » ou « ne montre pas où on atterrit ».
---

# Caméra

Notion : on peut passer de la première à la troisième personne en cours de partie. Leçons d'Eku (caméra de
plateforme, références : Itay Keren, « Scroll Back ») et de Lovecraft (caméra 3D qui passe en troisième personne
derrière les bâtiments).

## 1. Architecture

1. **Un seul propriétaire.** Rien d'autre ne déplace la caméra : une secousse passe par une seule fonction (skill
   `impact-feedback`), un point d'intérêt par un composant qu'elle lit, des bornes par l'arène.
2. **Au même rythme que ce qu'elle suit.** Si le personnage bouge au pas physique avec interpolation, la caméra se
   calcule après lui et s'interpole avec lui. Ne jamais la suivre dans `Update` sur une position interpolée à la main :
   les deux lissages se battent (saccades).
3. Après un saut net (réapparition, téléportation, bascule de vue) : remettre l'interpolation à zéro, sinon l'image
   glisse depuis l'ancien cadre.
4. **Lissage par ressort critique** (sans dépassement), visée continue : une visée bornée doit rester une **fonction
   continue** de la position du joueur. Eku, 19 octobre 2026 : des bornes qui changeaient d'un coup collaient le cadre au
   bord, puis il rattrapait à 6 fois la vitesse de course.
5. Ce qui se juge contre l'écran (marges, ancres) se règle **en fraction du cadre**, jamais en unités du monde.
6. **En multijoueur, la caméra est locale** : elle suit la position prédite et lissée du joueur, jamais l'état brut
   reçu du réseau.

## 2. Ce qui a marché pour un jeu de sauts (Eku)

- Privilégier le **point d'atterrissage probable** : en l'air, le cadre ne monte que si le joueur dépasse nettement son
  dernier sol ; posé, il se recale.
- Regarder devant (anticipation dans le sens de la course), avec un glissement doux au demi-tour.
- Projection en chute **seulement sous le dernier sol**, en compensant d'abord le retard du ressort : appliquée à toute
  chute, elle faisait plonger le cadre à chaque réception ; sans compensation, on ne voyait plus où on tombait.
- Zoom pour un événement : jamais au point de cacher le joueur ou un danger.

## 3. Troisième personne en 3D (Lovecraft, octobre 2026)

- **Rien ne devient transparent** : la silhouette fantôme à travers les murs a été refusée. Derrière un obstacle, la
  caméra cherche une place libre.
- Elle **se rapproche plutôt que de tourner** : chercher cap × inclinaison × distance dans un cône autour du cap habituel
  (Lovecraft : écart max 60°, inclinaison max 40°, distance jusqu'à 30 %). Retenir une place seulement si le joueur
  s'y voit **et** si le bras de caméra peut s'y tendre sans traverser.
- Ne pas chercher le dos du joueur s'il marche vers la caméra ; plafonner la vitesse de rotation (« elle fait des
  180° » était la plainte).
- Le repère des touches reste figé tant qu'on garde la même direction (repris après une courte pause ou un grand
  écart) : sinon la direction tenue change pendant que la caméra tourne.
- Sur un trajet coupé : monter un instant, raccourcir le bras (vite), le rallonger (lentement), et couper net en dernier
  recours (une fois par seconde au plus).

## 4. Secousse

Posée **aussitôt** dans le sens du coup, puis elle oscille et s'amortit **au temps du jeu** (le temps réel est faux en
test accéléré et laisserait la secousse s'éteindre pendant un gel d'image). Deux secousses ne s'ajoutent pas : la plus
forte l'emporte. Son intensité se dose dans les options du joueur (skill `accessibilite`). En première personne, une
secousse forte rend malade : la réduire nettement par rapport à la troisième personne.

## 5. Avant de dire « c'est fait »

Un test vérifie le suivi à 30, 60 et 144 images par seconde (pas de saut, pas de dépassement, joueur toujours dans le
cadre). Amortis, amplitude des secousses et confort de la bascule se jugent en jeu par un humain : le dire.
