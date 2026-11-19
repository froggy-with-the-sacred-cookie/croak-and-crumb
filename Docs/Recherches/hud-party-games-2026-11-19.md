# HUD des party games multijoueurs (19 novembre 2026)

Pourquoi : refonte du HUD de Froggy. Les cinq pistes Gemini du tour 1 (A à E, `Captures/refonte_ui/`) restent
« très grossières » selon Florent. On regarde ce que les jeux du genre ont résolu avant de relancer des tirages.

## Ce qu'on a regardé

Captures en jeu tirées d'Interface In Game, gardées en local pour étude (`Captures/refonte_ui/references_jeux/`,
hors dépôt, jamais jointes à un générateur) : Fall Guys (manche, élimination), Brawl Stars (combat), Overcooked! 2
(cuisine), Rocket League (but), Splatoon 2 (ville), Super Mario Party (dé). Planches :
`Captures/refonte_ui/planche_references_a.jpg` et `_b.jpg`.

Sources lues en entier : la chronique de Sakurai « Rethinking Your Menus » (Famitsu n° 138, 2006, traduction Source
Gaming), Game Developer « 6 examples of UI design… » (2017), Bugnet « How to Design a Readable HUD » (2026),
RocketBrush « What Is a Video Game HUD? », Indieklem « The basics of typography in game interface » (2024), Xbox
Accessibility Guideline 101 (taille du texte). Pas lus (403) : l'article d'un directeur artistique UI sur les erreurs
de HUD (Medium) et le résumé de la conférence de l'UI designer de Splatoon (UI Crunch #13).

## Faits

- **Le HUD tient peu de place.** Part de l'écran couverte, **estimée à l'œil** sur les captures (pas mesurée au
  pixel) : Fall Guys en manche ~2 %, Rocket League ~4 %, Overcooked 2 ~12 % (les commandes sont le jeu), Super Mario
  Party au choix du dé ~14 %. HUD actuel de Froggy ~12 % ; pistes Gemini : A ~13 %, D ~18 %, B et C ~25 %.
- **Les joueurs se reconnaissent à un visage, pas à un nom.** Super Mario Party : une bande de 4 portraits en haut, le
  score à côté de chacun. Brawl Stars : portraits des joueurs, noms et vies au-dessus des personnages.
- **L'information va dans le monde quand elle concerne quelqu'un.** Brawl Stars : nom, trophées et vie au-dessus de
  chaque personnage ; Overcooked : la cuisson au-dessus du poste ; Mario Party : « 17 to ★ » au sol. Game Developer
  cite Dead Space (vie sur la combinaison) pour la même idée.
- **Les événements passent, ils ne restent pas.** Fall Guys : un grand bandeau « ELIMINATED! » penché, puis il part ;
  en manche, seule une étiquette d'objectif dans un coin. Rocket League : « SHOT ON GOAL +10 » flotte au centre puis
  disparaît. Bugnet : l'information de situation « appear when relevant » et disparaît ensuite. RocketBrush range les
  temps de recharge en information secondaire, regroupée ou contextuelle.
- **Une voix typographique propre.** Fall Guys : un lettrage gras et rond, fait maison, en étiquettes penchées ;
  Splatoon : une police dessinée pour le jeu (« Ika font », d'après la page carrières de Nintendo). Sakurai (vidéo
  résumée par GoNintendo, 2023) : changer seulement la police et les couleurs d'un menu change la perception du jeu.
- **Des images plutôt que des mots** (Sakurai, 2006) : « Instead of forcing players to read, it's better to provide
  images and colors », et « Use pictures and letters together » ; but extrême : un menu utilisable dans une langue
  qu'on ne lit pas.
- **Lisibilité** : Xbox XAG 101, texte courant d'au moins **26 px de haut à 1080p sur console** (18 px sur PC), et
  texte agrandissable jusqu'à 200 %. Texte posé sur la scène : un fond sous le texte ou une ombre (Indieklem préfère le
  fond).
- **Formes** : chez Fall Guys et Rocket League, les étiquettes sont penchées ou biseautées, sans cadre épais. Dans nos
  tirages, chaque élément est une boîte cernée d'un gros bord, toutes avec le même poids.

## Règles proposées pour Froggy (propositions, à valider par l'équipe)

1. **Viser 5 à 8 % de l'écran** pour le HUD permanent, sur les bords ; le centre reste libre (valeur tirée des
   références, estimée à l'œil).
2. **Une bande de 4 têtes de Froggy** à la couleur de chaque joueur, avec leur numéro (la couleur n'est jamais seule)
   et leur nombre de cookies, au lieu d'un tableau « Joueur 1 · 1 cookie ». Le minuteur au milieu de la bande.
3. **Dans le monde** : le nombre de cookies porté au-dessus de chaque Froggy ; le porteur du cookie sacré et le gobelin
   marqués sur eux, avec une flèche au bord de l'écran quand ils sont hors champ.
4. **Les annonces passent** : météo et gobelin arrivent en grand bandeau penché au centre (1 à 2 s), puis se rangent
   en petite icône près du minuteur.
5. **Des icônes, pas des phrases** : objet, dash, torgnole en ronds avec jauge circulaire et icône de touche (déjà
   dans `IconesTouches`) ; le nom de l'objet seulement quand on le ramasse.
6. **Une police d'affichage** grasse et ronde, propre au jeu, plus une police de texte ; chiffres à chasse fixe pour
   le minuteur et les scores ; texte courant ≥ 26 px à 1080p.
7. **Des formes qui bougent** : étiquettes penchées, pas de gros cadres ; le poids vient de la couleur et de l'ombre.

## Ouvert

- Le mouvement (apparition, rebond) des HUD de référence n'a pas été vu : les entrées vidéo d'Interface In Game n'ont
  pas été regardées.
- Pas de source primaire chiffrée sur la place du HUD : les pourcentages sont des estimations à l'œil.
- Splatoon 3, Mario Kart World, Party Animals, Stumble Guys et Pikmin 4 manquent à la base d'Interface In Game.

## Comparaison à la même échelle (tour 1 ter)

Coins découpés et ramenés à 1080p (`Captures/refonte_ui/echelle_haut.jpg`, `echelle_bas.jpg`) ; tailles lues à l'œil,
à ±10 % près :
- portrait d'un joueur : Froggy ~150 px de diamètre, Super Mario Party ~90 px ;
- chiffres du minuteur : Froggy ~90 px de haut, Rocket League ~40 px ;
- bouton de capacité : Froggy ~170 px, Brawl Stars ~85 px, alors que Brawl Stars se joue au doigt.
- Dans Rocket League et Super Mario Party, le score est aussi grand que le portrait ; chez nous, le visage est grand
  et le score petit, alors que le score est l'information principale.
- Fall Guys ne met un grand bandeau au centre que quand le joueur ne joue plus (éliminé, fin de manche) ; pendant la
  manche, l'objectif est une petite étiquette penchée dans un coin.
- En jeu, les Froggy n'ont pas la couleur de leur joueur (seules les capsules de test, `CapsuleReseau`) : des
  portraits « tête de Froggy » seraient identiques à part leur fond.
