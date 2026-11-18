# Froggy B — premier lot de rig et animations

Date : 28 octobre 2026. Statut : lot de 21 crédits validé et réalisé ; cinq animations de test livrées. Leur qualité artistique et leur adéquation en jeu restent à juger par l’équipe.

## Modèle et prérequis

Le modèle de départ est la tâche Meshy `01a182da-ab28-73f9-97c2-28221bb45c78` : 31 163 triangles, texturé 2K, bipède en pose A. Le rig Meshy annonce une limite de 300 000 faces et un coût de 5 crédits avec marche/course incluses. Le modèle respecte ces prérequis généraux ; cela ne prouve pas encore que les poids fonctionneront sur ses proportions.

## Lot proposé

| Élément | Coût en crédits |
|---|---:|
| Squelette, marche et course incluses | 5 |
| Repos fait localement dans Blender | 0 |
| Saut sur mesure, génération Prime | 10 |
| Application du saut au modèle | 3 |
| Célébration Victory Cheer, action 59 | 3 |
| **Total du lot proposé** | **21** |

Alternatives présentées à l’utilisateur : 18 sans célébration ; 5 pour le squelette et ses deux déplacements inclus, plus repos local. Solde consulté : 1 967 crédits. Ce sont des crédits supplémentaires aux 30 du modèle statique.

L’utilisateur a choisi explicitement le lot de 21 crédits avec saut Prime et célébration. Le squelette et les déplacements inclus ont été contrôlés avant de générer les autres mouvements. Les quatre tâches payantes ont réussi : rig 5, mouvement Prime 10, application du saut 3, célébration 3. Solde lu avant/après : 1 967 puis 1 946 crédits. Aucun essai payant supplémentaire.

## Références et choix de mouvement

- [San Diego Zoo Wildlife Alliance — Frog and Toad](https://animals.sandiegozoo.org/animals/frog-and-toad), relu intégralement : les membres postérieurs puissants participent à la poussée du saut. C’est le repère biologique retenu ; aucun chiffre de cinématique n’est repris de cette fiche.
- [Meshy — aperçu Victory Cheer](https://cdn.meshy.ai/webapp-assets/feature-demo/animation/preview/biped/Victory_Cheer.gif), gratuit : GIF téléchargé par gstack et poses examinées. Les bras montent et un poing reste levé ; il faudra vérifier les passages près de la grosse tête de Froggy. Ce mannequin ne montre pas encore l’animation sur Froggy.
- Le catalogue Meshy interrogé sur « frog » renvoie zéro animation nommée ainsi. Les recherches « jump », « idle » et « victory » ont fourni des identifiants réels. Les clips marche/course livrés avec le rig sont des mouvements humains de test. Le repos de bibliothèque n’est pas acheté, compte tenu du piège documenté dans le skill.

Quatre recherches web indépendantes ont été tentées via Bing/gstack : extension des membres postérieurs ; réception ; séquence hanche/genou/cheville ; anticipation d’un saut en animation de créature. Les pages n’ont pas livré de résultats exploitables et deux navigations ont échoué. Aucune valeur ni conclusion scientifique n’est inventée à partir de ces recherches. La comparaison avec des références de jeu et l’étude biomécanique chiffrée restent à approfondir pour la locomotion finale.

Le saut proposé est une **chorégraphie de grenouille humanoïde**, avec accroupissement, poussée bilatérale, brève suspension puis réception sur les deux pieds. Il ne prétend pas représenter exactement la réception d’une espèce réelle. Sa durée de 3 secondes est un choix de génération, pas une durée de saut validée pour le gameplay. Le repos sera une animation discrète, avec appuis stables ; la respiration proposée est stylisée.

## Vérifications après validation

Contrôler les poids et les déformations des bras, cuisses, pieds et de la tête ; conserver les textures ; ignorer la sphère d’aide éventuelle. Mesurer et regarder les boucles de marche/course avant de les exporter comme boucles ; vérifier le saut et sa réception. Livrer le FBX armé, les clips, une scène Blender de contrôle et des aperçus animés du vrai modèle. La validation en jeu par l’équipe reste nécessaire avant intégration définitive. Aucun script de gameplay ni réglage de mouvement Unity ne sera changé dans ce lot.

Le plan détaillé, le prompt du saut, l’aperçu de célébration et l’état du budget sont conservés hors dépôt :

`~/.codex/visualizations/2026/10/28/01a182ca-4926-72d1-a272-4445c20e6c96/froggy-b-meshy/5_animations/`

## Résultat et contrôle

Cinq FBX animés avec texture embarquée : Repos, Marche, Course, Saut et Victoire. Chacun conserve un squelette de 24 os et 31 163 triangles. Une scène Blender par mouvement dans `Froggy_B_5_animations.blend`, avec textures empaquetées, permet de les lire. Les FBX gardent les unités d’origine ; la scène de présentation adapte l’échelle pour le cadrage.

Le repos a été réalisé gratuitement dans Blender à partir du rig, bras abaissés et faible mouvement du torse et de la tête, sans animer le bassin ni les jambes. Les raccords début/fin de repos, marche et course ont été mesurés ; les poses correspondent. Durées : repos 4 s, marche 1,033 s, course 0,633 s, saut 2,967 s, victoire 9,333 s, animations sources à 30 images/s. Les aperçus sont échantillonnés autour de 15 images/s.

Les cinq FBX exportés ont été réimportés dans Blender : 24 os, géométrie et texture présentes, durées conservées. L’écart maximal du bassin et des pieds par rapport aux données avant export est inférieur à 0,000002 unité du modèle. Le lecteur FBX a été appelé sans son décalage d’une image pour comparer les mêmes instants.

Les poses et rendus du vrai modèle ont été regardés : tête et texture restent stables. Les animations génériques de marche/course produisent une allure humaine et des plis marqués au ventre et aux hanches ; le saut est une première proposition dont la réception et la compression du torse restent à travailler. La célébration passe ses mains près de la grosse tête, à contrôler dans le jeu. Aucune de ces animations n’est déclarée définitive ni intégrée dans Unity.

Le saut Prime a un repère d’armature différent du rig et des clips de catalogue, malgré des noms d’os identiques : conservation de chaque animation sur son propre FBX et dans sa propre scène Blender. Cette leçon, mesurée sur les matrices de repos, a été ajoutée à la source `.claude/skills/route-3d-meshy/SKILL.md` puis synchronisée vers `.agents/skills/`.

Tâches : rig `01a182e8-cc9c-7290-9e0e-9a9f0d7862aa` ; mouvement `01a182eb-4f9f-73e4-b5e1-230634ea1dd7` ; saut appliqué `01a182ec-a9c9-74e5-ae47-c3bfc480a493` ; victoire `01a182eb-6195-70c4-819f-a4667b239a5e`.

Livraison regroupée dans `Froggy_B_rig_et_5_animations.zip` : modèle armé, cinq FBX animés, scène Blender, vidéos individuelles, aperçu groupé et contrôle de réimportation. L’archive a été relue par ZIP et son intégrité vérifiée. Le GIF groupé est allégé ; une vidéo H.264 de 10 secondes conserve l’aperçu complet.

Les scripts de production reproductibles sont dans `Tools/blender/froggy_b_animations.py`, `froggy_b_apercus.py` et `froggy_b_verifier_exports.py`. Ils consomment le dossier d’asset passé en argument et n’appellent pas Meshy ; ils ne dépensent aucun crédit. Ils ont été exécutés sur les fichiers de ce lot et leur syntaxe vérifiée après copie dans le dépôt.
