---
name: route-3d-meshy
description: Route 3D de Froggy (planche multi-vues Gemini, Meshy pour le modèle, le squelette et les animations, Blender pour les retouches, import dans Unity en humanoïde ou en objet), avec ses coûts, réglages et pièges. À charger AVANT tout appel Meshy (image_to_3d, multi_image_to_3d, remesh, rig, animate, text_to_motion), avant d'importer un FBX ou un GLB Meshy dans Unity, de régler un avatar humanoïde, ou quand on parle de 3D, de modèle, de squelette, de rig, d'animer la grenouille ou un personnage. Charger aussi production-assets.
---

# Route 3D : Meshy → Blender → Unity

Pilotes d'Eku (18-19 octobre 2026), de Lovecraft (arbres, maisons, personnage au gilet, 7-26 octobre) et de Glouton
(20 objets, créatures, 26-27 octobre). Lire la section 1 avant de dépenser quoi que ce soit, et le skill
`production-assets` (validation, coûts).

## 1. Les références (la cause des échecs)

- **Une image de trois quarts ne suffit pas** : Meshy invente le dos, le profil, les proportions. Il faut une planche
  **de face, de profil et de dos** (4 vues pour un objet), mêmes proportions, fond uni, sans perspective, puis
  `meshy_multi_image_to_3d` avec ces vues (élément 0 = la face). Eku, deuxième essai : la planche multi-vues a réglé les
  proportions du premier coup.
- **Meshy ne rend pas plus riche que l'image qu'on lui donne.** Froggy, 18 novembre 2026 : la lanterne *kasuga* du kit
  (script Blender), photographiée sous 4 angles sans contour (`Tools/blender/vues_pour_meshy.py`), puis meshy-7.1 +
  texture meshy-7, 30 crédits : la même lanterne, sans contour, texture d'un gris presque uniforme (`remove_lighting`
  efface aussi la lumière peinte sur les facettes), arêtes un peu fondues, un panneau de la boîte à lumière perdu. Moins
  bien que l'original. Pour « plus beau », enrichir d'abord l'image (concept Gemini), puis la planche, puis Meshy.
- **Gemini dessine mal trois vues cohérentes d'un seul jet** (profil tourné du mauvais côté, queue absente de dos,
  bouche ouverte de face et fermée de profil). Méthode qui corrige : garder la meilleure vue comme **ancre** dans une
  **planche-guide** (l'ancre à gauche, deux cases vides, des lignes horizontales aux repères : haut de tête, yeux,
  menton, épaules, bassin, sol) et demander de **compléter**. Une vue donnée « à garder » est recopiée telle quelle,
  défauts compris : pour la corriger, demander explicitement de la REDESSINER.
- **Pose A** (bras à ~30°), franche, **rien qui pende** (une cape a cassé le rig automatique sur Lovecraft) ; la même
  pose des bras dans toutes les vues (Meshy suppose une seule pose). Contours fins, pas noirs (ils se cuisent dans la
  texture).
- **Découper une planche** : jamais en tiers fixes (un membre déborde dans la case voisine) ; suivre les composantes
  connexes, rattacher les petites à la figure la plus proche, combler les trous, recadrer en carré.
- **Froggy n'est pas humain** : le squelette automatique de Meshy est **humanoïde** et sa bibliothèque d'animations fait
  des **gestes humains**. Pour une démarche ou un saut de grenouille, `meshy_text_to_motion` (décrire la posture :
  accroupi, cuisses repliées, poussée des pattes arrière) avant `meshy_animate`. Une grenouille *humanoïde* (Notion)
  passe mieux par le rig humanoïde, mais ses proportions (grosse tête, jambes repliées) se vérifient au rendu.
