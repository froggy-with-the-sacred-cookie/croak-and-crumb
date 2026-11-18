# Nénuphars qui s'enfoncent sous le poids (#16), 18 novembre 2026

Recherche faite avant d'écrire le comportement des nénuphars (skill `recherche-reference`). La houle, le lacet et la
pente au vent existaient déjà dans le shader du jardin (`CouleurSommet`, `_Flotter`) : cette fiche porte sur ce qui
manquait, le poids.

## Le réel

- **Un nénuphar ordinaire est souple.** Des essais d'enfoncement sur feuilles flottantes, charge posée au point
  d'attache du pétiole, donnent F = K·δ avec K = 30,7 ± 1,0 N/m (*Nymphaea* 'Black Princess') et 18,5 ± 0,7 N/m
  (*N. lotus*), contre 690,8 ± 17,3 N/m pour le géant *Victoria cruziana*. Les feuilles de *Nymphaea* « ressemblent
  presque à des disques plats ». Sous la charge, la feuille descend et déplace de l'eau, et c'est cette eau qui la
  remonte : la raideur mesurée mêle la flexion de la feuille et la poussée de l'eau.
  Source primaire lue : Box, Erlich, Guan, Thorogood, « Gigantic floating leaves occupy a large surface area at an
  economical material cost », *Science Advances* 8 (6), 2022
  ([article](https://pmc.ncbi.nlm.nih.gov/articles/PMC8827653/)). Non trouvé dans l'article : la force appliquée, la
  taille et la masse des feuilles de *Nymphaea*, toute mesure dynamique (oscillation, amortissement), toute donnée sur
  une grenouille.
- **Ordre de grandeur (hypothèse)** : une grenouille d'environ 30 g (0,3 N, masse non sourcée) sur K ≈ 20 à 30 N/m
  ferait descendre la feuille d'environ 1 à 1,5 cm, nettement visible à l'échelle d'une feuille.
- **La fréquence vient de l'enfoncement.** Un corps flottant chargé oscille à ω ≈ √(g/d), d étant son enfoncement,
  tant qu'on néglige la masse d'eau entraînée, qui abaisse la fréquence
  ([Fitzpatrick, hydrostatique](https://farside.ph.utexas.edu/teaching/336L/Fluidhtml/node28.html)).
  L'amortissement vient surtout des vagues que le corps fait en bougeant, et dépend de la fréquence : lu dans des
  résumés de cours d'hydrodynamique navale, pas dans le texte entier
  ([MIT OCW](https://ocw.mit.edu/courses/2-019-design-of-ocean-systems-spring-2011/bce4ce5626a30f6868668ac7215c9d50_MIT2_019S11_SK2.pdf),
  [Aalto](https://mycourses.aalto.fi/pluginfile.php/1402792/mod_folder/content/0/SD06/01.%20SD06_Lecture%20notes.pdf)).
  Pour le jeu, il est réglé à l'œil.
- **La feuille est tenue par sa tige.** Les pétioles des feuilles flottantes travaillent en traction sous le vent et
  les vagues (*Nymphaea odorata*, [résumé PubMed](https://pubmed.ncbi.nlm.nih.gov/21636476)) : elle penche et tourne un
  peu, elle ne dérive pas. Le shader du jardin le fait déjà (lacet de ±3°, pente au vent).

## Les jeux

Les plates-formes qui s'enfoncent se règlent par leur profondeur et leurs vitesses de descente et de remontée
([devlog](https://mik2121.itch.io/exploration-game/devlog/772693/interactables-part-1)). Ici, pas de plate-forme qui
coule : Notion demande des sauts précis, le nénuphar répond au poids sans jamais retirer le sol.

## Les règles retenues (`ReglagesNenuphar`, toutes PROVISOIRES)

| Fait | Règle | Valeur |
|---|---|---|
| Une feuille souple descend sous une grenouille | Le dessin descend de tant par grenouille posée | 5 cm |
| L'eau déplacée la remonte, plusieurs grenouilles l'enfoncent plus | Plafond, au-delà de la hauteur au-dessus de l'eau (9 cm au jardin) | 12 cm |
| ω ≈ √(g/d) | Fréquence dérivée de l'enfoncement, pas réglée à part | ≈ 2,2 Hz à 5 cm |
| Amortissement par les vagues (non mesuré) | Deux petits rebonds visibles | ζ = 0,35 |
| Proportionner (skill `rendu-vivant`) | La plongée à l'atterrissage suit la vitesse de chute, bornée | 10 % de la vitesse, 1 m/s au plus |
| Une charge au bord fait descendre ce bord | Pente vers la grenouille, selon sa distance au centre | 4° tout au bord |
| Seul le dessin bouge (skills `rendu-vivant` et `multijoueur-equilibrage`) | Le collider reste fixe ; le dessin de Froggy suit celui du nénuphar | — |

Mesuré en jeu dans le jardin : plongée à 7 cm à l'atterrissage, puis 4,8 à 5,2 cm posé, pente de 2 à 3,7° quand Froggy
se pose vers le bord, dessin de Froggy à 1 à 2 mm du dessus du nénuphar, collider immobile.

## Ce qui reste ouvert

- Les grenouilles des autres joueurs n'enfoncent les nénuphars que si leur contrôleur tourne sur la machine
  (`DeplacementWow.Pas`). En réseau, à voir avec le poste 2 : c'est visuel seulement, rien à synchroniser.
- La houle du shader (±1,2 cm) n'est pas suivie par le dessin de Froggy.
- La fleur posée au bord d'un nénuphar (`Fleur_Nenuphar`) est un objet à part : elle ne s'enfonce pas avec lui.
- Pas de rides ni d'éclaboussure à l'atterrissage (skill `rendu-vivant`, P8-03 et P8-06).
