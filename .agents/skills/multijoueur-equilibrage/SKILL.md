---
name: multijoueur-equilibrage
description: Règles du multijoueur de Froggy (autorité, état synchronisé ou rejoué, prédiction et lissage, départs et arrivées, bots) et méthode d'équilibrage par simulation headless (anti-boule-de-neige, rattrapage, mesures). À charger AVANT d'écrire du code réseau (FishNet), une règle de score, de capture, de réapparition, un bonus ou un malus, l'IA des bots, ou de régler un chiffre d'équilibrage. Aussi quand quelqu'un dit qu'une partie « est pliée d'avance », qu'un joueur « ne peut plus revenir », que « ça saccade en ligne » ou que « le leader écrase tout ».
---

# Multijoueur et équilibrage

Froggy est **exclusivement multijoueur** (2 à 4 joueurs, Notion) avec **FishNet**. Ce skill garde ce qu'a appris
Glouton (Unity 6, 2 à 6 joueurs en ligne, octobre 2026) ; il est indépendant de la bibliothèque. Pour l'API FishNet,
lire la documentation de la version installée : ne jamais l'écrire de mémoire.

## 1. Autorité et synchronisation (Glouton)

- **Une seule autorité** : l'hôte (ou le serveur) simule les règles, le score, les captures, les dégâts. Un client
  envoie ses **entrées**, jamais un résultat (« j'ai pris le cookie »).
- **État fréquent, non fiable et séquencé** pour ce qui bouge en continu (positions : Glouton envoyait 20 fois par
  seconde) ; **événements fiables** pour ce qui ne doit jamais se perdre (« le cookie 3 est pris par le joueur 2 »,
  début, fin, capture).
- **Ce qui se reconstruit ne s'envoie pas** : les clients rebâtissaient tout le village (900 objets) depuis une
  **graine** commune et jouaient localement les effets. Environ 3 Ko/s par client.
- **Son propre personnage est prédit** depuis ses entrées puis recalé en douceur ; les autres sont lissés. Un recalage
  sec se voit : il se fait sur quelques images.
- **Les effets de ressenti sont locaux** : gel d'image, secousse, vibration ne ralentissent jamais la simulation de
  l'autorité (skill `impact-feedback`).
- **Départ en cours de partie** : le joueur est remplacé par un bot. **Arrivée en cours** : il attend la manche
  suivante. Partie rapide : départ quelques secondes après l'arrivée d'un deuxième joueur, ou bots au bout d'un délai.
- Partie rapide et codes privés passent par un service de relais (Glouton : Unity Relay, qui demande de relier le
  projet à Unity Cloud, *Project Settings > Services*). Le réseau local par IP marche sans. Avec FishNet, vérifier le
  transport et le relais choisis par l'équipe.

## 2. Tester sans quatre humains

- **Des bots dès le début** : ils remplissent les parties et servent de joueurs simulés. Leurs réglages (vitesse, champ
  de vision, agressivité, taux d'erreur, « clémence » envers un humain un peu plus faible) font la difficulté.
- **Options du binaire** pour tester sans souris (Glouton) : `-autoplay -shots <dossier>` (l'IA joue, captures et
  journal), `-players n`, `-bots n`, `-difficulty`, `-roundtime s`.
- Réseau local : le premier lancement d'un build sur macOS ouvre la fenêtre du pare-feu, qui bloque le test. Relancer
  avant d'accuser le code.

## 3. Équilibrer par la simulation

Méthode de Glouton : un mode `-sim N` enchaîne N parties accélérées sans affichage, le « joueur » étant un bot de niveau
moyen ; un petit script résume (`tools/sim_report.py`). Mesurer : taux de victoire, rang moyen, écart premier-dernier,
moment où la partie se décide, nombre de retournements, morts par partie. Des interrupteurs coupent une mécanique à la
fois (`-nospit`, `-nopowers`) pour voir ce qu'elle change.

**La boule de neige est le danger n° 1** d'un jeu où l'avance donne de la force. Leçon de Glouton : tout coup qui
affaiblit le perdant fait basculer la partie vers le leader. Leviers qui ont marché :
- **Croissance amortie** : le gain diminue quand on est déjà fort (Glouton : divisé par `1 + (r/5)²`).
- **Départ rapide** : les premiers gains comptent double.
- **Rattrapage** : celui qui est en retard progresse plus vite (jusqu'à +80 %).
- **Prime sur le leader** : le battre rapporte plus (idée des jeux .io).
- **Le faible peut fuir** : la vitesse ne grandit qu'en racine de la force.
- **Réapparition** à 70 % de sa force, invulnérable 3 s, loin des menaces.

Pour Froggy (propositions, à juger par l'équipe) : dans un *Capture The Flag*, la boule de neige vient de l'équipe qui
mène et contrôle la carte ; les leviers ci-dessus se traduisent en temps de réapparition, en distance de la base, en
malus portés par le porteur du cookie (l'idée Notion du cookie changé en donut va dans ce sens).

Cibles chiffrées **avant** de régler (Glouton : Facile ~75 %, Normal ~35-40 %, Difficile ~20 % de victoires du joueur
simulé), 16 à 32 parties par réglage, et un **climax** en fin de partie plutôt qu'une partie pliée à la moitié.
L'équilibrage final se juge **à plusieurs humains** : la simulation dit où regarder, pas ce qui est amusant.

## 4. Physique des objets nombreux (Glouton)

Environ 260 objets se couchaient dans la première seconde sans que personne ne bouge : des petits objets naissaient
dans les boîtes des grands et PhysX les éjectait. Ce qui a réglé : réserver toute l'emprise tournée du grand objet au
placement, faire naître les objets **endormis**, plafonner la vitesse de dépénétration (1,5 m/s), et faire des
personnages qui marchent dans le décor des déclencheurs plutôt que des corps qui bousculent.

## 5. Rétention (recherche de Glouton, octobre 2026)

Un bon jour 1 suivi d'un mauvais jour 7 vient presque toujours d'un manque de méta-progression. Ce qui retient : niveaux
et XP à chaque partie, apparences liées aux habitudes de jeu et aux exploits, trois défis du jour tirés d'une graine
basée sur la date (les mêmes pour tous), série de jours, écran de fin qui donne envie de relancer (« encore une »),
première partie guidée par quelques conseils au bon moment. Tout cela reste une proposition pour Froggy.

## 6. Performance mesurée (Glouton, Apple M4, 1080p)

Image médiane 3,3 ms en solo, 7,2 ms à 4 vues. Ce qui comptait : Forward+, SRP Batcher et GPU Resident Drawer actifs,
distance des ombres qui suit la caméra, HUD qui ne reconstruit ses textes que quand ils changent, textures externes
réduites (build de 399 à 99 Mo, skill `route-3d-meshy`).
