# Pièges rencontrés (filon de cuivre de Lovecraft, septembre 2026)

Chacun a coûté une ou plusieurs itérations. Les relire avant le premier test en jeu. Ajouter ici ceux de Froggy.

## Blender

- **Couleurs de sommets en sRGB 0-255, sans conversion.** Les calques « octets » de bmesh gardent les valeurs telles
  quelles. Les convertir en linéaire les assombrit deux fois (la roche devient presque noire).
- **Attribut de couleur actif** : sans `color_attributes.active_color_name` et `render_color_index`, l'aperçu reste gris
  (`objet()` le fait).
- **Aperçu avec le contour** : activer `show_backface_culling`, sinon la coque retournée recouvre tout (`rendre()` le fait).
- **Formes** : une enveloppe de points aléatoires donne une galette ; un icosaèdre bruité, une « tête avec des oreilles » ;
  trop de petits triangles, un « grillage » d'arêtes claires. Tailler un ellipsoïde par des plans (`pierre`).
- **Tache sur une grande facette** : une teinte par face colore toute la facette. Réserver les teintes locales aux petites
  faces (creux, pièces subdivisées).
- **Poids** : un ellipsoïde `sub=3` pour chaque petit éclat fait exploser le compte (13 500 faces) : `sub=1`, `fin=False`.
- Un script exécuté dans Blender par le MCP n'a pas de `__file__` : chemins absolus ou variable d'environnement.

## Unity

- **Préfabriqué = variante du FBX** (`PrefabUtility.InstantiatePrefab` puis `SaveAsPrefabAsset`). Un préfabriqué fait
  depuis une copie détachée garde les positions des pièces du premier export : les nouvelles pièces tombent à côté de
  leurs creux.
