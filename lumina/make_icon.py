from PIL import Image, ImageDraw
from pathlib import Path

size = 256
img = Image.new("RGBA", (size, size), (9, 9, 14, 255))
p = img.load()
for y in range(size):
    for x in range(size):
        dx = x - size * 0.55
        dy = y - size * 0.45
        d = min(1.0, (dx * dx + dy * dy) ** 0.5 / 210)
        p[x, y] = (int(139 * (1-d) + 12*d), int(92 * (1-d) + 12*d), int(246 * (1-d) + 20*d), 255)

d = ImageDraw.Draw(img)
d.rounded_rectangle((24, 24, 232, 232), radius=48, outline=(190, 165, 255, 120), width=3)
d.rounded_rectangle((78, 56, 112, 190), radius=14, fill=(247, 245, 255, 255))
d.rounded_rectangle((92, 156, 184, 190), radius=14, fill=(247, 245, 255, 255))
path = Path(__file__).with_name("lumina.ico")
img.save(path, format="ICO", sizes=[(256,256),(128,128),(64,64),(48,48),(32,32),(16,16)])
print(path)
