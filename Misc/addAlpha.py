import os, fnmatch, shutil
import Image, struct


for root, dirnames, filenames in os.walk('.'):
  for fn in fnmatch.filter(filenames, '*-rgb.png'):
    filename=os.path.join(root, fn)
    afn=os.path.splitext(filename)[0][:-4]+'-alpha.png'
    if not os.path.exists(afn): continue
    print 'processing '+filename+' + '+afn

    im=Image.open(filename)
    aim=Image.open(afn)
    im.putalpha(aim.convert('L'))
    im.save(os.path.splitext(filename)[0][:-4]+'.png')
