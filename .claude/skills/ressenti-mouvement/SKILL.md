---
name: ressenti-mouvement
description: Règles du ressenti de contrôle de Froggy (tolérances de Celeste, contrôleur au pas physique, valeurs dérivées, mesures à plusieurs fréquences d'image, saut précis de nénuphar en nénuphar, nage, bonus de pluie). À charger AVANT d'écrire ou de modifier le contrôleur de la grenouille, ses valeurs de mouvement (course, saut, nage, langue, recul), ou d'ajouter un geste de déplacement (saut chargé, plongeon, rebond, plate-forme qui bouge). Aussi quand quelqu'un trouve le saut « flottant », « raide », « injuste », qu'un appui « ne part pas », qu'on « rate le nénuphar alors qu'on était dessus », ou que Froggy « accroche » un rebord.
---

# Ressenti du mouvement

Leçons d'Eku (contrôleur jugé bon au premier essai, octobre 2026), adaptées à une grenouille en 3D. Notion demande
des sauts **précis** de nénuphar en nénuphar sur un terrain étroit, et une pluie qui **accélère** les grenouilles : le
mouvement est le cœur du jeu, les capacités l'enrichissent, elles ne le remplacent pas.

## 1. Les tolérances (Celeste, « Celeste & Forgiveness »)

| Tolérance | Ce qu'elle fait | Valeur de départ d'Eku |
|---|---|---|
| Coyote time | Le saut part encore juste après avoir quitté le bord | 0,12 s |
| Tampon de saut (et des autres gestes) | Un appui un peu trop tôt part à l'atterrissage | quelques centièmes de seconde |
| Saut variable | Relâcher coupe le saut, **seulement** ce qui vient d'un saut | multiplicateur de coupe |
| Correction de coin | Un saut qui frôle un rebord glisse dessus, jamais contre la direction tenue | 0,25 unité |
| Apex suspendu | Gravité réduite au sommet | à programmer **coupé**, à essayer |

Une tolérance est **invisible quand elle marche** : elle ne change aucune valeur mesurée sur sol plat. En 3D, ajouter
l'équivalent pour les nénuphars : une marge d'atterrissage au bord du nénuphar (le joueur était « dessus » à l'œil).

## 2. Règles du contrôleur

1. Un appui se capte dans `Update`, se consomme ET s'efface au pas physique suivant, même sans effet.
2. L'état se recalcule depuis zéro à chaque pas (cascade à priorité fixe) ; une posture (accroupi) n'est pas un état.
3. **Les hauteurs se donnent, les vitesses se dérivent** : on règle la hauteur et la portée d'un saut, la vitesse
   initiale en découle (`v = √(2gh)`), jamais saisie à la main.
4. Gravité en deux demi-pas autour du déplacement : l'apex vaut exactement la hauteur voulue. Une gravité qui change en
   cours de pas s'intègre par morceaux, sinon l'apex dérive (Eku : −0,02 unité avant correction).
5. Une vitesse imposée (rebond, poussée) ne se coupe pas au relâchement ; les effets ponctuels se consomment avant tout
   retour anticipé.
6. Une téléportation prévient **avant** de déplacer, puis remet l'interpolation à zéro : sinon l'affichage glisse.
7. Une attaque ou une capacité prolonge le mouvement, elle ne le fige jamais.
8. **Les valeurs vivent dans un réglage** (ScriptableObject), commentées et marquées PROVISOIRE, jamais en dur.
   Une valeur jugée bonne par l'équipe ne change pas sans nouvel essai : une amélioration se programme **coupée par
   défaut** et se propose (précédent d'Eku : l'apex suspendu).

## 3. Multijoueur

Le contrôleur est prédit chez le joueur et rejoué par l'autorité (skill `multijoueur-equilibrage`) : il doit être
**déterministe à entrées égales** (pas de `Time.deltaTime` d'affichage dans la physique, pas d'aléa non partagé). Un
bonus de vitesse (la pluie) est un multiplicateur lu dans l'état de la partie, identique pour tous.

## 4. Nage (leçons d'Eku)

Demi-tour **net** (contre-exemple : Song of the Deep, dont le flottement empêche la précision) ; l'inertie se **voit**
(animation, éclaboussures, rides) plutôt qu'elle ne se sent dans le contrôle. La **sortie d'eau avec élan** est le geste
le plus expressif des jeux de nage (Ori) : son, gerbe d'eau, peut-être un bonus. La caméra a besoin d'un réglage de
nage (skill `camera`), le son d'un filtre en immersion (skill `son-integration`). Pour une vraie grenouille (poussée,
saut, nage), faire la recherche d'abord (skill `recherche-reference`).

## 5. Avant de dire « c'est fait »

- Un test mesure le saut (hauteur, portée, durée de vol) à **30, 60 et 144 images par seconde** : vert partout. Tout
  comportement nouveau y reçoit sa mesure ; s'il change la durée du vol, la théorie du test change avec lui.
- Un réglage qui toucherait un mouvement validé s'essaie sur une **copie** du réglage dans le test, jamais sur
  l'asset partagé.
- Le ressenti se juge **en jeu, à la manette et au clavier, par un humain** : le dire dans le compte rendu.
