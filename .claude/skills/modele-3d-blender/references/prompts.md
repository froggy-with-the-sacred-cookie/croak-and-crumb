# Prompts Gemini (objets taillés et peints)

Prompts en anglais (Gemini suit mieux) ; textes à l'écran et noms de fichiers en français. Remplacer les `<…>`.
Le style « chunky low-poly, hand-painted, thin dark outline » est celui de Lovecraft : le remplacer par la direction
artistique choisie pour Froggy (skill `direction-artistique`).

## 1. Quatre concepts

Références : image 1 = l'icône de l'objet (si elle existe), image 2 = une capture du jeu à l'endroit de l'objet
(caméra seule). Ratio 4:3.

Base commune :
```
Image 1 is the painted icon of <objet> in our <genre et ton du jeu>. Image 2 is a real screenshot of <arène> in the
game (camera angle, lighting, scale; <décrire le modèle actuel s'il y en a un, et pourquoi il ne va pas>).
Design a NEW <objet>, the <rôle dans le jeu : ce que le joueur en fait>, as a finished 3D game asset in the style of
the icon: <style>. Show it LARGE in the centre, seen from the game camera (<angle de la caméra du jeu>), standing on
<le sol du lieu>, nothing else around. It must be recognisable as <OBJET> instantly, even from far away: a strong
silhouette, <le contraste signature>, and exactly <N> distinct <pièces> on the visible side (the game removes one at
each <interaction>, so they must read as <N> separate pieces). No text, no UI, no character. Variant:
```
Puis une variante par piste, qui doit vraiment différer des autres. Exemples du filon de cuivre de Lovecraft :
- veines vives en relief qui brillent, pépites qui en jaillissent ;
- deux rochers, pépites tachées de patine turquoise, cailloux cuivrés au pied (retenue) ;
- affleurement stratifié, filons exposés, marques de pioche ;
- roche sombre couronnée de trois grappes de cristaux.

## 2. Planche de référence (6 vues)

Références : image 1 = le concept retenu ; image 2 = éventuellement un autre concept dont on reprend un détail.
Ratio 16:9, taille 2K.
```
Image 1 is our chosen design for <objet> (<résumé en une phrase : formes, pièces, matériaux>). <Image 2 only shows
<le détail repris> that we want to add.> Make a clean MODEL REFERENCE SHEET of exactly this object for a 3D modeller,
on a plain light grey background, flat even lighting, same shapes, colours and outline. Arrange six views in a 3 by 2
grid, each view the same scale, clearly separated, no text, no labels, no numbers: top row = FRONT view
(orthographic), RIGHT SIDE view (orthographic), BACK view (orthographic); bottom row = TOP view (orthographic, seen
from straight above), THREE-QUARTER view from <angle> above, and the same three-quarter view AFTER <interaction>: the
<N> <pièces> removed, leaving <ce qui reste : creux, cassure, miettes…>.
```
Gemini ajoute parfois des étiquettes (« FRONT ») malgré la consigne : sans importance pour une planche de travail.

## Ce qui a marché

- Montrer la capture du jeu avec le modèle actuel et dire pourquoi il ne va pas : les concepts se calent sur la vraie
  échelle et la vraie lumière. (Mais jamais une capture comme guide de **composition** d'une scène : skill
  `production-assets`, section 3.)
- Nommer le signe distinctif du matériau (la patine verte du cuivre) : c'est lui qui rend l'objet reconnaissable.
- Demander le nombre exact de pièces : il correspond au nombre d'interactions.