- **Un mouvement humain ne se voit pas sur un corps chibi.** Froggy P, 18 novembre 2026 : les 11 clips
  `text_to_motion`, rejoués sur P par le retargeting de Unity, bougeaient le corps d'un bloc et presque pas les bras
  (23 cm, sous une tête de 84 cm de large qui les cache vus de dos) : « le modèle semble ne pas être riggé ». Il
  l'était. Pour un personnage à grosse tête et petits membres, animer à la main par script Blender
  (`Tools/blender/froggy_p_animations.py` : le corps et la tête portent le geste, le visage l'émotion), et juger sur
  une planche à trois vues dont **la vue de dos de la caméra**, pas seulement de face.

## 2. Coûts et chaîne (à annoncer en une fois)

Coûts payés en octobre 2026, à revérifier avant de les annoncer (`meshy_check_balance` est gratuit) :

| Étape | Coût | Piège |
|---|---|---|
| `multi_image_to_3d` / `image_to_3d` texturé | ~30 | passer **`should_remesh: true` et un `target_polycount`** à la création, sinon le modèle dépasse la limite du squelette et il faut un remaillage à part |
| Arbre, objet en pièces (`image_to_3d`, topologie intelligente, ~1 500 à 3 000 faces) | ~15 | — |
| `meshy_remesh` | 5 | évitable (ligne du dessus) |
| `meshy_rig` | 5 | marche et course offertes ; humanoïde seulement |
| `meshy_animate` | 3 par action | `meshy_list_animations` est gratuit et donne un GIF d'aperçu : regarder avant d'acheter |
| `meshy_text_to_motion` | 3 ou 10 | « prime » (10) a donné une vraie démarche propre au personnage |

`remove_lighting: true` (texture sans lumière peinte), `image_enhancement: false`, `pose_mode: "t-pose"` ou `"a-pose"`
pour un modèle à armer. Budget de faces : ~6 000 pour un objet, ~30 000 pour un personnage.

## 3. Ce que livre Meshy (pièges)

- **Avec le MCP de Meshy, un squelette (`meshy_rig`) et une animation (`meshy_animate`) ne se téléchargent pas** : l'outil
  ne rend que des liens signés, qui arrivent masqués à l'IA (Froggy, 18 novembre 2026 : squelette 5 + torgnole de la
  bibliothèque 3 crédits, fichiers inaccessibles ; liens valables 3 jours). Les modèles (`image_to_3d`,
  `multi_image_to_3d`) et les mouvements `text_to_motion` en qualité prime, eux, s'enregistrent sur le disque. Pour
  animer : `text_to_motion` (10 crédits), puis `Tools/blender/mouvement_meshy_pour_unity.py`.
- **Le mouvement `text_to_motion` prime arrive sur un squelette SMPL-H** (52 os : Pelvis, L_Hip, L_Collar,
  L_Shoulder…) avec un mannequin de 16 Mo. Unity l'importe directement en humanoïde, avec la bonne correspondance
  (L_Collar = épaule, L_Shoulder = haut du bras), et le rejoue sur Froggy B, kawaii et P (vérifié le 18/11/2026 par des
  rendus de profil). Le script retire le mannequin et exporte la plage exacte de l'action : un clip de 2,5 s sortait de
  Blender à 8,3 s. Le mouvement avance le corps : en jeu, jouer sur place et couper au besoin (l'atterrissage commence
  par le saut, le contact au sol arrive vers 0,8 s).
- Un modèle Meshy de plus de quelques semaines peut ne plus être retrouvé par l'API (le squelette de Froggy B, fait
  par Codex le 28 octobre, était introuvable le 18 novembre) : garder les GLB et FBX d'origine à l'abri, hors du dépôt.

- Le GLB armé porte une **sphère d'aide** (« Icosphere », sans parent) : à exclure des mesures et du rendu.
- Le squelette est à l'**échelle 0,01** : une épaisseur donnée dans l'espace de l'objet se divise par l'échelle.
- Meshy ajoute parfois un **socle** sous le modèle : le retirer par script (Glouton : `trim_base.py`), garder l'original.
- Le repos « Idle » de la bibliothèque peut être inutilisable (Eku : bassin qui pivote de −80° à +9°, tête qui regarde
  ailleurs). Pour un repos : `text_to_motion` (« breathing gently, looking forward ») ou fait à la main.
- **Boucle qui boite** : un clip de 2 s contenait 2,4 foulées et 8 poses tirées sur tout le clip. Toujours mesurer la
  période d'une foulée (écart avant/arrière des os `LeftFoot` / `RightFoot`) et couper sur exactement une foulée.
- Les exports anciens « …_withSkin » avaient des os figés dans une image de l'animation ; l'export riggé récent
  (`…_Meshy_Merged_Animations.fbx`) a un squelette Mixamo **juste** (Lovecraft, 26 octobre, vérifié par un rendu
  Blender de la marche et de la course). Vérifier par un rendu avant d'intégrer.
- **Les courbes Prime ne se copient pas aveuglément sur le rig du catalogue.** Froggy B, 28 octobre 2026 : le FBX
  du saut issu de `text_to_motion` puis `animate` a la même liste d'os que marche/course/victoire, mais l'armature est
  tournée de 90° sur X et ses os au repos sont exprimés dans un autre repère. La marche et la course, elles,
  partagent exactement les positions de repos du rig. Garder les FBX animés complets sur leurs squelettes propres,
  ou retargeter explicitement en tenant compte des matrices de repos ; partager des noms d'os ne suffit pas pour
  partager une action. Contrôler les matrices, puis les vrais rendus, avant un assemblage ou une intégration Unity.
- **Un mouvement `text_to_motion` se réapplique à un autre rig pour 3 crédits** (`meshy_animate` avec son
  `motion_task_id`, dans les 3 jours) au lieu de 13 pour un mouvement neuf. Froggy kawaii, 28 octobre 2026 : le saut
  de B réutilisé ; les deux modèles ont ainsi le même saut, ce qui rend la comparaison juste.
