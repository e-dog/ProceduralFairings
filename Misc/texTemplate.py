import Image, ImageDraw

im=Image.new('RGB', (1024, 1024), '#800')

draw=ImageDraw.Draw(im)

# strip
draw.rectangle((1024-32, 0, 1024, 1024), fill='#ca2')

# outer
draw.polygon((10, 10+320, 10+480, 10+320, 10+480/2, 10), fill='#88c')
draw.rectangle((10, 1024-10-160, 10+480, 1024-10), fill='#8cc')
draw.rectangle((10, 10+320, 10+480, 1024-10-160), fill='#ccc')

# inner
draw.polygon((20+480, 10+320, 20+480*2, 10+320, 20+480*3/2, 10), fill='#448')
# draw.rectangle((20+480, 10, 20+480*2, 10+320), fill='#448')
draw.rectangle((20+480, 1024-10-160, 20+480*2, 1024-10), fill='#488')
draw.rectangle((20+480, 10+320, 20+480*2, 1024-10-160), fill='#888')

del draw

# im.show()
im.save('template.png')

