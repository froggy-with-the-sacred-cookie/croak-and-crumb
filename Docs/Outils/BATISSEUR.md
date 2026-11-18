# Habiller une arène : le Bâtisseur

Menu **Froggy ▸ Bâtisseur d'arène**. La fenêtre s'ouvre en onglet à côté de l'Inspecteur ; tire l'onglet contre le
bord de l'Inspecteur pour voir les deux en même temps. Repris du Bâtisseur de Lovecraft (octobre 2026) et refait pour
des arènes en 3D libre.

Le Bâtisseur fabrique et pose ; **la disposition de l'arène se décide dans l'éditeur, par l'équipe**. Rien de ce qu'il
fait ne change le jeu : il pose des préfabriqués dans la scène ouverte, comme on le ferait à la main.

## Premiers pas (10 minutes)

**Le plus simple : le bouton « Apprendre »** en haut de la fenêtre (ou **Froggy ▸ Bâtisseur ▸ Apprendre : le bac à
sable**). Il ouvre une scène d'essai, jamais enregistrée (un sol gris en damier, un étang, Froggy), et cinq missions qui
se cochent toutes seules : poser les 4 joueurs en symétrie, poser les cookies et une boîte, rendre l'arène équitable,
traverser l'étang en nénuphars, garder une version et jouer. Chaque mission dit comment faire et a son bouton.
**Quitter** rouvre la scène où tu étais.

Ensuite, sur une vraie arène :

1. **Froggy ▸ Bâtisseur d'arène**. En haut : le nom de l'arène, le nombre de joueurs de la partie, et la jauge
   **Prête pour un test** (5 cases : vert c'est fait, rouge il manque quelque chose, gris pas encore mesuré). Juste
   dessous, **Prochaine étape** dit quoi faire et a son bouton.
2. **② Jeu** : clique **● J1**, puis clique dans la vue Scène là où le joueur 1 doit apparaître. Choisis d'abord la
   **Symétrie** « Quart de tour » ou « Deux miroirs » : les joueurs 2 à 4 se posent en même temps. Puis **Cookies** et
   **Boîte**, quelques clics.
3. **③ Équilibre** : le Bâtisseur mesure tout seul les trajets de chaque joueur et écrit ce qu'il en pense (« J3 met
   1,2 s de plus… rapproche un cookie de J3 »), avec **Voir**. Les couleurs sur l'arène disent qui arrive le premier
   où ; la légende est en haut à droite de la vue Scène (un clic sur son titre la replie).
4. Retouche (glisse un cookie, un nénuphar) : la mesure se refait toute seule, la jauge suit.
5. **⑤ Tester** : **Garder cette version**, puis **▶ Jouer ici** (ou F5 la souris sur la vue Scène). Après le test,
   reviens à la version qui marchait le mieux.

