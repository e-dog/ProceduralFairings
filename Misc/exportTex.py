import os
import Image
from psd_tools import PSDImage


def export_tex(fn, downscale=2, ext='.png'):
  print 'processing', fn
  psd=PSDImage.load(fn+".psd")
  im=psd.as_PIL()
  wd,ht=im.size
  if downscale>1: im=im.resize((wd/downscale, ht/downscale), Image.ANTIALIAS)
  im.save(os.path.join(target_path, fn+ext))


target_path='../unity/Assets/ProceduralFairings'
export_tex('baseTex')
export_tex('baseRingTex')
export_tex('thrustPlate1')
export_tex('thrustPlate1bump', 1)

target_path="C:/games/KSPtest/GameData/ProceduralFairings/"
export_tex('fuselage1', 1, '.tga')
