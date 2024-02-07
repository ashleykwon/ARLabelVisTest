import os
import numpy as np
import matplotlib.pyplot as plt
from matplotlib.image import imread
from skimage.io import imsave

# run this script on mask photo files generated in photoshop to ensure all pixels are either 0 or 255
# put mask files into this script's enclosing folder

directory_path = './'

images = []

for filename in os.listdir(directory_path):
    if filename.endswith('.jpg') or filename.endswith('.png'):
        file_path = os.path.join(directory_path, filename)
        img = imread(file_path)
        img_binary = np.where(img < 128, 0, 255).astype(np.uint8)
        images.append((img_binary, filename))

for image, filename in images:
    new_filename = f"binary_{filename}"
    new_file_path = os.path.join(directory_path, new_filename)
    imsave(new_file_path, image, quality=100)
