---
name: recherche-reference
description: Recherche approfondie OBLIGATOIRE avant tout ce qui doit paraître vrai ou bien fait dans Froggy — la grenouille (saut, nage, posture, langue), la faune et la flore des arènes (marécage, jungle, désert, oasis), l'eau, la pluie, la lumière, les matières, et ce que les bons jeux du genre ont déjà résolu. Impose de chercher dans le réel (biologie, physique, avec des chiffres) ET chez les jeux et artistes qui l'ont bien fait, de lire au moins une source primaire, d'en tirer des règles mesurables, puis de vérifier par une sonde que le jeu les respecte. À charger AVANT d'écrire ou de modifier une animation ou un comportement vivant, un effet d'eau ou de pluie, un décor qui bouge, un prompt d'asset d'environnement. Aussi quand quelqu'un dit qu'un élément « ne fait pas naturel », « fait mécanique », « ne bouge pas comme en vrai », ou demande de « se renseigner ».
---

# Recherche avant de faire

Eku, 26 octobre 2026 : la nage des méduses n'est devenue juste qu'une fois appuyée sur les études (flottabilité
quasi neutre, deux poussées par battement, virage pendant la contraction, rythmes qui alternent). Avant, elle avait été
inventée : « trop soumises à la gravité ». Après : « ta recherche semble avoir bien aidé ». **On ne devine pas le
vivant : on l'apprend, puis on le traduit.** Le même principe vaut pour l'art (Eku, 19 octobre : des prompts sans
recherche sur la composition ont donné des images « trop IA »).

## 1. Quand

Toute demande qui touche à ce qui doit paraître vrai : la grenouille elle-même (Froggy est une grenouille humanoïde :
ses sauts, sa nage, sa langue gagnent à partir du vrai), une créature de fond, une plante, l'eau, la pluie, la boue, le
sable, la lumière. Aussi une mécanique de jeu dont le genre a des réponses connues (Capture The Flag, arènes à 2-4
joueurs). Une demande purement technique (un bogue) n'en a pas besoin.

## 2. La recherche, en trois volets

1. **Le réel** (biologie, physique) : comment la chose bouge, pousse, réagit, avec **des chiffres** (rythme, vitesse,
   durées relatives, amplitudes, proportions). Sources préférées : articles scientifiques (Journal of Experimental
   Biology pour la locomotion des grenouilles, par exemple), muséums, universités. Une vidéo de référence filmée se
   cite si elle est trouvée.
2. **L'art** : comment les jeux et animateurs qui l'ont réussi l'ont rendu (conférences GDC, articles, making-of).
   Ce que le réel donne, l'art le simplifie : chercher **ce qu'ils ont gardé** et **ce qu'ils ont exagéré**.
3. **La technique dans Unity** : la façon la plus simple de rendre ce qu'on a appris (animation, shader, particules,
   physique) et son coût (skill `tech-art`).

Règles :
- **Au moins quatre recherches**, lancées ensemble quand elles sont indépendantes.
- **Lire au moins une source primaire en entier** : un résumé de moteur de recherche ne suffit pas pour un chiffre.
- **Ne jamais inventer un fait ni un chiffre.** Ce qui n'a pas été trouvé se dit ; une hypothèse se marque comme telle.
- **Des faits aux règles** : chaque fait retenu devient une règle du jeu, avec sa valeur et son unité de jeu, et ce
  qu'on simplifie volontairement.
- **Pour l'environnement, regarder les jeux avant de composer** (Eku, 22 octobre : un placement « carrément aléatoire »
  s'est réglé en étudiant des captures de jeux de référence). Les captures de référence restent en local, hors dépôt,
  pour étudier, jamais pour les joindre à un générateur.

## 3. Consigner

- Une **fiche de recherche** par sujet : `Docs/Recherches/<sujet>.md` (faits et sources, ce que l'art en fait, règles
  retenues avec leurs valeurs, ce qui reste ouvert).
- Le commentaire d'en-tête du script dit les faits qu'il applique, en une phrase chacun.
- Le compte rendu **liste les sources** en liens.

## 4. Vérifier

Une **sonde** (scratchpad) mesure dans le jeu les règles tirées de la recherche (vitesses, rythmes, durées) et les
compare aux valeurs visées. Un comportement nouveau reçoit aussi sa vérification dans un test. Le dernier mot est l'œil
d'un humain, en jeu.

## 5. Avec les autres skills

Ce skill vient **avant** `ressenti-mouvement`, `rendu-vivant`, `direction-artistique`, `production-assets` et
`tech-art` : il fournit les faits, eux disent comment les rendre dans Froggy.
