# Jardin japonais : premiers éléments du kit 3D (28 octobre 2026)

**Statut : premiers jets proposés, pas une direction artistique.** La direction artistique du jardin (tâche P8-01,
illustrations validées par l'équipe) n'existe pas encore. Ces modèles servent à bloquer l'arène et à discuter ; ils
seront refaits ou retouchés quand une image-maîtresse sera choisie. Script : `Tools/blender/jardin_japonais.py`.

V1 (page Notion « V1 ») : « jardin japonais dans lequel il y a beaucoup de nénuphars », pluie tropicale et neige,
une arène pour 2 à 4 joueurs, 3e personne.

## Faits retenus et sources

| Élément | Fait | Source |
|---|---|---|
| Lanterne *yukimi-dōrō* | Repose sur des pieds, « usually a four-legged base but can have from 3 to 6 legs » ; boîte à lumière « generally six-sided » ; toit large, de 3 à 8 côtés ou rond ; de 0,5 m à plus de 3 m ; placée « at the edge of land and water » (à trois pieds : un sur la terre, deux dans l'eau). Le nom renvoie à la contemplation de la neige. | [NAJGA, Yukimi doro](https://najga.org/yukimi-doro-japanese-lantern) (lue en entier) |
| Pont *taiko-bashi* | Courbé « very steeply like the back of a taiko drum » ; l'arche et son reflet forment un cercle ; sans marches, il est trop raide pour être franchi ; rouge vermillon dans les sanctuaires, souvent bois naturel dans les jardins ; point focal d'un jardin. | [japanesegardens.jp](https://japanesegardens.jp/taiko-bashi-and-their-use-in-japanese-shrines-and-temples/) (lue en entier), [Wikipedia, Moon bridge](https://en.wikipedia.org/wiki/Moon_bridge) |
| Pierres de gué *tobi-ishi* | 30 à 62 cm de long, 9 à 15 cm d'épaisseur, environ 55 cm entre centres (fournisseurs, pas une norme). | [buildajapanesegarden.com](https://buildajapanesegarden.com/japanese-garden-materials/japanese-stepping-stones/tobi-ishi-stepping-stones) |
| Nénuphar (*Nymphaea*) | Feuille ronde, échancrure en V étroite et ouverte, profonde d'environ 1/3 du diamètre ; 20 à 28 cm de diamètre selon les variétés. | [Brevet « Purple Fantasy »](https://patents.justia.com/patent/PP26530), [Wetland Plants](https://wetland-plants.co.uk/shop/water-lilies/pink-sensation/) |
| Pin taillé (*niwaki*) | Taille en « nuages » ; pins à aiguilles courtes, dont *Pinus thunbergii* (pin noir). | [RHS, Cloud pruning](https://www.rhs.org.uk/advice/profile?PID=540) |
| Lisibilité | La lisibilité d'abord (échelle, silhouette, couleur, exagération), testée dès les formes grises. | [80.lv, Matt McDaid](https://80.lv/articles/matt-mcdaid-mastering-the-stylized-art/) |

Non trouvé : proportions chiffrées d'une lanterne (rapport pied / toit), dimensions d'un taiko-bashi, construction
d'une clôture *yotsume-gaki* (faite de mémoire, à vérifier), forme des étages d'un pin noir taillé.

## Ce que le jeu en fait (propositions)

- **Échelle** : Froggy mesure environ **1 m** (hypothèse : aucune taille n'est fixée). Les objets du jardin sont à
  l'échelle humaine réelle, sauf ce qui porte le jeu.
- **Nénuphars exagérés** : réels 20-28 cm, ici **1,2 m, 1,8 m et 2,6 m** de diamètre, pour qu'une grenouille d'1 m y
  atterrisse. L'échancrure garde la forme du vrai (V étroit) mais seulement **1/4 du diamètre** de profondeur au lieu
  d'1/3, pour ne pas manger la surface d'atterrissage. Dessus plat, bord à peine relevé.
- **Pierres de gué** : 0,7 à 0,9 m, dessus **plat** (on y atterrit), 0,14 m au-dessus du sol.
- **Lanterne** : 4 pieds, boîte à six côtés, toit hexagonal large aux coins relevés, ~1,3 m. Le toit large reçoit la
  neige : à faire par le shader de neige (skill `tech-art`), pas par un modèle à part.
- **Pont** : portée 4,4 m, flèche 0,8 m, largeur 1,1 m, **marches plates** (sans elles il ne se franchit pas). Pente
  aux extrémités ≈ 40° : à vérifier avec le contrôleur (skill `ressenti-mouvement`). **Deux variantes** : rouge
  vermillon (sanctuaire) et bois naturel (jardin), parce que le rouge pourrait être pris par une équipe ou par un
  objet de jeu (règle de lisibilité multijoueur du skill `direction-artistique`).
- **Couleurs** : peintes dans les sommets (méthode du skill `modele-3d-blender`), lumière douce en haut à gauche,
  contour vert sombre (celui des recherches de personnage) au lieu du brun de Lovecraft. Une gamme chaud / froid :
  verts et gris bleutés, accents chauds (érable rouge, papier de la lanterne, pont).

## Retour de Florent (28 octobre 2026, soir)

- **Pont rouge retenu** (« tellement plus de personnalité »). La variante en bois reste dans le kit (`Pont_Taiko_Bois`),
  sans être posée dans la vitrine.
- **Style validé** pour ces assets : « presque déjà parfait ».
- **Érable en couleurs d'automne** gardé ; **cerisiers en fleurs** demandés.

## Deuxième lot

| Élément | Fait | Source |
|---|---|---|
| Cerisier (*Prunus × yedoensis* 'Somei-Yoshino') | Petit arbre étalé, plus large que haut ; fleurs nombreuses, rose pâle qui pâlit vers le blanc, avant les feuilles ; écorce lisse gris rougeâtre à grandes lenticelles horizontales. | [Virginia Tech Dendrology](https://dendro.cnre.vt.edu/DENDROLOGY/carddetail.cfm?ID=312), [NC State Extension](https://plants.ces.ncsu.edu/plants/prunus-x-yedoensis/common-name/yoshino-cherry/) |
| *Tsukubai* | Bassin (*chōzubachi*), pierre de devant (*mae-ishi*), pierre à bougie et pierre à seau de part et d'autre (*teshoku-ishi*, *yuoke-ishi*, côtés interchangeables), louche (*hishaku*), souvent une arrivée d'eau en bambou ; une lanterne derrière. | [NAJGA, Tsukubai](https://najga.org/tsukubai-design-construction/), [japanesegardens.jp](https://japanesegardens.jp/chozubachi-hachimae-tsukubai-water-basins/) |
| *Shishi-odoshi* | Tube de bambou à pivot décentré : l'eau le remplit, il bascule, se vide et retombe sur une pierre (le claquement). | [Université de Toronto, démo de physique](https://ugdemos.physics.utoronto.ca/db/demos/shishi-odoshi-a-non-simple-non-harmonic-oscillator) |
| Iris d'eau (*Iris ensata*) | 60 à 90 cm au jardin ; fleurs violettes à tache jaune ; au bord de l'eau, 0 à 15 cm de profondeur. | [Waitrose Garden](https://www.waitrosegarden.com/plants/_/iris-ensata/classid.2000038470), [Ballyrobert](https://www.ballyrobertgardens.com/products/iris-ensata) |
| Gravier ratissé (*samon*) | Lignes droites (le calme), cercles concentriques autour des pierres (*mizumon*, l'eau troublée), vagues… | [NAJGA, 7 motifs de ratissage](https://najga.org/7-common-raking-patterns-in-japanese-dry-landscape-gardens/) (lue en entier) |

Non trouvé : l'écartement des sillons (pris à 25 cm), la longueur d'un *shishi-odoshi* (pris à 90 cm). La triade de
pierres dressées du gravier est faite de mémoire.

## La vitrine (`Tools/blender/vitrine_jardin.py`, scène `Assets/Froggy/Scenes/Vitrine_JardinJaponais.unity`)

**Une scène pour juger le kit en jeu, pas l'arène** : la disposition de l'arène se décide dans l'éditeur par l'équipe.
Terrain de 36 × 28 m : un étang en deux bassins, avec un pont sur l'étranglement, 24 nénuphars et 7 fleurs. Une butte
de 1,1 m porte un cerisier. Deux sentiers de pierres de gué mènent au pont, puis au *tsukubai*. Un sentier de terre
mène au gravier ratissé. La lanterne et les iris sont au bord de l'eau. Une clôture borde le nord, avec des bambous
derrière.

## Troisième lot : la vitrine prend vie (28 octobre 2026, fin d'après-midi)

Demande de Florent : animer le *shishi-odoshi*, faire tomber les pétales, faire bouger les nénuphars, une vraie eau,
un ciel et un soleil, du post-traitement URP. Tout est **visuel et local** : rien ne pilote une règle, rien n'est
synchronisé en réseau.

| Sujet | Fait | Ce que le jeu en fait |
|---|---|---|
| Rythme du *shishi-odoshi* | Intervalle réel de 1 min à 4 min environ, fixé par le débit et le volume du tube : intervalle × débit = volume ([Wikipedia](https://en.wikipedia.org/wiki/Shishi-odoshi), [Université de Kyoto](https://repository.kulib.kyoto-u.ac.jp/dspace/handle/2433/292947), [vidéo de Taizō-in](https://vimeo.com/311374391)) | **Resserré à 12-18 s** pour qu'on le voie, jamais deux fois la même durée de suite. Le tube frémit, bascule en accélérant, se vide, retombe et claque avec un petit rebond. L'événement `claquement` attend son son. |
| Chute des pétales | Un article d'étudiants ([Leicester, 2021](https://journals.le.ac.uk/index.php/pst/article/view/3916), lu en entier) trouve 5,35 cm/s. Mais son calcul prend la densité d'un pétale pour celle de l'air. Le tableau qu'il cite (pétales de rose mesurés, Yilmaz et Ekinci 2011) donne **1,25 à 1,57 m/s**. | **0,5 à 0,8 m/s**, ralenti pour qu'on les voie voltiger. Ils culbutent sur trois axes, le vent les pousse, ils se posent puis s'effacent. Ils font 6 à 9 cm, contre 1,5 cm en vrai, pour se lire de loin. Huit pétales par seconde et par arbre. |
| Nénuphars | La tige les retient : ils oscillent sur place sans dériver. Les tiges de surface travaillent en traction par vent ou par vagues ([PubMed 21636476](https://pubmed.ncbi.nlm.nih.gov/21636476)). | Houle de ±1,2 cm, lacet de ±3° autour de la tige, inclinaison vers le vent. Chaque nénuphar a sa phase. Seul le dessin bouge : la collision reste fixe. |
| Eau stylisée | Recette « Ghibli / animation » de la Tech Art Bible (volume 1) ; leçons de Lovecraft dans le skill `tech-art`. Les assets du commerce font de même : dégradé selon la profondeur, écume d'intersection ([Stylized Water 2](https://marketplace.unity.com/packages/vfx/shaders/stylized-water-1-71207)). | `Froggy/Eau` : couleur par profondeur (Beer-Lambert), réfraction, profondeur lissée sur 5 points, écume de rive en bandes, motif de Voronoï déformé, reflet du ciel, éclats du soleil. |

**Le vent a une seule source**, `VentJardin`, qui pose `_FroggyVent` : l'eau, les nénuphars, les nuages et les pétales
la lisent. Le soleil est bas (22°), au sud-ouest, en fin d'après-midi. Le ciel `Froggy/Ciel` peint un dégradé, un disque
de soleil assez lumineux pour le bloom, et des nuages à deux tons poussés par le vent.

Le post-traitement passe par un volume global :

- tonemapping Neutral ;
- bloom (seuil 1) ;
- contraste et saturation légers ;
- balance des blancs un peu chaude ;
- ombres à peine bleutées ;
- vignette discrète.

Pas de flou de mouvement ni de profondeur de champ (skills `accessibilite` et `unity-mcp`).

**L'ancien rendu reste disponible** : décocher `Rendu > RenduJardin > nouveauRendu` dans la scène.

## Quatrième lot et vitrine agrandie (28 octobre 2026, soir)

Demande de Florent : agrandir l'arène et continuer les assets. Script : `Tools/blender/jardin_japonais_lot4.py`, avec la
porte ajoutée à `muraille_jardin.py`. Fait dans le Blender ouvert, par l'extension blender-mcp (port 9876).

| Élément | Fait | Source |
|---|---|---|
| *Yatsuhashi* | Pont ancien en bois, surtout au-dessus des massifs d'iris : de larges planches posées une à une en zigzag, sur de courts pieux plantés dans la vase, pour « flâner au-dessus des plantes d'eau ». Le nom veut dire « huit ponts », mais il y en a de plus ou moins de planches. | [Conder, *Landscape Gardening in Japan* (1912)](https://www.gardenvisit.com/book/landscape_gardening_in_japan_1912/chapter_8_garden_bridges/zigzag_bridges_yatsuhashi) (lue en entier), [MLIT, Kōraku-en](https://www.mlit.go.jp/tagengo-db/en/R2-01679.html) |
| *Kasuga-dōrō* | Haute et mince, sur un pied ; souvent près d'une porte de sanctuaire. Parties, de haut en bas : *hōju*, *ukebana*, *kasa* (six ou huit côtés, coins relevés en *warabite*), *hibukuro* (six ou quatre côtés, gravé de cerfs, du soleil ou de la lune), *chūdai*, *sao* (rond ou carré), *kiso*. | [Wikipedia, Tōrō](https://en.wikipedia.org/wiki/T%C5%8Dr%C5%8D) (lue en entier) ; hauteur : un antiquaire annonce 213 cm ([1stdibs](https://www.1stdibs.com/furniture/building-garden/garden-ornaments/japan-antique-kasuga-deer-granite-stone-lantern-84-inches/id-f_39218482)), pas une norme |
| *Azumaya* | Abri de chaume sans murs, ouvert sur les quatre côtés, plan carré sur quatre poteaux, toit en pyramide ; on s'y assoit et on s'y repose. Les poteaux du *shia* (même famille) sont posés sur des pierres ; un garde-corps ferme trois côtés et une partie du quatrième. | [JAANUS, azumaya](https://aisf.or.jp/~jaanus/deta/a/azumaya.htm) (lue en entier), [JAANUS, shia](https://aisf.or.jp/~jaanus/deta/s/shia.htm) |
| *Karikomi* | Arbustes taillés, le plus souvent des rhododendrons (azalées *satsuki*) : le sommet est arrondi, mais les flancs descendent droit jusqu'au sol, jamais une boule parfaite. L'*o-karikomi* est une seule masse faite de plusieurs arbustes. Taille après la floraison. | [japanesegardens.jp, karikomi](https://japanesegardens.jp/pruning-shrubs-in-japanese-gardens-karikomi/) (lue en entier), [Atlanta Botanical Garden, satsuki](https://atlantabg.org/?p=536) |
| Porte *munamon* | Deux piliers qui portent un toit à deux pentes. Les vantaux de planches à bord droit, qui s'ouvrent vers l'intérieur, sont décrits pour le *kabukimon*. | [Wikipedia, Mon (architecture)](https://en.wikipedia.org/wiki/Mon_(architecture)), [JAANUS, kabukimon](https://aisf.or.jp/~jaanus/deta/k/kabukimon.htm) (lue en entier) |

Non trouvé, donc pris par hypothèse :
- la largeur et la hauteur des planches du *yatsuhashi* : 0,8 m de large (deux planches), dessus 22 cm au-dessus de l'eau ;
- les proportions de la *kasuga* et le nœud de son pied, faits de mémoire ;
- les dimensions de l'*azumaya* : 2,5 m entre poteaux, faîte à 4,35 m, pente du chaume de 45° environ ;
- les bancs de l'*azumaya* ;
- la hauteur du toit de la porte et ses ferrures.

Les cerfs du *hibukuro* ne sont pas gravés : deux panneaux pleins les remplacent. Le soleil et la lune sont des découpes
de papier éclairé.

Ce que le jeu en fait (propositions) :
- **Yatsuhashi** : un module d'une planche (`Yatsuhashi_Planche`, 2,6 m), posé bout à bout. Le pont entier se compose
  dans l'éditeur. Le passage étroit au-dessus de l'eau sert la règle de Notion (terrain étroit, sauts précis).
- **Kasuga** : 2,2 m, plus haute que Froggy (≈ 1 m). Elle sert de repère vertical, que l'on voit au-dessus des
  arbustes.
- **Karikomi** : quatre variantes (petit, moyen, fleuri, o-karikomi). En fleur, la satsuki se couvre presque entière :
  le fleuri est rose et magenta, avec un peu de vert. Le feuillage prend le matériau `CouleurSommet_Feuillage`.
- **Porte** : un module de 4 m, qui remplace un module du mur au milieu du côté sud. Les vantaux sont fermés : la
  vitrine finit là.

**La vitrine agrandie** est une scène à part (`Vitrine_JardinJaponais_Grande.unity`). Elle fait **52 × 40 m** au lieu
de 36 × 28 m, soit 2,1 fois la surface. Ce sont des multiples des 4 m de la muraille. Le jardin d'origine ne bouge pas,
et on y entre maintenant par la porte. Autour de lui, on a ajouté :
- à l'ouest, un marais aux iris (0 à 30 cm d'eau), traversé par un *yatsuhashi* de huit planches ;
- à l'est, un plateau de 45 cm qui porte l'*azumaya* ;
- des *karikomi* le long des murs ;
- des lanternes *kasuga* à la porte, aux deux bouts du *yatsuhashi* et devant l'*azumaya* ;
- des sentiers de pierres de gué, qui partent tous d'un sentier existant.

La vitrine d'origine reste telle quelle, pour comparer. La disposition reste une proposition à remanier dans l'éditeur.
Pour la refaire :
- dans Blender : le lot 4, puis `VITRINE=grande` sur `vitrine_jardin.py` et sur `muraille_jardin.py` ;
- dans Unity : menu *Froggy > Vitrine du jardin japonais > Vitrine agrandie (52 × 40 m) : tout construire*, ou
  `ToutFaire -grande` en ligne de commande.

Captures : `Captures/vitrine_grande/`. Planche : `Captures/modeles/jardin/planche_4.png`.

## Cinquième lot (28 octobre 2026, nuit)

Script : `Tools/blender/jardin_japonais_lot5.py`. Les éléments sont posés seulement dans la vitrine agrandie.

| Élément | Fait | Source |
|---|---|---|
| *Ishibashi* | D'une seule dalle de granit à des ponts très construits. Les dalles taillées des grands jardins (Katsura) dépassent 2 m de long et 20 cm d'épaisseur. Les jardins plus tardifs emploient des dalles simples, souvent naturelles, seules ou en deux ou trois travées. | [JAANUS, kiri-ishibashi](https://aisf.or.jp/~jaanus/deta/k/kiriishibashi.htm), [Nara, Japanese Garden Dictionary](https://www.nabunken.go.jp/org/bunka/jgd/pages/StoneBridge.html), [Bowdoin, bridges](https://learn.bowdoin.edu/japanesegardens/elements/bridges/bridges.html) |
| Tour de pierre (*sekitō*) | En général un nombre impair d'étages, de 3 à 11 ; 5, 7 et 9 sont les plus courants. Les pierres sont posées sans mortier (le fleuron tombe aux séismes). Le *gorintō* empile, de bas en haut, un carré (terre), une sphère (eau), une pyramide (feu), une demi-lune (vent) et un joyau (vide). | [NAJGA, towers and ornaments](https://najga.org/japanese-gardens-towers-and-ornaments/) (lue en entier), [JAANUS, gorintou](https://aisf.or.jp/~jaanus/deta/g/gorintou.htm) |
| *Kenninji-gaki* | Clôture pleine de bambou fraîchement coupé, fendu en quatre, posé en double (l'écorce verte dehors des deux côtés). Deux ou trois barres de demi-bambou le tiennent, liées de cordes teintes ; un gros bambou sert de faîtage. JAANUS compte quatre ou cinq barres et des poteaux à intervalles. | [Conder 1912, Kenninji fencing](https://www.gardenvisit.com/book/landscape_gardening_in_japan_1912/chapter_6_garden_enclosures/bamboo_fences_kenninji_fencing) (lue en entier), [JAANUS, kenninjigaki](https://projects.mcah.columbia.edu/jaanus/node/3130) |
| *Sode-gaki* | Courte clôture d'ornement, près d'une véranda ou d'un bassin, attachée d'un côté à un mur ou à un poteau. Elle fait de 3 à 4 pieds de large sur 5 à 7 pieds de haut (JAANUS : environ 1 m sur 2 m). Rectangulaire, le haut souvent arrondi d'un côté. Éléments debout tenus par des lattes de bambou, bordure roulée épaisse. | [Conder 1912, sleeve fences](https://www.gardenvisit.com/book/landscape_gardening_in_japan_1912/chapter_6_garden_enclosures/sleeve_fences_sode_gaki) (lue en entier), [JAANUS, sodegaki](https://aisf.or.jp/~jaanus/deta/s/sodegaki.htm) |
| *Giboshi* (hosta) | Une vingtaine d'espèces au Japon ; plante d'ombre partielle à totale, en sol frais. Compagnons cités : fougère peinte du Japon, herbe de Hakone. | [RHS, Hosta plantaginea var. japonica](https://www.rhs.org.uk/plants/58129/hosta-plantaginea-var-japonica/details), [Gardenia, Hosta gracillima](https://www.gardenia.net/plant/hosta-gracillima-plantain-lily) |

Non trouvé, donc pris par hypothèse :
- la hauteur d'une *kenninji-gaki* : prise à 1,8 m, pour que Froggy s'y cache ;
- les proportions de la tour (~2,6 m) et sa flèche à anneaux ;
- la forme des feuilles de *giboshi* et des fougères, de mémoire.

Choix volontaires :
- **Bambou de la kenninji** : passé au soleil (paille) et non vert frais, pour qu'il se détache de l'herbe.
- **Mousse** : en petites plaques sombres et bosselées. Claire et ronde, elle ressemblait à un nénuphar sur l'herbe :
  un faux appui de saut pour un joueur.
- **Plantes basses et mousse** : on les traverse (pas de collision) et elles prennent le matériau du feuillage.

Ce que la vitrine agrandie en fait (proposition, à remanier dans l'éditeur) :
- un pont de deux dalles naturelles sur la pointe ouest de l'étang, agrandi à 1,12 pour garder de la berge, avec deux
  sentiers qui le raccordent ;
- la tour au bord du marais, le *gorintō* dans le coin des bambous ;
- deux rangs de trois modules de *kenninji-gaki* au sud, qui font des caches ;
- une *sode-gaki* contre l'*azumaya* et une contre le *tsukubai* ;
- les plantes d'ombre au pied des arbres, des lanternes et des clôtures.

La version précédente de la scène agrandie (lot 4 seul) est gardée dans `Captures/vitrine_grande/scene_lot4.unity`.

## Muraille artisanale (28 octobre 2026, nuit)

Demande de Florent : une muraille « plus faite par des artisans ». Script : `Tools/blender/muraille_artisanale.py`.
Il reprend les dimensions de `muraille_jardin.py`. Nouveaux FBX : `Muraille_Artisan_Mur_1`, `_Mur_2`, `_Angle`,
`_Porte`.

| Détail | Fait | Source |
|---|---|---|
| Toit *hongawarabuki* | Des tuiles plates et creuses (*hiragawara*) posées d'abord, depuis l'égout ; des tuiles rondes (*marugawara*) couvrent leurs joints. À l'égout, des tuiles rondes à disque (*nokimarugawara*, le disque s'appelle *gatō*) alternent avec des tuiles plates à lèvre pendante (*nokihiragawara*). | [JAANUS, hongawarabuki](https://aisf.or.jp/~jaanus/deta/h/hongawarabuki.htm), [JAANUS, nokimarugawara](https://aisf.or.jp/~jaanus/deta/n/nokimarugawara.htm) (lues en entier) |
| Faîtage | *Noshigawara* : tuiles épaisses, à peine courbes, empilées le long des faîtages et des arêtiers, couvrant la jonction des deux sortes de tuiles. *Ganburigawara* : la tuile ronde qui coiffe la pile. Tuiles d'ogre (*onigawara*) au bout des faîtages et aux angles. | [JAANUS, noshigawara](https://www.aisf.or.jp/~jaanus/deta/n/noshigawara.htm) (lue en entier), [JAANUS, ganburigawara](https://projects.mcah.columbia.edu/jaanus/node/4457), [JAANUS, ichi no oni](https://projects.mcah.columbia.edu/jaanus/node/3864) |
| Mur *tsuiji* | Boue et argile damées entre des planches (*seki-ita*), le procédé *hanchiku* ; poteaux inclinés tous les 1,8 à 2 m ; cinq lignes blanches d'enduit pour le plus haut rang. | [JAANUS, tsuiji](https://aisf.or.jp/~jaanus/deta/t/tsuiji.htm) (lue en entier), [MLIT, Jison-in](https://www.mlit.go.jp/tagengo-db/en/R1-00636.html) |
| Soubassement | *Uchikomi-hagi* : des pierres dont les faces sont grossièrement frappées et cassées pour s'ajuster au parement, sans mortier ; la technique la plus courante. | [JAANUS, uchikomihagi](https://aisf.or.jp/~jaanus/deta/u/uchikomihagi.htm) |
| Vantaux | Planches debout, ferrures décoratives (*hassō kanagu*). | [JAANUS, kabukimon](https://aisf.or.jp/~jaanus/deta/k/kabukimon.htm) |

Non trouvé, donc fait de mémoire ou par hypothèse :
- les strates de terre là où l'enduit est tombé ;
- les joints de planches tous les 0,3 m ;
- le dessin des tuiles d'ogre (écu, cornes, disque) ;
- le bossage du disque, sans motif : les motifs (*tomoe*, lotus) n'ont pas été confirmés ;
- les fissures ;
- les cales dans les joints des pierres ;
- les bagues de fer des poteaux de la porte.

Le fait main :
- chaque tuile est posée un peu de travers ;
- une tuile sur douze, plus sombre et plus bleue, a été remplacée ;
- les lignes blanches ondulent d'un ou deux millimètres et sont en léger relief ;
- l'enduit tombé laisse paraître la terre en retrait de 3,5 cm ;
- les pierres du soubassement sont un peu de biais, avec de la mousse au pied.

Les petites pièces (tuiles, disques, lignes, cales) n'ont pas de contour : il est réservé aux grandes masses.

Les bouts de chaque module sont ouverts, puisque le voisin les cache. Sinon, leur contour traçait un joint sombre tous
les 4 m, et l'ancienne muraille a ce défaut.

Coût : environ 9 000 faces par module de mur, contre ~1 500 pour l'ancien, soit environ 400 000 faces pour les
50 pièces. Il faudra des niveaux de détail (skill `tech-art`) si les performances le demandent.

Dans la vitrine agrandie, les deux murailles sont dans la scène. « Muraille artisanale » est allumée, « Muraille »
(l'ancienne) est éteinte : rallumer l'une et éteindre l'autre pour comparer. Menu : *6 bis. Poser la muraille
artisanale*.

Captures :
- Blender : `Captures/modeles/vitrine_grande/muraille_*_ancienne.png` et `muraille_*_artisanale.png` ;
- Unity : `Captures/vitrine_grande/nouveau_muraille*.jpg`.

## Muraille de Chine et talus (28 octobre 2026, nuit)

Demande de Florent : « un truc style muraille de Chine », avec le sol relevé sous la muraille.

**À signaler à l'équipe** : la V1 de Notion est un jardin japonais. Une muraille de Chine mêle deux cultures. C'est
donc une **troisième vitrine, à côté** des autres, qui ne remplace rien : `Vitrine_JardinJaponais_Chine.unity`.

Scripts :
- `Tools/blender/muraille_chine.py` ;
- `VITRINE=chine` dans `vitrine_jardin.py`.

Menu : *Vitrine sur talus, muraille de Chine : tout construire*, ou `ToutFaire -chine`.

| Élément | Fait | Source |
|---|---|---|
| Profil | À Badaling, le mur est un trapèze : 6,5 à 7,5 m à la base, 4,5 à 5,8 m en haut, 6 à 9 m de haut ; le dessus laisse passer cinq chevaux de front. | [Visit Beijing, Badaling](https://english.visitbeijing.com.cn/article/47OOiKK1Kj8) (lue en entier), [Visit Our China, walls](https://www.visitourchina.com/special/china-great-wall/walls.html) (lue en entier) |
| Créneaux | *Duokou* du côté de l'ennemi, d'environ 2 m : grandes ouvertures en haut pour guetter et tirer, petites meurtrières en bas. | [Visit Our China, walls](https://www.visitourchina.com/special/china-great-wall/walls.html) |
| Accès | Des portes voûtées (*chuan*), à petits intervalles du côté intérieur ; des marches y montent au sommet. | idem |
| Matériaux | Pierre taillée pour les fondations, les rives et les portes ; sous les Ming, la brique cuite devient le matériau principal. Une porte de Badaling repose sur plus de dix assises de granit, sous de grandes briques. | idem, [China Xian Tour](https://www.chinaxiantour.com/travel-guide/how-was-the-great-wall-built.html) |
| Tours de guet | Creuses, en maçonnerie, sur le mur, à deux niveaux reliés par un escalier ; voûtes, meurtrières, fenêtres de guet, drains. Environ 10 à 15 m de haut, 9 × 9 m à Badaling. | [Visit Beijing](https://english.visitbeijing.com.cn/article/47OOiKK1Kj8), [Simatai, Google Arts](https://artsandculture.google.com:443/story/SAVhLZQwDXng7Q) |
| Porte | Vantaux rouges à 81 clous dorés, neuf rangs de neuf, réservés à l'empereur (sept fois sept pour un prince) ; plaque au-dessus de l'arche. | [Chiculture](https://www.chiculture.org.hk/en/china-five-thousand-years/4501), [Visit Beijing](https://english.visitbeijing.com.cn/article/47OOiKK1Kj8) |

Ce que le jeu en fait (propositions) :
- **Échelle** : environ 0,55 du réel, pour une grenouille d'1 m. Le mur fait 4,4 m de haut sur sa fondation de
  granit (0,9 m), 3,9 m d'épaisseur au pied et 3,1 m au chemin de ronde.
- **Créneaux et parapets** : merlons de 1,35 m côté dehors, créneaux de 0,45 m tous les 2 m (le module de 4 m se
  raccorde), une meurtrière dans chaque merlon. Côté jardin, un parapet de 0,65 m.
- **Tours de guet** : six sur les longs côtés, quatre sur les petits, quatre d'angle, et la porte au milieu du sud.
- **Talus** : le sol monte de 1,4 m sur les 5 derniers mètres et reste haut sous la muraille, puis redescend au-delà.
  Dans l'eau, il ne monte qu'au ras de la rive (la pointe ouest du marais).

Non trouvé, donc fait de mémoire ou par hypothèse :
- la gargouille de pierre côté jardin ;
- le petit abri à toit de tuiles sur les tours ;
- les briques peintes de 0,4 × 0,2 m ;
- les proportions des tours ;
- la plaque laissée vierge (pas de caractères).

Coût : environ 3 000 faces par module de mur, 9 500 à 12 800 par tour, soit environ 230 000 pour les 50 pièces.

Ce qui reste à décider :
- **Le chemin de ronde** : il n'est pas accessible. Les murs invisibles de la vitrine sont au pied du parement, et
  les portes voûtées sont peintes. S'il doit devenir un terrain de jeu (un chemin haut tout autour, pour le Capture
  The Flag), c'est une décision de design.
- **Les vantaux de la porte** sont fermés.

Captures :
- Unity : `Captures/vitrine_chine/` ;
- Blender : `Captures/modeles/vitrine_chine/`.

### Deuxième version : « ça fait muraille de château » (28 octobre 2026, nuit)

Le premier jet de Florent a été jugé « muraille de château, pas muraille de Chine ». Il posait un rectangle plat, avec
des tours carrées hautes aux angles, des petits toits en pointe et des briques très marquées : un plan de château fort.
Le premier jet est gardé dans `Captures/vitrine_chine/premier_jet_*`.

| Ce qui distingue la muraille | Fait | Source |
|---|---|---|
| Elle suit les crêtes | Simatai : le mur s'accroche aux arêtes des monts Yan, raide et étroit par endroits ; Jinshanling (« les crêtes de la montagne d'or ») suit des arêtes sur 10,5 km. | [Wikipedia, Simatai](https://en.wikipedia.org/wiki/Simatai) (lue en entier), [China Highlights, Jinshanling](https://www.chinahighlights.com/greatwall/section/jinshanling-greatwall.htm) |
| Tours rapprochées | Jinshanling : 67 tours sur ~10,5 km, une tous les 100 à 150 m. | idem |
| La grande porte | Shanhaiguan : tour de deux étages à double avant-toit, des bêtes sculptées aux quatre coins des avant-toits, 68 meurtrières, une grande plaque. | [Travel China Guide, Shanhaiguan](https://www.travelchinaguide.com/china_great_wall/scene/hebei/shanhaiguan/index.htm) (lue en entier), [Lonely Planet](https://www.lonelyplanet.com/points-of-interest/first-pass-under-heaven/1107413) |

Ce qui a changé :
- **La crête** (`vitrine_jardin.py`, `VITRINE=chine`) : le jardin est au fond d'une vallée. Le sol monte vers une crête
  qui fait le tour : 0,6 m au col de la porte, au sud, jusqu'à 4,8 m au nord. La crête se compte en joints de module,
  par pas de 0,6 m, plate sous les tours et la porte.
- **Le mur suit la crête** : cinq modules en pente, de −1,2 à +1,2 m sur 4 m.
- **Les tours** sont trapues, débordent des deux côtés et portent trois fenêtres voûtées par face. Les deux plus hautes
  ont un pavillon à toit recourbé.
- **La porte** porte une tour de bois à deux étages, à double avant-toit : colonnes rouges, treillis, plaque bleue à
  cadre doré, arêtiers et bêtes aux coins, ornements dressés au bout du faîtage.
- **Les briques** sont beige-gris et peu contrastées : de loin, une seule masse, pas une maçonnerie de donjon. Les
  créneaux sont plus serrés (tous les 1,33 m), avec une fenêtre de guet en haut des merlons et une meurtrière dans le
  bandeau.
- **Le décor au-delà** : des montagnes (`Chine_Montagnes`, ~13 000 faces) et deux branches de muraille de 26 et
  22 modules. Elles partent des angles nord et grimpent sur les crêtes jusqu'à 12 et 14 m, avec une tour tous les six
  modules. C'est ce qui dit « Grande Muraille » plutôt que « ville fortifiée ». Personne ne sort de l'arène.
- **Les murs invisibles** de la vitrine montent maintenant à 14 m, contre 5 m avant (pour toutes les variantes, à la
  prochaine reconstruction).

Fait de mémoire : la courbe des toits (plus plats à l'égout), les ornements du faîtage, le dessous des avant-toits peint
en bleu-vert, le dessin des montagnes.

À juger par l'équipe :
- le mélange Chine et Japon ;
- un chemin de ronde jouable ;
- la hauteur de la crête : un sol en pente au bord change le terrain de jeu, et c'est une décision de design.

## Bordure de bambous et choix du contour dans le menu de debug (28 octobre 2026, nuit)

Demandes de Florent :
- « une troisième version plus sobre, avec juste des roseaux ou quelque chose de fin, comme des bambous, pour faire le
  contour de l'arène » ;
- pouvoir changer de contour depuis le menu de debug.

### La bordure

Script : `Tools/blender/bordure_bambous.py`. Modules `Bordure_Bambous_1`, `_2` et `_Angle`, posés aux places de la
muraille de la vitrine agrandie.

| Plante | Fait | Source |
|---|---|---|
| Bambou traçant (*Phyllostachys*) | Cannes jusqu'à ~4 cm de diamètre ; 4,5 à 9 m de haut (*P. aureosulcata*) ; en écran, plantés à 1,5 m ou plus serrés (conseil de pépiniériste). | [UConn, Jeff Ward](https://cipwg.uconn.edu/wp-content/uploads/sites/244/2016/10/PPT-PDF-Jeff-Ward-Session-6.pdf), [BC Arboretum](https://bcarboretum.org/plants/genus/Phyllostachys/species/aureosulcata), [Lewis Bamboo](https://lewisbamboo.com/products/yellow-groove) |
| Roseau commun (*Phragmites australis*) | 2 à 4 m (jusqu'à 6 m) ; tiges jusqu'à 2,5 cm ; panicule de 15 à 50 cm. | [Colorado](https://ag.colorado.gov/conservation/noxious-weeds/noxious-weed-species/common-reed), [African Legacy](https://www.africanlegacy.org.za/indigenous-plant/phragmites-australis-common-reed), [Mass.gov](https://www.mass.gov/doc/phragmites-0/download) |

Ce que le jeu en fait :
- **Les bambous** : douze cannes par module de 4 m, en deux rangs serrés, de 4,5 à 6,5 m de haut et 5 à 7 cm de
  diamètre (grossies pour se lire de loin). Une sur quatre est plus jaune.
- **Les roseaux** : des touffes de 1,8 à 2,6 m au pied, des deux côtés, avec un plumeau brun violacé.
- **Le sol** : chaque module porte sa bande de terre herbeuse, parce que le sol de la vitrine s'arrête aux bornes.
- **La traversée** : on ne bute pas sur la bordure, ce sont les murs invisibles qui tiennent le bord. La caméra la
  traverse aussi.
- **Le coût** : environ 8 000 faces par module. Le premier jet en faisait 12 500, soit 600 000 faces pour le tour de
  l'arène : les feuilles n'ont plus de contour, et les cannes ont cinq pans.

### Le choix du contour

Le composant `Assets/Froggy/Scripts/Jardin/ChoixMuraille.cs` est posé sur « Menu debug caméra » (étape 9 de
VitrineJardin). Le menu de debug (touche au-dessus de Tab) a une section « Contour de l'arène » :
- **Temple japonais (muraille artisanale)**, **Temple japonais (muraille simple)** et **Bambous et roseaux** : les
  trois sont dans la vitrine agrandie, le menu allume l'un et éteint les autres.
- **Muraille de Chine** : elle charge la scène `_Chine`, qui a son sol en crête. Les éléments du jardin près des bords
  y sont posés sur la pente : une seule scène ne pouvait pas porter les deux sols. Choisir un contour du temple depuis
  la scène chinoise ramène à la vitrine agrandie avec ce contour allumé.

Après un changement de scène, le menu se rouvre. Dans un build de développement, les deux scènes devront être dans les
Build Settings (non modifiés ici). Dans l'éditeur, elles se chargent sans y être.

Vérifié :
- tests `ChoixMurailleTests` (4 OK) ;
- en mode Play, dans l'éditeur : temple, puis bambous (un seul contour allumé), puis muraille de Chine (scène chargée,
  menu rouvert), puis retour au temple avec la muraille simple.

Captures en jeu : `Captures/vitrine_grande/jeu_bambous_*.jpg`.