Près du curseur, un petit bandeau dit toujours ce que fera le clic (et pourquoi c'est refusé, le cas échéant) ; après
chaque geste, une notification le confirme et rappelle **Ctrl+Z**. La barre « Bâtisseur d'arène » de la vue Scène a un
aide-mémoire de **toutes les touches**. Les réglages rares sont rangés sous **Plus de réglages** ; **Réglages du Bâtisseur ▸ Mode
avancé** les montre tous.

## Les cinq étapes

La fenêtre est rangée par ce qu'on veut faire, dans l'ordre où on le fait. **Maj+1** à **Maj+5** (la souris sur la vue
Scène) ouvrent les étapes ; la barre « Bâtisseur d'arène » de la vue Scène aussi.

| Étape | À quoi elle sert | Ses outils |
|---|---|---|
| **① Terrain** | Le sol, l'eau, les nénuphars, les murs : là où l'on court et où l'on saute. S'ouvre sur le kit de blocs gris. | Poser, Tracer |
| **② Jeu** | Où apparaissent les joueurs, les cookies, les boîtes et le gobelin. | Repères |
| **③ Équilibre** | Est-ce juste pour chacun ? La mesure des trajets et ses conseils. | — |
| **④ Décor** | L'habillage : arbres, lanternes, buissons. | Poser, Semer, Tracer, Tampons |
| **⑤ Tester** | Jouer ici, garder une version, chercher les erreurs. | — |

**Prête pour un test** (les critères de P8-02) : les apparitions de tous les joueurs de la partie, des cookies, tout
accessible, moins de 20 % d'écart entre les joueurs pour le premier cookie, aucune erreur grave.

**Les mêmes gestes partout** :

| Geste | Ce qu'il fait |
|---|---|
| Clic | Poser, semer, ajouter un point. |
| Maj+clic | Retirer l'objet visé (Poser), effacer en semant (Semer), retirer un point (Tracer). |
| I+clic | Prélever : l'objet visé devient celui qu'on pose. |
| A / E | Tourner à gauche / à droite ce qu'on tient (le pas se règle dans Réglages du Bâtisseur : 15°, 30°, 45° ou 90°). |
| T | Un autre tirage (orientation et taille au hasard, variante du tampon, hasard du tracé). |
| M | Tampon en miroir. |
| Entrée | Appliquer le tracé. |
| Échap | Arrêter l'outil (Tracer : efface d'abord la courbe). W, E, R… reprennent aussi les outils d'Unity. |
| Ctrl+Z | Annuler un geste entier : un clic, un coup de pinceau, un tracé, une pose de tampon. |

A et E ne passent devant les touches d'Unity que si un outil du Bâtisseur est pris : sans outil, E reste l'outil de
rotation. La case **A / E tournent aussi la sélection** les fait tourner les objets sélectionnés même sans outil.
Toutes les touches se changent dans **Edit ▸ Shortcuts ▸ Froggy**.

**La symétrie** (en haut des étapes Terrain, Jeu et Décor) : aucune, miroir gauche-droite, miroir avant-arrière, deux miroirs (4 quarts),
demi-tour (2 côtés) ou quart de tour (4 côtés). Poser, Tracer, Tampons et Jeu posent alors aussi les images, en violet
dans l'aperçu ; l'apparition du joueur 1 pose celles des joueurs 2 à 4. **Symétriser la sélection** complète après coup
ce qui est déjà posé. Le centre est le repère **Centre de l'arène** (outil Jeu), sinon l'origine de la scène. Les
images gardent la hauteur de l'original ; Semer ne suit pas la symétrie (sème, puis symétrise la sélection).

**Où va ce qui est posé** : sous le groupe **Ranger sous** (Réglages du Bâtisseur ; vide : un groupe « Décor » créé à
la racine de la scène), puis par outil : `Décor ▸ <dossier du préfabriqué>`, `Décor ▸ Semis ▸ <mélange>`,
`Décor ▸ Tracés ▸ <tracé>`, `Décor ▸ Tampons ▸ <tampon>`. Sélectionner un groupe déplace ou supprime tout un tracé ou
un tampon d'un bloc. Le choix du groupe est retenu par scène.

Tout ce qui est posé garde son lien avec le préfabriqué, avec sa rotation et sa taille d'origine (les modèles venus de
Blender sont tournés de 90° sur X dans leur préfabriqué : le Bâtisseur ajoute sa rotation par-dessus).

## Le kit de blocs gris (① Terrain)

