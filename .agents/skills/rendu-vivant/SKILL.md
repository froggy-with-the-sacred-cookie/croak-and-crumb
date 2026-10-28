---
name: rendu-vivant
description: Règles pour que tout ce qui s'affiche en mouvement dans Froggy paraisse vivant et organique, jamais mécanique ni répétitif (ancrage sur la cause, contrepoint, variation bornée, désynchronisation, rythme non linéaire, lisibilité avant variété). À charger AVANT d'écrire ou de modifier un effet visuel (impact, éclaboussure, poussière, gouttes, aura, alerte), une particule, une animation de décor (roseaux, nénuphars, eau), un retour d'action, même pour un « petit » ajustement de position. Aussi quand quelqu'un dit qu'un effet « apparaît toujours au même endroit », « fait répétitif », « fait mécanique » ou « manque de vie ».
---

# Rendu vivant

Le but : un monde qui **respire et répond**. Un effet qui apparaît deux fois au même endroit, au même instant de son
cycle, avec la même image, se lit comme une machine. Le cas d'origine (Eku) : l'éclat d'impact tombait toujours au
même point de la cible, par-dessus l'arc de la lame.

## Les règles

1. **Ancrer sur la cause, pas sur un point fixe.** Un effet naît là où quelque chose se passe vraiment : la zone de
   contact entre deux corps, le pied (la patte) qui touche le sol ou l'eau, le bord de l'onde. Jamais « au centre du
   corps » par défaut.
2. **Les effets d'un même geste ne se superposent pas.** Chacun a sa place : la trace devant, l'éclat sur la cible, la
   gerbe au sol. Mieux : ils se répondent **en contrepoint** (un effet en haut appelle sa réponse en bas).
3. **Chaque temps d'une séquence a sa place** : l'étape N ne ressemble pas à l'étape N−1 (position, variante, ampleur).
4. **Suivre le mouvement** : l'effet prend l'orientation du geste et peut hériter d'une part de sa vitesse.
5. **Proportionner** : chute plus haute, gerbe plus large ; geste fort, effet plus ample (le son suit pareil).
6. **Varier dans des bornes étroites** : petit écart de position, rotation, échelle ou départ ; plusieurs variantes
   d'image, **jamais deux fois la même de suite**. L'effet reste reconnaissable : on varie la place et le grain, pas la
   silhouette ni la couleur de lecture.
7. **Désynchroniser les répétitions** : plusieurs roseaux, nénuphars ou plantes ne battent jamais en phase (départ et
   vitesse décalés par instance, tirés d'un hachage **stable** de leur position, pas de `GetHashCode` d'une chaîne).
8. **Le rythme n'est jamais linéaire** : entrées et sorties avec accélération et amorti ; un effet qui disparaît
   s'efface ou se disperse, il ne s'éteint pas d'un coup.
9. **Ce qui bouge au vent lit une seule source de vent** (skill `tech-art`, la météo), jamais une direction en dur.

## Ce qui ne bouge pas

- **Un effet qui informe reste sur ce dont il parle** : l'éclat d'impact varie À L'INTÉRIEUR du corps touché, jamais à
  côté (Eku : sorti derrière la cible, « il ne disait plus : ici, c'est touché »).
- **La lisibilité passe avant la variété** : un télégraphe, un repère de jeu (cookie, base, zone dangereuse) ou une
  information du HUD reste **constant**. On peut le faire respirer, pas le déplacer ni changer sa forme.
- **Seul le dessin bouge** : zones de frappe, portées, captures ne se déduisent jamais d'un effet visuel. En
  multijoueur, les effets sont locaux et cosmétiques ; ils ne pilotent aucune règle.
- L'aléa passe par un générateur propre au script (graine réglable), pour qu'un test puisse le rendre déterministe.
- **Ce qui luit ne se peint pas en blanc** : la couleur reste moyenne dans la texture, et l'émission (HDR, bloom) la
  pousse au-delà ; une lueur peinte plafonne avant même que la scène l'éclaire.

## Avant de dire « c'est fait »

1. **Audit** : chercher les autres apparitions du même effet et signaler celles qui restent figées, sans les corriger
   d'office si personne ne l'a demandé.
2. **Sonde** (scratchpad) : déclencher l'effet plusieurs fois, aux différentes étapes et des deux côtés, imprimer
   positions et variantes, vérifier qu'elles diffèrent, restent dans leurs bornes et suivent le geste.
3. **Compte rendu** : les emplacements obtenus (tableau par étape), le réglage qui les commande, et la validation finale
   à l'œil d'un humain en jeu.