- **Yeux bavés** : quand le profil de la planche montre l'œil, Meshy mélange l'œil de face et celui de profil, et le
  noir déborde sur les côtés de la tête (Froggy kawaii, 28 octobre 2026 ; parfait de face, taches de trois quarts).
  Réparation gratuite, `Tools/blender/froggy_kawaii_corriger.py` : situer chaque texel en 3D, repeindre les yeux par
  diffusion de la peau voisine, poser de vrais yeux (ellipsoïdes mesurés sur les texels vus de face). Plutôt que de
  régénérer la texture (10 crédits, sans garantie). **Ne pas accrocher ces yeux à l'os `Head`** : Meshy a pesé la grosse
  tête du kawaii surtout sur le cou, et dès qu'un clip tourne la tête (les `text_to_motion`), les yeux partaient seuls
  (18 novembre 2026). Les yeux prennent les poids de la peau juste en dessous : `Tools/blender/froggy_kawaii_yeux_peau.py`
  (contrôle : l'écart œil-peau reste de 2,0 cm tête tournée de 25°). Après remplacement des FBX, vider
  `humanDescription` (§ 4), sinon plus d'avatar ni de clip.

## 4. Dans Unity

- **Un personnage animé seulement par ses propres clips s'importe en `Generic`** (Froggy, 18 novembre 2026, le kappa :
  modèle Meshy, squelette et clips faits par script Blender). En humanoïde, Unity écrasait ses postures tenues (penché
  en avant, cookie serré, bras croisés) : le clip rejouait presque la pose A ; en générique, il rend exactement Blender
  (vérifié de face et de profil). L'humanoïde ne sert que pour partager des clips entre squelettes (les Froggy).
- **Personnage** : type humanoïde, avatar créé depuis le modèle avec la correspondance automatique. Si on remplace le
  FBX, **vider `humanDescription`**, sinon l'avatar n'est plus humain. **Ne pas forcer la pose T** dans
  `humanDescription.skeleton` : la configuration automatique le fait (Lovecraft : l'écraser a penché le corps de 29°).
  Pour juger une pose, comparer les muscles (`HumanPoseHandler`) à un personnage de référence au même instant.
- Remplacer un FBX en gardant son GUID garde toutes les références. Clips de marche et de course en boucle, racine
  figée dans la pose s'ils sont sur place ; régler la vitesse de lecture sur la foulée **mesurée à la taille du jeu**
  (pied au sol), pas à l'œil.
- **Un clip par FBX, chacun avec son propre avatar humanoïde** (Froggy, 28 octobre 2026) : le retargeting humanoïde
  absorbe le repère différent du saut Prime (ligne du § 3). Réglages posés par `Assets/Froggy/Editor/ImportPersonnagesFroggy.cs`
  pour tout `Assets/Froggy/Personnages/`, contrôleurs et scène de test par le menu *Froggy > Personnages* : y ranger un
  nouveau modèle (`<Dossier>/Froggy_X_<Clip>.fbx`) suffit pour le comparer en jeu.
- **Texture embarquée dans le FBX = modèle blanc dans Unity** tant qu'elle n'est pas extraite
  (`ModelImporter.ExtractTextures`, puis réimport) ; `PersonnagesFroggy.Construire` le fait. Ne pas ranger un
  personnage sous `Assets/Froggy/Models/` : `ImportModelesFroggy` y coupe les animations.
- `bakeAxisConversion` comme les exports de Blender ; matériaux copiés d'un matériau du jeu puis remappés, pour garder
  le shader du projet.
- **Poids des textures** (Glouton) : GLB converti en glTF avec textures externes réduites à 1024 px, compressées par
  Unity : build de 399 à 99 Mo (`unpack_glb.py`). Originaux gardés à part.
- Glouton : un script Blender d'ajustement (`fit_meshy_creature.py`) ajoutait aux créatures Meshy les nœuds dont le
  jeu a besoin (pose, bouche, mâchoire), avec un contrat de noms de pièces écrit en tête de script.

## 5. Le pont Meshy ▸ Blender

L'extension officielle de Meshy pour Blender importe « Envoyer à Blender » dans le fichier ouvert. Sur Lovecraft, elle a
été retouchée : serveur sur `127.0.0.1` seulement (l'original écoute sur toutes les interfaces) et minuteur persistant
(l'opérateur d'origine s'arrêtait à l'ouverture d'un fichier). Une mise à jour de l'extension efface ces retouches.

## 6. Ce que vaut la route

Verdict d'Eku (jeu 2D) : la 3D rendue en sprites a perdu trop de détails face à l'illustration. Pour Froggy, jeu 3D,
c'est la route principale ; ses gains mesurés restent : volumes et visage identiques sous tous les angles, une nouvelle
animation pour quelques crédits, aucune ombre peinte. Ses limites : formes fines perdues à la reconstruction, texture
souvent trop claire (la corriger plutôt que régénérer).