Pour essayer une disposition avant le vrai décor (l'arène grise de P8-02), comme dans les studios. À la première
ouverture de ① Terrain : **Créer le kit de blocs gris** (ou **Froggy ▸ Bâtisseur ▸ Créer le kit de blocs gris**). Il
pose onze pièces dans `Assets/Froggy/Prefabs/Gris`, sans jamais remplacer une pièce existante :

| Pièce | Taille | À quoi elle sert |
|---|---|---|
| Dalles 4×4 et 8×8 | 0,5 m d'épaisseur | le sol |
| Blocs 1×1 et 2×2 | 1 m de haut | obstacles bas, abris |
| Marche | 0,3 m | se monte en marchant |
| Plate-forme de saut (orange) | 1,2 m | demande un saut |
| Rampe | 1 m sur 4 m | monter sans sauter |
| Mur, pilier (foncés) | 2,5 et 3 m | bloquent |
| Nénuphar (vert) | 2 m de large | se saute, s'enfonce sous le poids |
| Eau 8×8 (bleue, sans collision) | — | se nage ; l'analyse la reconnaît à son nom |

Les nénuphars (le gris comme ceux du jardin) **s'enfoncent sous le poids** de Froggy, plongent à l'atterrissage et
penchent vers qui se tient au bord : seul leur dessin bouge, leur collision reste où tu l'as posée, donc la mesure des
sauts ne change pas. Le réglage est commun : `Postes/1_MouvementCamera/ReglagesNenuphar.asset`.

Un damier de **1 m** sur chaque face fait lire les distances d'un coup d'œil. Dans ① Terrain, la palette s'ouvre sur la
catégorie « Gris » et **Aimanter à la grille** est à portée de main. Pour passer au vrai décor : sélectionne les
blocs, prends l'objet du jardin en main, **Retoucher la sélection ▸ Remplacer par**.

## Poser

- **La palette** : les préfabriqués des **Dossiers** (par défaut `Assets/Froggy/Prefabs`, plusieurs séparés par
  « ; »), une puce par sous-dossier (une par kit : JardinJaponais, puis les prochains), une recherche.
- **Glisser une vignette** dans la vue Scène la pose à la façon d'Unity (sans rangement ni réglages).
- **Le fantôme** suit la souris sur ce qui se voit, l'eau comprise. Cadre **vert** : la place est libre ; **rouge** :
  un autre objet est déjà là (on peut poser quand même : en 3D, un rocher qui mord une berge est souvent voulu).
- **Orientation au hasard**, **Pencher comme le sol** (pour un rocher ; un arbre ou une lanterne restent droits),
  **Taille au hasard** (entre deux bornes).
- **Maj+clic** retire l'objet visé (cadre rouge), jamais le joueur ni une caméra. Pour que le sol ou l'eau ne soient
  jamais visés, rends-les non sélectionnables (la petite main dans la Hiérarchie).
- **Aimanter à la grille** (0,25 à 2 m) : la pose tombe sur la grille, au sol sous le point.
- **Retoucher la sélection** (petit tiroir) :
  - **Remplacer par « … »** : chaque objet sélectionné devient l'objet en main, à la même place, même orientation, même
    taille relative, même rang dans la hiérarchie. C'est le passage du bloc gris au vrai décor, en un Ctrl+Z ;
  - **Poser sur le sol** : chaque objet descend (ou monte) jusqu'à toucher ce qui est dessous.

## Semer

- Choisis un **mélange**, un **rayon**, puis glisse dans la vue Scène. Repasser épaissit, jusqu'à l'espacement.
  **Clairsemer** retire une partie de ce que le mélange a semé sous le pinceau, **Effacer** (ou Maj+glisser) tout.
- **On sème sur l'objet visé au début du coup** (le sol, un talus), et seulement lui : il lui faut une collision. Sous
  la couronne d'un arbre, on sème à son pied. Jamais dans un autre objet (lanterne, pont, mur), jamais plus près que
  l'espacement d'un autre objet du même mélange.
- **L'eau n'a pas de collision** : le pinceau passe au travers jusqu'au fond de l'étang. Règle **l'altitude minimale**
  du mélange pour ne pas semer sous l'eau.
- **Le mélange se règle sous le menu**, sans code : ses éléments (préfabriqué, part, penché comme le sol, enfoncement),
  la densité, l'espacement, les massifs (0 : régulier, 1 : groupé en massifs et clairières), la taille, la pente et
  l'altitude permises. **Nouveau**, **Copier**, **Suppr.** ; tout est dans `Assets/Froggy/Batisseur/Semis.asset`.
- Les quatre mélanges de départ viennent du kit du jardin japonais : ce sont des propositions, à régler en jeu.

## Tracer

- **Clique** pour ajouter un point au bout de la courbe ; **clique sur la courbe** pour en insérer un ; **glisse** un
  point pour le déplacer ; **Maj+clic** sur un point le retire ; **Retour arrière** retire le dernier.
- **L'aperçu** montre les pièces à chaque geste. **Entrée** (ou **Tracer**) les pose, en un Ctrl+Z.
- **La courbe reste** : retouche-la ou change un réglage, puis Entrée, et le tracé est refait **à la place** du
  précédent. **Échap** (ou **Nouvelle courbe**) l'efface pour en commencer une autre ; ce qui a été tracé reste.
- **Ce que devient la courbe** (`Assets/Froggy/Batisseur/Traces.asset`, réglable sans code) : les pièces et leur part,
  une ou deux rangées (allée), le pas (ou **le pas de la pièce** : les murs et les clôtures se touchent), l'orientation
  (le long, face à la courbe, au hasard), les écarts au hasard, la taille, la hauteur (**sur le sol**, ou **celle de la
  courbe** : pour poser sur l'eau, clique les points sur l'eau), et **Dégager** (retire ce que le semis a posé sur le
  passage).
- **Autre tirage** change le hasard (écarts, orientations, choix des pièces).
- Sur le sol, une pièce ne se pose que sur ce qui est sous les points cliqués : pas sur la couronne d'un arbre voisin.

## Tampons

- **Poser** : clique un tampon, puis dans la vue Scène. Le fantôme montre exactement ce qui sera posé. A / E le
  tournent, M le retourne, T tire une autre variante ; chaque pose en tire une nouvelle.
- **Variation** (0 à 1) : à 0, posé à l'identique ; plus haut, chaque pièce tourne, grandit et se décale un peu, sauf
  les pièces **fixes** (plus de 4 m, ou cochées dans l'Inspecteur du tampon).
- **Suivre le relief** : chaque pièce se pose sur le sol sous elle, à la hauteur qu'elle avait au-dessus du sien.
- **Enregistrer un tampon** : sélectionne les objets d'un coin réussi (ou leur groupe), donne un nom, **Enregistrer**.
  Un fichier par tampon dans `Assets/Froggy/Batisseur/Tampons`. Seuls les objets posés depuis un préfabriqué y
  entrent, avec leur place, leur rotation et leur taille, **pas leurs retouches dans l'Inspecteur** (pour qu'elles
  comptent, en faire une variante de préfabriqué).
- **Remplacer « … » par la sélection** : pose le tampon, retouche-le, sélectionne son groupe, remplace.
- Clic droit sur un tampon : l'afficher dans le Project, ou le mettre à la corbeille (ce qui a été posé reste).
- **Varier la sélection** : les objets restent en place mais tournent et changent un peu de taille, pour casser
  l'effet copier-coller d'une rangée.

## ② Jeu et ③ Équilibre : les repères et l'équité

**Les repères** (rangés sous « Repères de jeu »). Ils se voient même quand les gizmos sont coupés dans la vue Scène : **Joueur** (son numéro, 1 à 4), **Cookies**, **Boîte** à objets,
**Gobelin** (départ du cookie sacré), **Centre** de l'arène. Clic pour poser, A / E pour tourner (le joueur regarde
dans ce sens), Maj+clic pour retirer. **Sert dans les parties** : une arène à 4 se joue aussi à 2 et à 3, un repère
peut ne servir qu'à certaines. Chaque joueur a sa couleur **et** sa forme (rond, triangle, carré, losange). La fenêtre
dit, pour 2, 3 et 4 joueurs, s'il manque des apparitions.

