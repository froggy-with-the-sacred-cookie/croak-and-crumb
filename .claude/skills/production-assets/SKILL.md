---
name: production-assets
description: Circuit de production des assets de Froggy avec plusieurs IA (photos réelles, planches Gemini, validation explicite asset par asset, Meshy, Blender, Codex, intégration et capture en jeu), rôles de chaque outil, et règles avant toute génération payante. À charger AVANT de commander, générer, préparer ou intégrer un asset (personnage, arène, objet, cookie, décor, icône, HUD, effet), d'écrire un prompt pour Gemini, GPT ou Codex, ou de lancer un outil payant (Meshy, Higgsfield, API d'images). Aussi quand quelqu'un dit qu'un asset « ne colle pas », parle de « workflow », de « circuit », de « style », de « cohérence » ou de « crédits ».
---

# Production des assets

Ce circuit vient de trois prototypes (Eku, Lovecraft, Glouton, septembre-octobre 2026) où Florent a fait travailler
ensemble Gemini, GPT (Codex), Meshy, Blender et Claude. Chaque règle a coûté des crédits ou des jours.

## 1. Avant toute génération payante

1. **Le gratuit d'abord.** Blender en arrière-plan, code, Pillow, retouche d'un modèle existant, photos libres, et
   Gemini sur l'abonnement déjà payé passent avant les crédits. Une variante (saison, état, couleur) se fait par
   retouche dans Blender, pas par une nouvelle génération (Lovecraft, 8 octobre : les arbres d'hiver, zéro crédit).
   « Je paie déjà 4 outils IA et le jeu ne rapporte rien. »
2. **Vérifier que les références suffisent AVANT de payer.** Un outil qui reconstruit (3D, autre angle, nouvelle pose)
   invente tout ce que la référence ne montre pas. Eku, 18 octobre : une image de trois quarts envoyée à Meshy a donné
   un personnage longiligne à petite tête, hors modèle, 46 crédits perdus. Si une vue manque, la faire d'abord avec
   Gemini et la faire valider.
3. **Annoncer la chaîne entière et son coût total**, étapes obligatoires comprises (Meshy : modèle + remaillage s'il
   dépasse la limite + squelette + chaque animation), et ce qui pourrait la faire échouer. Une seule confirmation pour
   un coût connu. **Un coût plus élevé que prévu = s'arrêter** : ce sera fait à la main.
4. **Validation explicite, asset par asset**, avant Meshy. « Continue » n'est pas une validation : demander (une case
   par asset). Les appels gratuits (solde, listes, statut, téléchargement) restent libres.
5. **Tester petit** : 1 ou 2 assets d'un lot d'abord. Glouton, 26 octobre : ~940 crédits pour 20 objets générés et
   régénérés, « ça suffit ». Accepter un « assez bien » et le corriger en code (retirer un socle) plutôt que régénérer.
6. Les crédits sont ceux de Florent : celui qui paie doit pouvoir dire non en sachant ce qu'il risque.

## 2. Le circuit

| Étape | Qui | Porte |
|---|---|---|
| Photos réelles de référence (Unsplash, Wikimedia Commons, sources gardées) | IA | — |
| Concepts (plusieurs pistes vraiment différentes) | Gemini, sur l'abonnement, prompts écrits par l'IA | l'équipe choisit |
| Planche de modèle (face, profil, dos, pose A) | Gemini | **l'équipe valide**, explicitement |
| Modèle 3D, squelette, animations | Meshy (payant) | coût annoncé et accepté |
| Retouches, variantes, objets simples | Blender par script (gratuit) | rendu de contrôle à côté de la planche |
| Images animées 2D, HUD, icônes, monstres 2D | GPT dans Codex | porte mesurée (skill `animation-gpt`) |
| Intégration, capture en jeu | Claude / Codex dans Unity | **verdict en jeu** par l'équipe |

Rangement qui a marché (Lovecraft, `Atelier3D/`) : un dossier par asset, `1_references/` (photos, modèle actuel),
`2_planches/` (tirages, vues découpées, planche), `3_meshy/` (GLB, textures, `tache.json` avec crédits dépensés et qui a
validé), et un dossier `_validation/` pour les planches de lot. Les images lourdes restent **hors du dépôt** ; les
scripts qui les produisent, dans le dépôt.

