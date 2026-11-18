# Pétales de sakura en voltige

Proposition du 29 octobre 2026, fabriquée dans Blender sans crédit payant.
Demande précisée par Florent : **uniquement des pétales**. La forme et le mouvement
attendent son avis visuel ; ils ne constituent pas une décision de direction artistique.

## Contenu

- `Petale_Sakura_Ouvert.fbx` : pétale large, peu courbé.
- `Petale_Sakura_Courbe.fbx` : pétale en coupe.
- `Petale_Sakura_Torsade.fbx` : pétale plus étroit et légèrement tordu.
- `Sakura_RoseIvoire.png` : texture partagée 128 × 128, sans lumière ni ombre peinte.
- `Sakura_Voltige_Boucle_8s.fbx` : démonstration précalculée de 48 pétales, boucle de huit secondes à 24 images/s.
- `Sakura_Manifest.json` : poids et vérifications de la source.

Chaque modèle individuel mesure environ une unité de long et possède 96 triangles,
faces arrière comprises. Les normales avant/arrière sont indépendantes. UV et couleurs
de sommets sont fournis. La démonstration utilise 4 608 triangles ; ce chiffre n'est
pas une mesure de performance en jeu.

## Utilisation dans Unity

Les trois modèles individuels sont destinés au mode **Mesh** du Renderer d'un système
de particules. Choisir une taille d'environ 0,075 à 0,115 pour retrouver l'aperçu.
Les tailles et la chute de 0,5 à 0,75 m/s sont des choix de lisibilité artistique.

Le projet possède déjà `VitrineJardin.RéglerPetales`, le matériau `Petale_Cerisier`
et le composant `VentSurParticules`. Pour une intégration, partir d'une **copie** de cet
effet, attribuer les trois variantes au Renderer et garder `VentSurParticules` :
il lit le vent partagé `VentJardin`. Ne pas ajouter une seconde source de vent.

Le shader `Froggy/Petale` existant permet le fondu par l'alpha des particules et
lit leurs couleurs. Pour retrouver le dégradé de la texture sur des objets ordinaires,
utiliser un matériau URP/Lit avec `Sakura_RoseIvoire.png` comme Base Map. Dans les deux
cas, garder la couleur multiplicative du matériau blanche pour éviter une double teinte.
Les maillages possèdent déjà leur dos : un rendu avec élimination des faces arrière suffit.

Le FBX animé est autonome : il sert à examiner la voltige et à placer une décoration
précalculée. Importer son animation en **Generic**, désactiver les caméras/lumières et
activer **Loop Time** sur la plage de huit secondes. Le placer dans un Animator pour
le lire dans la scène. Cette boucle ne réagit pas au vent ni au sol du jeu ; l'effet
en particules est le chemin prévu pour les arènes. Aucun préfabriqué ni aucune scène
existants n'ont été remplacés.

## Vérification et limites

La source contrôle le bouclage et le mouvement des 48 instances. Le FBX final a été
réimporté dans une seconde exécution de Blender pour vérifier les animations, UV,
couleurs, matériaux, texture externe et la reprise de pose. Blender peut décaler
les numéros d'images lors de l'import ; le contrôle utilise les bornes du clip.

Unity n'était pas accessible pendant cette production. L'import Unity, le matériau
URP, les collisions et le résultat dans le jardin restent à vérifier dans l'éditeur
et à juger par un humain en jeu. Aucun script C# n'a été ajouté ou modifié.

Les aperçus, la source `.blend`, l'archive du kit et le rapport de réimportation sont
dans `Captures/sakura_voltige/` (ignoré par git). Le premier essai avec quelques fleurs
est conservé à part dans ce dossier ; il n'est pas livré dans le kit de pétales.

Le générateur est `Tools/blender/sakura_voltige.py`. La recherche et ses sources sont
consignées dans `Docs/Recherches/sakura-voltige-2026-10-29.md`.
