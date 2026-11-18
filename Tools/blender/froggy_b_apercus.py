from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import json, subprocess, math, sys, shutil

root=Path(sys.argv[1])
manifest=json.loads((root/'inspection/animations-livrees.json').read_text())
assert len(manifest)==5
previews=root/'apercus';previews.mkdir(exist_ok=True)
ffmpeg=shutil.which('ffmpeg')
if not ffmpeg: raise RuntimeError('ffmpeg doit être installé pour encoder les aperçus.')
font_path=next((p for p in [Path('/System/Library/Fonts/Helvetica.ttc'),Path('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'),Path('C:/Windows/Fonts/arial.ttf')] if p.exists()),None)
def make_font(size):
    return ImageFont.truetype(str(font_path),size) if font_path else ImageFont.load_default(size=size)
font=make_font(25)
big=make_font(35)
small=make_font(21)
clips=[]
for record in manifest:
    label=record['label'];paths=sorted(Path(record['render_folder']).glob('*.png'))
    assert len(paths)==record['preview_frames']
    frames=[Image.open(p).convert('RGB') for p in paths]
    fps=record['preview_fps']
    video=previews/f'Froggy_B_{label}.mp4'
    subprocess.run([ffmpeg,'-hide_banner','-loglevel','error','-y','-framerate',str(fps),'-i',str(Path(record['render_folder'])/'%04d.png'),'-c:v','libx264','-preset','medium','-crf','18','-pix_fmt','yuv420p','-movflags','+faststart',str(video)],check=True)
    gif_frames=[f.quantize(colors=128,method=Image.Quantize.MEDIANCUT) for f in frames]
    durations=[max(10,round((i+1)*100/fps)*10-round(i*100/fps)*10) for i in range(len(frames))]
    gif=previews/f'Froggy_B_{label}.gif'
    gif_frames[0].save(gif,save_all=True,append_images=gif_frames[1:],duration=durations,loop=0,optimize=True,disposal=2)
    clips.append((record,frames))
    print('ENCODED',label,'frames',len(frames),flush=True)

grid_frames=[]
for k in range(120):
    t=k/12
    grid=Image.new('RGB',(1200,970),'#f4f1e8');d=ImageDraw.Draw(grid)
    d.text((24,14),'FROGGY B — PREMIÈRES ANIMATIONS',font=big,fill='#2a3627')
    for i,(rec,frames) in enumerate(clips):
        x=(i%3)*400;y=65+(i//3)*450
        d.text((x+16,y+6),rec['label'].upper(),font=font,fill='#2a3627')
        ix=int((t%rec['duration_seconds'])*rec['preview_fps'])%len(frames)
        frame=frames[ix].resize((396,396),Image.Resampling.LANCZOS)
        grid.paste(frame,(x+2,y+44))
    d.text((830,565),'5 animations',font=big,fill='#2a3627')
    d.text((830,625),'Repos · marche · course',font=small,fill='#596350')
    d.text((830,660),'Saut · célébration',font=small,fill='#596350')
    d.text((830,725),'Vrai modèle rendu dans Blender',font=small,fill='#596350')
    d.text((830,760),'21 crédits supplémentaires',font=small,fill='#596350')
    d.text((830,795),'Essai à valider en jeu',font=small,fill='#596350')
    grid_frames.append(grid)
palette=Image.new('RGB',(600,485*5))
for i,k in enumerate([0,20,45,70,100]):palette.paste(grid_frames[k].resize((600,485)),(0,485*i))
palette=palette.quantize(colors=192,method=Image.Quantize.MEDIANCUT)
indexed=[f.quantize(palette=palette,dither=Image.Dither.FLOYDSTEINBERG) for f in grid_frames]
durations=[round((i+1)*100/12)*10-round(i*100/12)*10 for i in range(120)]
indexed[0].save(previews/'Froggy_B_5_animations.gif',save_all=True,append_images=indexed[1:],duration=durations,loop=0,optimize=True,disposal=2)
grid_frames[0].save(previews/'Froggy_B_5_animations-affiche.png')

# Film fixe de poses clés pour la relecture indépendante de l'animation.
contact=Image.new('RGB',(1600,5*250),'#f4f1e8');dc=ImageDraw.Draw(contact)
for row,(rec,frames) in enumerate(clips):
    dc.text((10,row*250+6),rec['label'],font=small,fill='#2a3627')
    for col in range(8):
        frame=frames[round((len(frames)-1)*col/7)].resize((195,195),Image.Resampling.LANCZOS)
        contact.paste(frame,(col*200,row*250+40))
contact.save(previews/'Froggy_B_poses-de-controle.png')

print('PREVIEWS_COMPLETE',flush=True)
