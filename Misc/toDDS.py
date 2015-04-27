from PIL import Image, ImageOps
import os, glob

for fn in glob.glob("../GameData/ProceduralFairings/*.tga"):
  if "_NRM" in fn:
    im=Image.open(fn)
    r, g, b = im.split()
    # g=ImageOps.invert(g)
    im=Image.merge('RGBA', (g, g, g, r))
    im.save("tmp.png")
    os.system("crunch -yflip -dxt5 -file tmp.png -out "+fn[:-4]+".dds")
    os.remove("tmp.png")
  else:
    os.system("crunch -yflip -dxt5 -file "+fn+" -out "+fn[:-4]+".dds")