**Les erreurs sont prévenues à la pose** : une apparition de joueur est **refusée** dans l'eau, sur une pente trop
raide, là où un objet gêne Froggy ou au-dessus du vide (le fantôme passe au rouge, le bandeau dit pourquoi). Un cookie,
une boîte ou le gobelin peuvent y être posés quand même, avec un avertissement.

Le composant `RepereArene` (`Assets/Froggy/Scripts/Arene/`) ne fait rien en jeu : c'est une **proposition d'interface**
pour les règles (P3-01 cookies, P3-04 réapparition, les boîtes, le gobelin), à valider par leurs responsables.

**Trajets et équité** (tiroir de l'outil Jeu) : **Analyser** mesure l'arène autour des repères (où Froggy tient debout,
où il nage, où il ne passe pas) et calcule le temps de chaque apparition vers chaque point, en courant, en nageant et en
sautant. Choisis la **partie** (2, 3 ou 4 joueurs) et la **météo** (la pluie accélère, la neige ralentit).

- **Les conseils** (« Ce que le Bâtisseur en pense ») : ce qui est hors d'atteinte, les écarts entre joueurs au-delà
  de 10 % (orange) ou 20 % (rouge) avec quoi déplacer, les sauts justes ou ratés de peu ; chacun avec **Voir**.
- **Le tableau** (Détails chiffrés) : pour chaque joueur, le cookie le plus proche, la moyenne vers tous les cookies, la boîte la plus
  proche, le départ du gobelin, et le nombre de cookies où il arrive le premier ; dessous, **l'écart** entre les
  joueurs, en vert sous 10 %, orange sous 20 %, rouge au-delà. Un repère qu'un joueur ne peut pas atteindre est
  signalé, avec **Voir**. C'est le critère de P8-02 : « chaque point d'apparition est à distance équitable des
  cookies ».
- **Le calque** dans la vue Scène, outil pris ou non :
  - **Territoires** : chaque case prend la couleur du joueur qui y arrive le premier ; blanc là où deux joueurs arrivent
    à moins de 0,5 s l'un de l'autre (les zones disputées) ; gris où personne n'arrive ;
  - **Temps depuis un joueur** : du vert (tout près) au rouge (loin) ;
  - **Sauts seulement** : tous les sauts d'une surface à une autre (vert : à l'aise ; orange : serré, moins de 0,5 m
    de marge ; rouge : raté de peu, jusqu'à 0,75 m trop loin). Sur les autres calques, seuls l'orange et le rouge
    restent, avec leur marge. Un saut serré qu'on peut contourner par un troisième nénuphar n'est pas signalé.
- **Suivre les retouches** (coché) : l'analyse se refait une demi-seconde après chaque changement de la scène. On
  glisse un nénuphar, le saut passe du rouge au vert. Une analyse prend de 20 à 150 ms sur la vitrine du jardin.
- **Le gabarit de Froggy** (`Assets/Froggy/Batisseur/Gabarit.asset`) : taille, marche, pente, course, sprint, nage, saut,
  effet de la météo, finesse de l'analyse. **Tout est provisoire** tant que le contrôleur (P1-01) et la nage (P1-02) ne
  sont pas faits ; **Reprendre du contrôleur** relit la course et le saut du contrôleur actuel. La nage (3 m/s) et
  l'effet de la pluie et de la neige (× 1,25 et × 0,75) sont des hypothèses, à remplacer par les vrais chiffres.
- **Ce que l'analyse ne voit pas** : elle court en ligne droite d'une case à l'autre, sans accélération ; un saut part à
  pleine vitesse et rien ne le gêne en vol ; l'eau se reconnaît au nom de son objet (« Eau », réglable), car elle n'a
  pas de collision ; on ne cherche pas le sol plus de 3 m au-dessus des repères (sinon Froggy « marcherait » sur la
  couronne des arbres). C'est fait pour **comparer** deux apparitions ou deux dispositions, pas pour chronométrer au
  dixième : le ressenti se juge en jeu.

## Versions de l'arène

Le tiroir **Versions de l'arène**, en bas de la fenêtre : **Garder cette version** range une copie éteinte du décor
(le groupe « Ranger sous ») et des repères de jeu dans la scène, sous « Versions de l'arène » (marqué `EditorOnly` :
rien n'entre dans un build). **Y revenir** garde d'abord la disposition du moment, puis remet celle-ci : on bascule de
l'une à l'autre sans rien perdre, pour comparer en test. Les copies gardent leur lien aux préfabriqués. Le sol et l'eau
posés hors du groupe ne font pas partie de la version.

## Vérifier l'arène

Le tiroir **Vérifier l'arène**, en bas de la fenêtre, passe la scène ouverte en revue :

- en rouge : un préfabriqué qui n'existe plus, un script manquant, un matériau vide, le joueur sans sol sous lui ;
- un objet posé deux fois au même endroit (un copier-coller oublié) ;
- un objet qui flotte au-dessus du sol (au-delà du seuil réglable), ou sans rien dessous. Ceux qui flottent exprès
  (nénuphars, eau…) se mettent dans **Ne pas chercher s'ils flottent** ;
- le bilan de ce qui coûte au rendu : objets, rendus, triangles (de près), collisions, lumières en temps réel.

**Voir** sélectionne l'objet et y mène la vue Scène. Les règles propres au Capture The Flag (bases, cookies,
apparitions à égale distance) viendront quand ces objets existeront.

