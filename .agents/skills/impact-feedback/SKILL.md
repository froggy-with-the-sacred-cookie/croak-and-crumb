---
name: impact-feedback
description: Règles du poids et de la lisibilité des actions de Froggy (gel d'image, secousse, vibration, échelle de poids, télégraphes, équité, capacités et malus lisibles) en multijoueur. À charger AVANT d'écrire ou de modifier un coup, une capacité, un malus, une capture de cookie, des dégâts, un télégraphe, un gel d'image, une secousse ou une vibration. Aussi quand quelqu'un dit qu'une action « manque de poids », qu'une capacité est « injuste », « illisible », « arrive sans prévenir », ou qu'un affrontement est « fouillis ». Charger aussi rendu-vivant pour la partie visuelle.
---

# Poids et lisibilité des actions

Leçons du combat d'Eku (références : Sakurai pour le gel d'image, Vlambeer et « Juice it or lose it » pour les
retours, Doom 2016 pour les jetons d'attaque, Hollow Knight pour le recul), et du « juice » de Glouton.

## 1. Le poids d'une action

Ce qui compte plus s'arrête plus longtemps et secoue plus. Échelle d'Eku, comme point de départ :

| Action | Gel (s) | Secousse (unités) |
|---|---|---|
| Petit coup rapide | 0,05 | aucune |
| Coup appuyé | 0,06 à 0,07 | 0,07 à 0,08 |
| Coup qui élimine | 0,09 | 0,12 |
| Grand moment (capture, victoire) | 0,30 | 0,30 |
| Coup reçu | — | 0,18 |

Règles :
1. **Proportionner** : un nouveau geste se place dans l'échelle, il ne l'écrase pas.
2. **Réserver** la secousse aux actions qui comptent : une suite rapide qui secoue à chaque fois devient du bruit.
3. **Un seul chemin** : une fonction unique fait secousse ET vibration, dosées par les options du joueur. Jamais un
   appel direct à la vibration ou à la caméra ailleurs.
4. **Direction** : la secousse part dans le sens du coup, pas au hasard.
5. **Jamais cumulé** : un gel en cours ignore le suivant ; la secousse la plus forte l'emporte.
6. Le gel d'image arrête le temps : un minuteur qui doit le traverser (son, effet) suit le temps réel.
7. Les valeurs vivent dans un réglage, marquées PROVISOIRE, jamais en dur.

## 2. En multijoueur

- **Le gel d'image est un effet local d'affichage** : il ne ralentit jamais la simulation de l'autorité ni les autres
  joueurs. Chacun ressent ce qui lui arrive (le frappeur et le frappé), pas les coups lointains.
- Le résultat (touché, cookie lâché) vient de l'autorité ; l'effet peut partir tout de suite chez le joueur qui agit
  (prédiction), mais ne promet rien que l'autorité pourrait refuser.

## 3. Lisible et juste

- **Télégraphe visuel ET sonore** avant toute action majeure d'un adversaire ou d'un piège, **constant** d'une fois sur
  l'autre : c'est sa constance qui l'apprend. Il se lit **en niveaux de gris** (aucune information par la seule
  couleur ; en multijoueur, la couleur d'équipe ne suffit pas non plus).
- **Récupération lisible** après chaque action forte : une fenêtre pour répondre.
- **Aucune perte sans avertissement** : un malus (le cookie qui devient donut, idée Notion) s'annonce avant ou se voit
  clairement au moment où il frappe, avec sa cause.
- **Jetons d'attaque** (Doom 2016) si des bots peuvent attaquer à plusieurs : chacun prend un jeton avant d'attaquer.
- Garder le **recul** de ce qui est frappé : il décale la cible par rapport au décor et dit « touché ».
- Les dégâts de contact sont la première source de frustration « injuste » : n'en ajouter qu'en le signalant.

## 4. Le « juice » de Glouton

Popups de score, son avec variation de hauteur, particules, caméra qui recule quand on grossit, bannière de palier,
tremblement sur les grosses prises, ralenti de fin de partie. Chaque retour dit quelque chose au joueur ; aucun ne
cache l'action.

## 5. Avant de dire « c'est fait »

Un test vérifie l'échelle de poids (aucun geste au-dessus d'un plus important) et le respect des options (0 coupe).
Le poids se juge en jeu par un humain : le dire.
