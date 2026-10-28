---
name: accessibilite
description: Règles d'accessibilité de Froggy (réglages joueur, effets dosables, information jamais portée par un seul sens, couleurs d'équipe lisibles, touches affichées depuis l'Input System, réaffectation, confort de la première personne). À charger AVANT d'ajouter un effet qui secoue, clignote, flashe ou vibre, une information de jeu à l'écran ou par le son, un menu, une option, une commande, ou la bascule 1re/3e personne. Aussi quand on parle d'options, de difficulté, de confort, de daltonisme, de mal des transports ou d'accessibilité.
---

# Accessibilité

Repris d'Eku (références : Game Accessibility Guidelines, Celeste, Prince of Persia: The Lost Crown) et adapté à un
jeu multijoueur en 3D.

## Règles

1. **Tout effet fort se dose dès qu'il existe.** Les réglages du joueur vivent à part (hors sauvegarde de progression
   et hors réglages de jeu) ; un effet qui secoue, flashe ou vibre lit son intensité au moment où il part (0 coupe, 1
   joue tel que réglé). Une nouvelle famille d'effets ajoute sa clé, défaut 1, et sa ligne dans le menu d'options.
2. **Aucune information par la couleur seule** : forme, mouvement ou son la doublent. En multijoueur, les **équipes**
   se distinguent aussi par une forme ou un motif (pas seulement rouge contre bleu). Test en niveaux de gris des
   télégraphes et des repères.
3. **Aucune information par le son seul** : un événement important se voit aussi.
4. **Les touches affichées viennent des actions de l'Input System** (dernier périphérique utilisé), jamais écrites en
   dur, pour que la réaffectation suive dans le HUD et les aides.
5. **Commandes** : toute action est réaffectable ; déplacement par position physique (AZERTY et QWERTY) ; manette de
   première classe ; macOS intercepte `Ctrl`, `F3`, `F4`. La vibration va à la dernière manette utilisée.
6. **Première personne** : champ de vision réglable, secousse et balancement de tête réduits ou coupables (le mal des
   transports vient vite en vue subjective).
7. **Mode d'assistance** (proposition, à la Celeste) nommé sans jugement (Celeste a abandonné « Cheat Mode ») ; en
   multijoueur, il ne doit pas donner d'avantage sur les autres : plutôt contre des bots ou en entraînement.
8. Texte lisible et contrasté sur fond opaque ; des aides de touche plutôt qu'un long tutoriel écrit.

## Avant de dire « c'est fait »

Un effet nouveau : vérifier dans un test qu'il respecte le réglage à 0 (coupé) et à une valeur intermédiaire. Un
réglage nouveau : borné, écrit dans un fichier de **test** pendant le test, jamais dans le fichier réel du joueur.
