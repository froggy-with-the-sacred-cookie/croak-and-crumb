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

## 4. Dans Unity

- **Personnage** : type humanoïde, avatar créé depuis le modèle avec la correspondance automatique. Si on remplace le
  FBX, **vider `humanDescription`**, sinon l'avatar n'est plus humain. **Ne pas forcer la pose T** dans
  `humanDescription.skeleton` : la configuration automatique le fait (Lovecraft : l'écraser a penché le corps de 29°).
  Pour juger une pose, comparer les muscles (`HumanPoseHandler`) à un personnage de référence au même instant.
- Remplacer un FBX en gardant son GUID garde toutes les références. Clips de marche et de course en boucle, racine
  figée dans la pose s'ils sont sur place ; régler la vitesse de lecture sur la foulée **mesurée à la taille du jeu**
  (pied au sol), pas à l'œil.
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
