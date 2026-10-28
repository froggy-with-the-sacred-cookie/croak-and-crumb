---
name: son-integration
description: Règles d'intégration du son de Froggy dans Unity (événements nommés, manifeste, variantes, bus et priorités, télégraphes audibles, musique adaptative en stems, baisse pendant les annonces, son en multijoueur, pluie et eau). À charger AVANT d'ajouter ou de modifier un son, un événement audio, un mixer ou un bus, la musique adaptative ou le mixage. Aussi quand quelqu'un dit qu'un son « répète », « couvre » un autre, « manque », que la musique « coupe » ou « pompe ». Pour composer ou fabriquer un son, charger composition-audio.
---

# Son et musique : l'intégration

Règles apprises sur Eku (manifeste d'événements, musique en stems) et Glouton (Sound Forge, fondus de musique), à
reprendre dans un code audio Unity (`AudioMixer`, `AudioSource`).

## Effets sonores

1. **Le gameplay nomme un événement, jamais un fichier** : `Sons.Jouer("froggy.saut")`. Un manifeste (ScriptableObject
   ou JSON) donne variantes, bus, volume, plage de hauteur, intervalle minimal et nombre de voix. Un événement sans son
   reste muet, sans erreur.
2. Un son entre dans le jeu **après écoute validée** ; sa recette (Sound Forge) est gardée à côté.
3. **Jamais deux fois la même variante de suite** ; 3 à 5 variantes pour un son fréquent (pas, sauts, plongeons,
   interface) ; écart de hauteur léger (une transposition forte rend l'action méconnaissable).
4. **Proportionner** : l'intensité suit l'action (la réception porte la hauteur de chute ; le plongeon, la vitesse).
5. **Le télégraphe passe avant tout** : son bus de priorité haute (Eku : bus `Critique`). Priorités d'Eku : 0 dangers
   vitaux, 1 joueurs et adversaires, 2 interactions, 3 ambiances, 4 décoration. Voix limitées **par événement** (au-delà,
   la plus ancienne se tait) ; aucune attaque importante ne se fait voler sa voix.
6. Un son qui doit traverser un **gel d'image** suit le temps réel.
7. **Multijoueur** : un son est joué localement à partir des événements reçus, spatialisé à la position de sa source ;
   les sons de son propre personnage peuvent partir dès l'entrée (prédiction), ceux des autres à la réception.
8. **Pas** : les sons de pas viennent des **appuis de l'animation** (hauteur des os des pieds, au moins 0,2 s entre deux
   appuis, sinon la course a un second creux par pied ; Lovecraft), choisis selon le sol (boue, eau, sable, feuilles,
   mouillé). Vérifier d'abord ce qui existe déjà avant de dire qu'un son manque (Lovecraft, 6 octobre : « j'avais raté
   que les pas existaient »).

## Musique

- **Un morceau = des stems de même longueur** (même BPM, même tonalité, départ à l'échantillon 0, durée exacte en
  échantillons), lus ensemble ; les états (calme, tension, combat ; pour Froggy : début, mène, porte le cookie, dernière
  minute) dosent les stems par fondus, **avec hystérésis** (seuil d'entrée plus haut que le seuil de sortie, temps
  minimal entre deux changements). Glouton avait trois morceaux : menu, partie, et une version pressée pour les 30
  dernières secondes.
- La transition entre états est **quantifiée** (attend le temps ou la mesure) ; une annonce de danger, elle, n'attend
  jamais la mesure.
- Pendant une annonce ou une voix, musique et ambiances baissent (Eku : −9 dB) puis reviennent **exactement** à leur
  niveau (on ne rend que ce qu'on a changé).
- **Stingers** : courtes phrases calées sur la mesure pour les moments forts (cookie pris, capture, victoire) ; elles
  marquent l'événement et cachent les coutures de la boucle.
- Les morceaux qui se fondent l'un dans l'autre ont **exactement la même durée**, sinon la lecture repart au début.
- **Nappes d'ambiance** par arène (un bus à part) : elles tiennent le lien quand la musique s'efface. La **pluie**
  (Notion) a sa nappe, dosée par la même source que le visuel (skill `tech-art`).

## Eau

Sous l'eau : un rendu filtré propre, **sans** passe-bas généralisé qui rendrait les signaux incompréhensibles ; la
sortie d'eau a son propre son. Sur le Web, les effets d'`AudioSource` (passe-bas…) n'existent pas (Lovecraft).

## Avant de dire « c'est fait »

Un test vérifie que chaque événement nommé du code existe dans le manifeste (ou est volontairement muet) et que les
variantes ne se répètent pas. En headless, la musique est suivie sans être entendue : le rendu réel se vérifie à
l'oreille, en jeu, par un humain.
