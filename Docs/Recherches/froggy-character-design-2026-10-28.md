# Froggy — recherche de personnage, 28 octobre 2026

**Statut : premier essai Meshy de B produit après validation de la planche et du budget par l’utilisateur.** Les autres pistes restent des propositions. Le design définitif du jeu n’est pas décidé. Aucune classe de personnage ni capacité n’est ajoutée au jeu.

## Références

- Image fournie par l’utilisateur : `Gemini_Generated_Image_z258gyz258gyz258 copy 2.jpg`. Référence de charme, de gourmandise, de volumes ronds et d’expression.
- [Froggy – Présentation du projet](https://app.notion.com/p/4077e94430ba80caac00da616ba0e73c), consultée : grenouille humanoïde, Capture The Flag multijoueur à 2–4 joueurs. Le texte accessible ne fixe pas les proportions du personnage.
- [San Diego Zoo Wildlife Alliance — Frog and Toad](https://animals.sandiegozoo.org/animals/frog-and-toad), fiche lue intégralement. Les yeux saillants, le cou très peu apparent, les membres postérieurs puissants et les différences entre silhouettes élancées et trapues donnent des repères de formes. Aucun chiffre de locomotion n’en est repris.

## Traduction artistique proposée

- Garder des yeux hauts, une bouche large et des appuis de grenouille comme signes principaux de reconnaissance.
- Rendre les cuisses et les pieds visibles dans les propositions destinées à évoquer le saut, plutôt que dessiner un corps humain simplement coloré en vert.
- E explore des membres plus fins avec de grands disques terminaux ; F propose une masse plus trapue, des jambes courtes et quelques reliefs de peau simplifiés. Ce sont des libertés de character design, pas des représentations scientifiques d’espèces précises.
- Pour la recherche de proportions, A vise une tête autour de 40 % de la hauteur et H une hauteur totale proche de deux têtes. **Ce sont des consignes de dessin proposées, pas des mesures biologiques ni des valeurs validées.**
- Une seule pose jouée et un portrait par proposition : choisir d’abord une personnalité et une silhouette ; une planche cohérente de face, profil et dos viendra après sélection.

## Déroulé et limites de recherche

Les deux images ont été produites à partir de la référence fournie ; la lecture zoologique complète a servi à la relecture après génération. Quatre recherches de références ont été tentées via gstack : rainettes et disques digitaux, anatomie des pattes postérieures, silhouette de personnage à la GDC, conception des personnages de Splatoon. Google les a bloquées par un contrôle de trafic. La fiche Smithsonian essayée renvoyait une erreur 404 et l’article Riot sur la lisibilité une erreur de certificat. Ces pages ne sont donc pas des sources vérifiées. Aucune référence de jeu n’a été jointe au générateur ni recopiée.

Le volet artistique repose ici sur l’image de l’utilisateur et les règles de composition du projet ; la comparaison documentée avec des productions de jeux reste à approfondir après sélection. Il n’y a pas de comportement, de sonde Unity ni de valeur de gameplay à tester dans cette livraison de concepts.

## Livrables

Les fichiers lourds et les prompts sont conservés hors dépôt :

`~/.codex/visualizations/2026/10/28/01a182ca-4926-72d1-a272-4445c20e6c96/froggy-character-design/`

- `froggy-planche-01-proche-reference.png` : A Gourmand, B Bondissant, C Canaille, D Costaud.
- `froggy-planche-02-silhouettes.png` : E Voltigeur, F Baroudeur, G Freluquet, H Mini.
- `prompts-et-provenance.md` : prompts complets, références et limites.

Les deux planches ont été affichées et regardées. Les identifiants, corps, pieds et portraits complémentaires sont présents. Les doigts, les proportions entre poses et certains détails de bouche restent à stabiliser avant modélisation. G propose notamment des dents cartoon, à trancher artistiquement. La lisibilité depuis la caméra et l’adéquation du futur modèle devront être jugées en jeu par l’équipe.

Avis proposé : C pour le regard rusé et la personnalité, B pour les proportions et appuis évocateurs du saut, A pour la proximité avec la référence gourmande. L’équipe choisit.

## Suite : premier essai de B

L’utilisateur a retenu B : « Le B est vraiment bien. J'aimerai tester une première génération meshy sur ce modèle. » Les skills `route-3d-meshy` et `production-assets` ont été lus. Une planche face/profil/dos en pose A a été préparée via la génération intégrée de Codex, avec ancre de face corrigée puis vues complémentaires. La bouche a été fermée, les bras ouverts et le dos/profil proposés. L’utilisateur a validé cette planche et un seul essai texturé Meshy 7.1 à 30 crédits par « Oui ».

Dossier de préparation hors dépôt :

`~/.codex/visualizations/2026/10/28/01a182ca-4926-72d1-a272-4445c20e6c96/froggy-b-meshy/`

Essai réalisé : une seule génération multi-vues texturée avec `meshy-7.1`, texture 2K, géométrie standard, remaillage demandé à environ 30 000 faces, formats FBX/GLB produits côté Meshy et seul le FBX téléchargé localement. La planche a été séparée par composantes connexes en trois images carrées sans redessiner ni redimensionner les personnages, face en première entrée. Tâche : `01a182da-ab28-73f9-97c2-28221bb45c78`, état `SUCCEEDED`, **30 crédits consommés**. Solde juste avant l’appel : 2 022 ; après : 1 992. Le solde 1 922 correspondait à la consultation précédente pendant la préparation.

Le FBX contient **31 163 triangles, 15 538 sommets, une couche UV et une texture 2048 × 2048**, sans squelette. Le FBX a été importé et rendu dans une session Blender indépendante du fichier ouvert de l’utilisateur. Face, profil, dos et trois-quarts ont été regardés ; une planche de comparaison référence/rendu est conservée dans `4_controle/`. Les proportions générales, la tête et les bracelets sont reconnaissables. Des irrégularités de surface restent visibles sur le torse, les membres et aux jonctions des doigts. Le rendu paraît légèrement plus pâle selon l’éclairage. Il reste à juger par l’utilisateur et n’a pas été intégré ni testé dans Unity.

Livraison : FBX brut et couleur de base dans `3_meshy/`, scène Blender de contrôle autonome et rendus dans `4_controle/`, archive `froggy-b-premier-essai-meshy.zip`. Détails, paramètres, validations, coût et prompts dans `preparation-et-prompts.md` et `3_meshy/tache.json`. Aucun rig, aucune animation ni seconde génération payante n’ont été lancés.