- **Refaire un préfabriqué casse la référence du catalogue** (l'identifiant de la racine change) : le jeu retombe sur
  l'ancien modèle sans erreur. Réassigner l'entrée.
- **Orientation** : un objet tourné au hasard finit avec ses pièces face au mur. Le code doit tourner le côté des
  `Morceau_*` vers la caméra ; les mettre d'un même côté (−Y dans Blender). En vue à la première personne, le joueur
  tourne autour : prévoir des pièces sur plusieurs côtés ou un objet lisible sous tous les angles.
- **Lumière du lieu** : sous une lumière chaude, le gris devient brun ; hors des lumières, tout bleuit. Un fond de creux
  mal éclairé peut sortir bleu marine.
- **Changement de modèle pendant une animation** : sans attente, l'objet se brisait avant que la dernière pièce parte.
  Attendre la fin des départs avant de passer au modèle épuisé.

## Captures et tests

- Sauvegarde de test avant de lancer, rendue après l'arrêt complet (`isPlayingOrWillChangePlaymode == false`).
- **Le personnage interagit tout seul** avec ce qui est à sa portée pendant les `Step()` : le placer hors de portée et
  déclencher l'interaction par réflexion.
- **`manage_camera` n'écrase pas un fichier existant** (il ajoute `-1`, `-2`) : on croit voir un nouveau rendu et c'est
  l'ancien. Toujours un préfixe neuf.
- **Objet cyan uni** : shader en compilation asynchrone ; `EditorSettings.asyncShaderCompilation = false` le temps des
  captures.
- **Profondeur de champ** : de près, ce qui n'est pas à la hauteur du personnage est flou. Juger aussi la vue de loin.
- **Le MCP de Unity peut se déconnecter** (redémarrage de Unity) : `Tools/unity_mcp.py` parle directement au serveur.
- **Le contrôle de sécurité automatique peut tomber en panne** (plusieurs refus « sans verdict ») : s'arrêter proprement,
  dire où on en est, et reprendre au message suivant.

## Froggy

- **Changer `CONTOUR` après l'`exec` de la lib ne change pas le contour** : la couleur par défaut d'`ajouter_contour`
  est figée à la définition de la fonction. Passer la couleur en argument (`ajouter_contour(bm, ep, TRAIT)`), comme
  `Tools/blender/jardin_japonais.py` (28 octobre 2026).
- **Le MCP `blender` se déclare par dossier** dans la configuration de Claude Code : il existait pour `~/Dev/Froggy` et
  `~/Dev/lovecraft`, pas pour `croak-and-crumb` (28 octobre 2026). Sans lui, `Blender -b -P` fait le même travail.
- **Blender → Unity (FBX de `exporter()`, import avec `bakeAxisConversion`)** : une position (x, y, z) de Blender devient
  (x, z, y) dans Unity, et une rotation θ autour de z devient un lacet de −θ. La racine du FBX garde pourtant **+90° sur
  x** : poser `Quaternion.Euler(0, lacet, 0) * prefab.transform.rotation`, jamais le lacet seul (la vitrine du jardin,
  28 octobre 2026 : tout était couché, sauf le sol, qui n'avait pas été tourné). Un contrôle par rayon sur deux points
  connus du sol a trouvé l'erreur de miroir (`VitrineJardin.Controler`).
- **Le Lit d'URP ignore les couleurs de sommets** : les modèles peints prennent `Froggy/CouleurSommet`
  (`Assets/Froggy/Shaders`). Le feuillage prend `CouleurSommet_Feuillage`, à l'ambiante du dessous plus claire : sans
  lui, le dessous des cerisiers sortait gris mauve.
- **Une collection exclue du calque de vue ne s'exporte pas** (« ne peut pas être sélectionné car il n'est pas dans le
  calque de vue ») : un script qui range ses modèles (`exclude = True`) casse l'export du suivant dans la même session.
  Remettre `layer_collection.children[nom].exclude = False` juste après `collection()` (vitrine agrandie du jardin,
  28 octobre 2026).
- **Reprendre les briques d'un script de kit sans refaire le kit** : `jardin_japonais_lot4.py` exécute
  `jardin_japonais.py` jusqu'au repère « fabrication, export, rendus ». Ce repère ne doit pas changer de texte.
- **Créer un calque bmesh (`faces.layers.int.new`) invalide les faces déjà prises** (« BMFace has been removed ») : créer
  les calques d'abord, puis faire la liste des faces (tour de pierre du jardin, 28 octobre 2026).
- **Le contour trace un joint entre deux modules posés bout à bout** (la coque gonflée referme chaque bout) : sur la
  muraille, un trait sombre tous les 4 m. Supprimer les faces des bouts cachés par le voisin avant `ajouter_contour`
  (`muraille_artisanale.py`, 28 octobre 2026).
- **Beaucoup de petites pièces (tuiles, clous) : pas de contour sur elles.** Le contour double les faces ; le garder pour
  les grandes masses, qui font la silhouette.

### Personnage armé par script (Froggy P, `froggy_p.py`, 28 octobre 2026)

- **Métaballes** : une ellipsoïde de raideur 2 au seuil 0,6 a un demi-axe visible de **0,575 × rayon × taille** (mesuré
  sur Blender 5.2). Poser `radius = 1/0,575` et `size_*` = demi-axes voulus en mètres ; les volumes qui se touchent
  gonflent en se fondant, donc vérifier par la superposition à l'image (rendu orthographique à son échelle exacte).
- **Un décalque posé sur une peau qui plie se fait traverser**, même à 3,5 mm et avec des poids interpolés sur la face
  la plus proche : le ventre se froissait au saut. Tailler la zone dans la peau (arêtes coupées sur la courbe, faces
  intérieures dans un autre matériau). Les décalques ne vont que sur une partie rigide (la tête, pesée à 100 % sur `Head`).
- **Les poids automatiques donnent aux cuisses le bas du ventre** d'un personnage trapu : il se plissait dès que les
  cuisses se repliaient. Rendre au bassin l'influence des jambes au-dessus de l'entrejambe, puis lisser.
- **`vertex_group_smooth` refuse le contexte en `-b`** : lisser les poids à la main (moyenne des voisins par les arêtes,
  numpy, puis normalisation).
- **La coque du contour jette son ombre sur la peau dans Eevee** (taches sombres sur le dessus de la tête) :
  `use_backface_culling_shadow = True` sur son matériau.
