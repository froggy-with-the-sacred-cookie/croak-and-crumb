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
