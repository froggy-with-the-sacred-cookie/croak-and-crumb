# Sakura : pétales en voltige

29 octobre 2026. Demande : créer des assets de sakura qui virevoltent, puis précision
de Florent : seulement des pétales. Kit indépendant, proposé pour validation visuelle.

## Réel et sources primaires lues

- [NC State Extension, Prunus × yedoensis](https://plants.ces.ncsu.edu/plants/prunus-x-yedoensis/),
  fiche entière consultée : Somei-Yoshino a des fleurs simples rose pâle qui pâlissent
  vers le blanc ; la fiche fournit les photographies de fleurs et les caractéristiques
  botaniques. Traduction : rose doux à la base, bord ivoire, petite encoche et forme ovale.
- [National Park Service, Types of Trees](https://www.nps.gov/subjects/cherryblossom/types-of-trees.htm),
  page entière consultée : distingue les fleurs simples de Yoshino/Akebono des fleurs
  doubles de Kwanzan. Le kit retient des pétales isolés, sans fleur double ni pompon.
- [Unity, Renderer module](https://docs.unity3d.com/6000.0/Documentation/Manual/PartSysRendererModule.html),
  page entière consultée : le mode Mesh rend les particules avec des maillages ; plusieurs
  variantes peuvent être distribuées, et les ombres peuvent être coupées. Documentation
  Unity 6.0 consultée pour ce principe ; pas de nouvelle API de code Unity introduite.

## Recherches et limites

Les recherches web ont porté sur : forme des fleurs Yoshino, aérodynamique de chute,
voltige des pétales et références de vent dans les jeux, puis technique Unity.
Google a imposé un CAPTCHA, DuckDuckGo a répondu 418 et Bing n'a renvoyé que des
résultats sur le premier mot, hors sujet. Aucun chiffre ni procédé artistique n'est
attribué à ces résultats. Aucune mesure primaire fiable de vitesse d'un pétale de sakura
n'a été obtenue pendant cette session.

La recherche existante `jardin-japonais-kit-2026-10-28.md` distingue déjà la mesure
sur la rose des choix ralentis pour le jeu. Ici, aucun résultat sur la rose n'est
présenté comme une mesure sur le sakura. L'aperçu reste une animation artistique,
pas une simulation aérodynamique validée. La référence de jeu n'ayant pas pu être
consultée correctement, aucune affirmation sur sa méthode de rendu n'est retenue.

## Règles de fabrication proposées

| Réglage | Valeur | Statut et raison |
|---|---|---|
| Géométrie | 3 formes, 96 triangles chacune, dos compris | Budget choisi pour de petites particules |
| Longueur du maillage source | environ 1 unité | Normalisation pour l'échelle du ParticleSystem |
| Taille visible | 7,5 à 11,5 cm | Exagération artistique pour la lecture à distance |
| Vitesse de chute moyenne | 0,5 à 0,75 m/s | Choix artistique, non mesuré sur le sakura |
| Démonstration | 48 pétales, 8 s, 24 Hz | Aperçu reproductible ; graine 291026 |
| Rotations | tours entiers et oscillations périodiques, phases différentes | Reprise continue à la fin du clip |
| Entrée/sortie | échelle amortie sur 7 % / 9 % de la vie | Masque la remise en haut hors visibilité |
| Vent en jeu | `VentJardin` via `VentSurParticules` existant | Une seule source de vent |

La chute et la dérive sont précalculées dans le FBX de démonstration. Les maillages
individuels servent à un véritable effet de particules qui suit le vent du jardin.
La boucle autonome n'a ni collision ni adaptation à la météo du jeu.

## Vérifications et verdict restant

Contrôle à la source : 48 instances se déplacent après une seconde et les matrices de
début et fin coïncident à la précision flottante. Contrôle par réimportation FBX dans
un Blender neuf : nombre d'objets, animations, UV, couleurs, matériaux, texture externe,
durée et bouclage. Résultat détaillé dans `Captures/sakura_voltige/Verification_FBX.json`.

Audit de l'existant : `VitrineJardin.RéglerPetales` et `VentSurParticules` produisent
déjà des pétales animés. Ils ne sont pas remplacés. Aucun problème de répétition en jeu
n'est affirmé sans observation.

Unity ne répondait pas à son interface de commande : aucun aperçu en jeu ni mesure
de performance n'a été effectué. L'équipe doit juger couleur, échelle et rythme dans
le jardin avant d'adopter le kit.
