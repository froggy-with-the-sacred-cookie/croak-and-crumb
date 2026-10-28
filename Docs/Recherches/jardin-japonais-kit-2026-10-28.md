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
