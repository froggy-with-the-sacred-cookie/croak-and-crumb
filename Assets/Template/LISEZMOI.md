# Scène Template

Point de départ et référence pour tester une mécanique. Ce n'est **pas** le contrôleur de Froggy : c'est un cube qui
court, saute et regarde autour de lui, pour que chacun parte d'une base qui marche.

## Utiliser

1. Dupliquer `Scenes/Template.unity` (Ctrl+D) dans son propre dossier, ne pas modifier l'original.
2. Glisser `Prefabs/PlayerTemplate.prefab` dans n'importe quelle scène : il apporte sa caméra, rien d'autre à ajouter
   (retirer la Main Camera de la scène si elle en a une).
3. Le décor vient du pack **Grid Master** (`Assets/Assets/Grid Master/URP/Prefabs`) : sol `GM-Base`, marches
   `GM-Cube` de 0,5 / 1 / 1,5 / 2 m, rampe `GM-Ramp`, escalier `GM-Stairs`.

## Commandes

Elles viennent de `Assets/InputSystem_Actions.inputactions`, carte « Player » (PlayerInput sur le prefab).

| Action | Clavier / souris | Manette |
|---|---|---|
| Move | ZQSD / WASD, flèches | Stick gauche |
| Look | Souris | Stick droit |
| Jump | Espace | A / Croix |
| Sprint | Maj gauche | Clic stick gauche |

Échap libère le curseur, un clic le reprend.

## Le prefab

- `PlayerTemplate` : Rigidbody (interpolé, rotation gelée), BoxCollider sans friction, `PlayerInput`,
  `TemplatePlayerController`.
  - `Visual` : le cube vert, tourné vers la course (la bande sombre marque l'avant). Remplacer ici le modèle.
  - `CameraRig` : `TemplateCameraRig`, caméra orbitale (seule à déplacer la caméra).
    - `PlayerCamera` : Camera + AudioListener.

Les valeurs de mouvement sont dans `Settings/TemplateMovementSettings.asset` (vitesse, sprint, hauteur de saut,
coyote time, tampon de saut, saut variable). Pour essayer d'autres valeurs, dupliquer l'asset plutôt que de changer
celui-ci. Sensibilités de la caméra et inversion de l'axe Y : sur `CameraRig`.

## Ce qui manque volontairement

Pas de réseau (FishNet), pas de collision de caméra avec les murs, pas de bascule 1re / 3e personne, pas de nage.
