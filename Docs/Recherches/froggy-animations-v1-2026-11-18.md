# Animations de Froggy pour la V1 (nage, plongeon, réception)

Date : 18 novembre 2026. Sert aux prompts `text_to_motion` de Meshy (#74, #75, #79). Froggy est une grenouille
**humanoïde** (Notion) : on garde du réel ce qui se lit, et on reste sur un squelette humain.

## Faits retenus et sources

| Sujet | Fait | Source |
|---|---|---|
| Réception | Le crapaud buffle freine et se stabilise **avec les pattes avant seules**. Il aligne ses bras sur sa trajectoire dès 16 ms avant de quitter le sol, les garde alignés pendant tout le vol, et touche le sol bras dans l'axe (bras 4,1 ± 7,6° au-dessus de l'angle d'impact). | Cox & Gillis 2017, *Biology Open* 6:71-76, lu en entier ([Europe PMC](https://www.ebi.ac.uk/europepmc/webservices/rest/PMC5278434/fullTextXML)), CC-BY |
| Réception | Les coudes commencent à s'étendre au même moment quel que soit le saut ; un saut plus long laisse plus de temps, donc des coudes plus tendus et plus de distance pour freiner. | même article, discussion (renvoie à Cox & Gillis 2015) |
| Réception | Les grenouilles léopards et les sonneurs se reçoivent sur les pattes avant et ramènent les pattes arrière sous le corps en plein saut. | [SICB, Reilly et al.](https://sicb.org/?p=19486) (résumé) |
| Nage | Cycle « coup puis glisse » : forte accélération pendant le coup des deux pattes arrière ensemble, puis glisse ralentie par la traînée. La rotation des pieds donne l'essentiel de la poussée, en fin de coup. | Richards 2010, [JEB 213:621](https://journals.biologists.com/jeb/article/213/4/621/10128/Kinematics-and-hydrodynamics-analysis-of-swimming) (résumé : le texte intégral est bloqué au navigateur) |
| Nage | Le sillage montre deux anneaux tourbillonnaires, un par pied : les deux pieds poussent ensemble. | Stamhuis & Nauwelaerts 2005, [JEB 208:1445](https://journals.biologists.com/jeb/article/208/8/1445/16018/Propulsive-force-calculations-in-swimming-frogs-II) (résumé) |
| Entrée dans l'eau | Les rainettes grillons lancent les pattes avant devant elles (pose « Superman ») avant de toucher l'eau ventre le premier. | [Virginia Tech, 2024](https://news.vt.edu/articles/2024/12/eng-me-socha-frogs.html) |
| Brasse | On l'appelle « coup de grenouille », mais un vrai coup de grenouille claque les pieds palmés vers l'arrière ; la brasse est un geste en fouet. | [360swim](https://360swim.com/blog/breaststroke-lesson-how-about-them-legs) |

| Croassement | Chez la plupart des anoures, le sac vocal gonfle **en même temps** que le chant (signal « fixe »). Le robot-grenouille des auteurs, réglé sur un mâle réel, gonfle puis dégonfle en **~450 ms pour un chant de 400 ms**, le dégonflement finissant **~50 ms après** la fin du son. Décalé de la moitié du chant, l'ensemble est rejeté par les femelles : l'image et le son doivent aller ensemble. Sur la photo, le sac d'un túngara est aussi gros que la grenouille. | Taylor, Klein, Stein & Ryan 2011, *JEB* 214:815-820, lu en entier ([KOPS](https://kops.uni-konstanz.de/bitstreams/95e55b09-7d6b-4d5e-a116-165a57009b1f/download)) |
| Attaque (art) | Une attaque lisible se réduit à trois poses : **élan, impact, récupération** (attaque de Quill, *Moss: Book 2*). | [80.lv, 2026](https://80.lv/articles/breakdown-animating-tiny-character-with-massive-two-handed-weapon) (résumé d'une analyse d'anim.works) |
| Grosse tête (art) | Une tête qui bouge d'un bloc avec le corps fait statue : elle doit suivre avec un temps de retard (recouvrement). | [Cursa](https://cursa.app/en/page/character-animation-starter-kit-readable-poses-silhouettes-and-line-of-action), [anim.works](https://anim.works/?p=28648) (conseils d'animateurs, source faible) |

Non trouvé : le rôle des pattes avant pendant la nage (aucun résultat ne les décrit) ; les durées chiffrées d'un cycle de
nage ou d'un plongeon ; une conférence sur l'animation de Fall Guys ou d'un autre personnage à petits bras (le blog de
Mediatonic ne parle que du design). Ce qui suit est donc, pour ces points, une **hypothèse de style**.

## Pourquoi les mouvements de Meshy ne se voyaient pas sur Froggy P (18 novembre 2026)

Mesures sur le modèle (`Tools/blender/froggy_p.py`) : bras de **23 cm** de l'épaule à la main, épaule à 40 cm du sol,
tête de **84 cm de large** entre 45 cm et 1 m de haut. Bras tendu sur le côté, la main arrive à 41 cm de l'axe : sous
le bord de la joue. Vue de dos (la caméra du jeu), la tête cache donc tout geste de bras ; vue de face, un geste humain
(coup droit, porter) reste sous le menton. Les clips `text_to_motion`, rejoués par le retargeting humanoïde, bougeaient
surtout le corps d'un bloc : « le modèle semble ne pas être riggé ». Il l'est (ses propres clips lèvent les bras).

Règles retenues pour Froggy P (hypothèses de style, à juger en jeu) :
- **le corps et la tête d'abord** : bascule du buste de 10 à 25°, torsion de 25 à 35°, tête de 15 à 25° ; les bras
  ensuite, levés au-dessus de l'épaule ou écartés, jamais seulement devant le ventre ;
- **le visage porte l'émotion** : yeux fermés « ︶ » (clignement, tristesse), heureux « ^ ^ », plissés « > < » (effort,
  coup reçu), par formes (blendshapes) ;
- **attaque** : élan tenu au moins 0,1 s (la torgnole doit se lire avant de toucher), frappe en deux images à 30 i/s,
  impact tenu 0,1 s, récupération ;
- **la tête suit** le corps par un ressort (4 à 5 Hz, amortissement 0,5 ; 3 Hz et 0,3 pour un coup reçu qui fait
  vaciller) ;
- **croassement** : sac vocal sous le menton, gonflé de 0,30 à 0,78 s du clip (0,48 s, d'après Taylor et al.), tête
  levée pour qu'il se voie : **le son doit partir à 0,30 s et finir vers 0,73 s**.

## Ce que l'animation en garde

- **Réception** : bras tendus vers l'avant **avant** de toucher, mains au sol d'abord, coudes qui plient pour amortir,
  puis pieds écartés en accroupi profond, et on se relève. Saut long = coudes plus tendus à l'impact.
- **Nage** : les deux jambes poussent ensemble, vite (le coup), puis une glisse jambes tendues, plus longue, puis les
  genoux remontent lentement. Bras devant le corps pendant la glisse (hypothèse de style, non sourcée).
- **Plongeon** : accroupi au bord, bond en avant, bras tendus devant (pose « Superman »), jambes tendues derrière.

## Ce qui reste ouvert

- « Glissade » (Notion, animations de déplacement) n'est pas définie : glisser sur un sol mouillé, ou glisser à plat
  ventre ? À préciser par l'équipe avant de la générer.
- La vitesse de lecture de chaque clip se règle en jeu sur le contrôleur (P1-01), pas ici.