## Jouer ici (F5)

- **F5** (menu **Froggy ▸ Jouer ici**), la souris sur la vue Scène : la scène se lance avec Froggy posé **sous la
  souris**, tourné comme la vue Scène. Sans la souris sur la vue : au centre de la vue. **F5** en jeu arrête.
- Le panneau **Froggy : jouer** de la vue Scène (menu ⋮ de la vue Scène ▸ Overlays) : **Jouer ici**, et après une
  partie **Où j'étais en jeu**, qui mène la vue Scène là où tu t'es arrêté.
- Le joueur est le premier objet qui porte `DeplacementWow` ou `TemplatePlayerController` (sinon le tag `Player`).
  Quand le réseau fera apparaître les joueurs, ce point d'arrivée devra passer par l'apparition.

## Pour le code

- Tout est dans `Assets/Froggy/Editor/Batisseur/` (namespace `Froggy.Outils`) : la fenêtre `BatisseurArene` et ses
  `partial` (Etapes : les étapes, la jauge, le bandeau, les notifications, la légende ; Poser, Semer, Tracer, Tampons,
  Jeu, Symetrie, Versions), `Fantome` (l'aperçu dessiné sans rien créer),
  `RaccourcisBatisseur` (raccourcis à contexte), `JouerIci`, `VerificationArene`, les réglages `SemisArene`,
  `TracesArene`, `TamponArene`, `GabaritFroggy`, l'analyse `TrajetsArene` (grille, sauts, plus courts chemins) et le
  calcul sans éditeur `CourbeArene` (courbe, symétrie, doublons). Le repère de jeu `RepereArene` est dans
  `Assets/Froggy/Scripts/Arene/`. Tests : `Editor/Tests/BatisseurAreneTests.cs`.
- **Le Mono de Unity calcule les `float` en double** puis les arrondit en les rangeant : un plus court chemin en
  `float` tournait plus de 15 minutes sur 6 000 cases (2 ms en .NET). Les temps se calculent en `double`, et une
  amélioration doit dépasser 1 µs.
- Ce qui coûte à chaque image a son cache : l'aperçu d'un tampon (refait seulement si sa place ou ses réglages
  changent), la sélection sous la souris pour Maj+clic (refaite si la souris bouge de plus de 3 points), la recherche
  du groupe « Ranger sous ». Les doublons se cherchent par cases (5 000 objets : 1,5 s en comparant toutes les paires).
- Chaque geste ouvre son groupe d'annulation (`Undo.IncrementCurrentGroup`) ; un coup de pinceau ou un tracé se
  referme par `Undo.CollapseUndoOperations`.
- Une touche lue par `KeyDown` dans la vue Scène arrive **après** les raccourcis d'Unity : E y prenait l'outil de
  rotation (vécu sur Lovecraft). D'où les raccourcis à contexte de `RaccourcisBatisseur`.
- Toute nouvelle fonction de construction d'arène va dans le Bâtisseur, pas dans une fenêtre de plus, à l'étape qui
  correspond à ce qu'elle permet de faire ; ses réglages rares sous `Plus(clé)`. Chaque geste confirme par
  `Notifier(...)` et dit comment l'annuler ; un état vide dit quoi faire.