**Qui fait quoi** (choix de Florent sur Eku, 20 octobre) : Gemini est le meilleur pour le **design** et les images de
référence ; GPT dans Codex est meilleur pour **reproduire et animer** en 2D, et pour les monstres de bout en bout ;
Meshy « modélise mieux que Claude ou Codex » en 3D ; Claude intègre, mesure et vérifie. Glouton, 27 octobre : **jamais
Meshy pour les images** (texte vers image) : ses crédits sont limités, Gemini est déjà payé.

## 3. Bien utiliser Gemini

- Prompts **en anglais**, en phrases (pas en mots-clés), verbe fort en tête, **rôle de chaque image jointe** (« Image 1
  is the exact identity… Image 2 is the style reference only, not its scenery or light »).
- **Une référence de style par famille** : une image d'une autre famille déteint (Lovecraft : un puits avec des bottes,
  un pommier qui sort d'un puits). L'image de style impose aussi sa silhouette : **écrire les proportions en toutes
  lettres** (« IMPORTANT proportions… »).
- **Jamais une capture d'écran du jeu comme référence de mise en page** : Gemini la recopie (palette, valeurs, objets
  collés avec leur lumière d'origine). Utiliser un plan de masse ou une étude de valeurs (skill `direction-artistique`).
- **Fond magenta pur** pour tout ce qui se détoure, puis détourage local (Pillow : `uv run --with pillow python …`).
  Gemini ajoute parfois un cadre ou des restes (sol, toit fantôme) : les effacer avant la découpe, refaire si ça touche
  le dessin.
- Gemini ignore souvent la grille et la taille demandées : **mesurer la planche** et adapter le découpage.
- **Le personnage d'abord** (Glouton, 27 octobre) : un premier concept symétrique, sans yeux, à bouche en cercle parfait
  « manquait de personnalité ». Les prompts de concept commencent par une bio d'une ligne, les yeux et sourcils, une
  asymétrie, des défauts, des poses jouées, 4 variantes ; la planche neutre pour Meshy vient **après**.
- **Le niveau de détail attendu est riche** (Lovecraft, 9 octobre : « dorénavant tu devras avoir ce niveau de
  détail ») : matières en deux tons, ferrures, végétation, petites touches lumineuses, en gardant des volumes lisibles
  pour Meshy.
- Quand l'API n'a plus de crédit, Florent tire à la main sur son abonnement : lui donner le prompt exact, l'image à
  joindre et où déposer le résultat. **Le guide se donne dans le chat** (une étape par bloc à copier), et s'écrit
  aussi dans un `.md` du dossier de l'asset.

## 4. Fidélité et comparaison

- **Un modèle fait d'après une image lui ressemble vraiment** (silhouette, proportions, pièces). Lovecraft, 6 octobre :
  « prends ton temps pour refaire vraiment le personnage proprement comme sur l'image de base ». Planche de contrôle
  concept | modèle aux mêmes angles à chaque itération, liste d'écarts tenue à jour, ne présenter qu'une fois les écarts
  majeurs résolus.
- **Garder l'ancien et pouvoir basculer** (Lovecraft : « je veux pouvoir faire des aller-retour entre les deux styles »).
  Un nouveau modèle est une nouvelle entrée, l'ancienne reste ; on montre les deux côte à côte.
- **Toujours montrer** : ouvrir la planche (`open <fichier>`). Pour un choix esthétique : plusieurs variantes rendues
  **en jeu** sur une planche, une recommandation, l'équipe choisit.
- **Chaque asset seul est joli, mais assemblé ça ne colle pas** (Eku, 16 octobre). Causes mesurées : chaque asset avec sa
  propre couleur de lumière, contours presque noirs contre des ombres colorées, fond aussi détaillé que le plan de jeu,
  pas d'ombres de contact ni d'atmosphère. Remède : une image-maîtresse par arène (skill `direction-artistique`), une
  lumière neutre venue du dessus dans les textures (sans ombre portée ni halo peint), et l'étalonnage fait par le moteur.

Rien n'est validé tant que l'équipe ne l'a pas dit ; noter qui a validé et quand.
