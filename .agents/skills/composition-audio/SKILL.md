---
name: composition-audio
description: Composer la musique et fabriquer les effets sonores de Froggy (avec Sound Forge ou un autre outil) — thème mémorable qui se fredonne, leitmotiv décliné par arène, forme et stems adaptatifs, grammaire d'un effet sonore (attaque, corps, queue, matière, variantes), équilibre des niveaux, nommage et formats. À charger AVANT d'écrire une partition, un stem, un thème d'arène ou de personnage, un stinger, ou de rendre un effet sonore. Aussi quand quelqu'un dit qu'une musique « manque de quelque chose », « n'est pas mémorable », « tourne en rond », « lasse », ou qu'un son est « générique ». Pour brancher le résultat dans le jeu, charger aussi son-integration.
---

# Composer et fabriquer le son

`son-integration` dit comment un son **entre** dans le jeu ; ce skill dit comment l'**écrire**. Repris d'Eku (direction
audio et composition des thèmes, octobre 2026) et de Glouton (SFX et musiques générés avec Sound Forge). Tout ce qui
sort d'ici est une **proposition à écouter** : l'équipe juge à l'oreille, en jeu.

## 1. Avant de produire

Pour chaque demande : la **fonction** exacte (partie, menu, moment fort, signal de danger, interface, ambiance), la
durée visée ; une **intention** en deux phrases au plus ; trois traits de **signature sonore** (timbre, rythme, espace).
**Ne jamais annoncer un fichier audio si aucun rendu réel n'a été fait.** Pas d'imitation d'une œuvre ou d'un
compositeur identifiable.

## 2. Un thème mémorable

Une boucle d'accompagnement n'est pas un thème. Avant de rendre, vérifier :
1. **Une phrase, pas des notes** : idée de deux mesures, sa reprise variée, une continuation qui fragmente et accélère,
   une cadence (la « phrase » de Schoenberg). Huit mesures.
2. **Un rythme signature**, tiré de la mécanique du jeu (pour Froggy, le saut : appel, envol, réception ?),
   reconnaissable tapé sur une table. Garder le rythme et changer les notes pour développer.
3. **Un contour familier et un détail inattendu** : monter puis redescendre, avec UN intervalle qui surprend
   (Jakubowski 2016).
4. **Un sommet unique**, préparé, une ou deux fois par boucle.
5. **Appel et réponse**, et des **silences** : le thème se tait, puis revient.
6. **Un timbre propre au lieu**, qu'on n'entend que là.
7. **Le test de Kondo** : dix boucles sans lassitude ? Sinon varier (instrumentation, octave, contre-chant), pas
   seulement allonger.

**Un leitmotiv** très simple pour tout le jeu (Larkin : un ou deux thèmes ; Coker : répéter, ressusciter, varier),
décliné par arène avec **son mode, son rythme, son timbre**. Un moment tendu le garde **suspendu** ; une victoire le
**résout**. Un état de la partie peut changer les notes, pas seulement le volume.

## 3. Forme et adaptatif

- Forme A A′ B A″ d'au moins 32 mesures ; une boucle de 16 mesures lasse vite.
- Stems : `ambience` (identité, tonalité), `melody` (avec parcimonie), `tension`, `combat` (énergie). La tension
  ajoute, elle ne remplace pas. Palette de 3 à 5 timbres au plus par morceau ; composer les stems en solo, puis
  ensemble, et vérifier les collisions de fréquences.
- Peu de changements : des déclencheurs aux moments forts, pas à chaque action.

## 4. Grammaire d'un effet sonore

1. **Attaque / corps / queue** : impact immédiat lisible, texture de la matière, décroissance courte si le jeu est rapide.
2. **Matière** : eau, boue, bois, feuille, pierre, sable… La même action sonne différemment sur chaque matière.
3. **Variantes** : au moins 3 pour les gestes fréquents ; pas de transposition extrême.
4. **Télégraphe** : distinctif dans le spectre (aigu contre grave, vérifié au spectrogramme), jamais seul (un signal
   visuel lui répond) ; la préparation ne sonne pas comme l'impact.
5. **Crête par rôle** : impact −1 à −4 dB, interaction −6 à −10 dB.

## 5. Fabriquer dans Sound Forge (pièges appris)

Sound Forge est un outil local de Florent (MCP `sound-forge`) : sur une autre machine, utiliser un autre outil et
garder ces règles.
- **Écrire la partition en script** (Python, scratchpad) puis la passer à `render_music` : durées exactes, harmonie
  vérifiable, motifs transposés (`patterns` + `play` avec `transpose`). Un motif transposé par accord évite la tierce
  (racine, quinte, octave) sauf si toutes les grilles ont le même mode.
- **Vérifier l'harmonie note à note** avant le rendu : chaque note longue contre l'accord du moment.
- **La normalisation est commune à tous les stems** (`peak_db` vise la crête du mélange) : un stem trop fort fait
  baisser les autres. Repères mesurés (RMS) : mélodie ≈ −22 dB, ambiance ≈ −32 dB, combat ≈ −34 dB.
- Les instruments échantillonnés ont des niveaux très différents : toujours mesurer après rendu (`analyze_sound`).
- `render_music` ne se rejoue pas depuis sa recette : renvoyer la partition entière.
- `render_sound` avec 2 à 5 variantes pour un son fréquent ; `compare_sounds` vérifie qu'un effet ne se fait pas
  masquer par la musique.
- Faire écouter : `open_app` puis `open_sound` ; pour la musique, donner le chemin du mix.

## 6. Formats et noms

- Travail : WAV 16 bits ; stems stéréo, effets de proximité mono s'ils sont spatialisés. Jeu : musique et ambiances en
  OGG, effets courts en WAV. Garder un master non compressé.
- `mus_<arene>_<etat>_stem_<nom>_v01.wav`, `sfx_<source>_<action>_<matiere>_<variante>.wav`. Une nouvelle version garde
  son suffixe (`_v02`) et ne remplace l'ancienne qu'après écoute.
- Un manifeste par groupe de stems : BPM, tonalité, durée en échantillons, niveaux, déclencheurs, versions.
