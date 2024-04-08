from PIL import Image
import numpy as np
import matplotlib.pyplot as plt
from skimage.color import rgb2hsv, hsv2rgb, rgb2lab, lab2rgb
import copy
import colorsys

from mpl_toolkits.mplot3d import Axes3D
from sklearn.cluster import KMeans

def plot_rgb_space(image):
    """
    Plot each pixel of the image in the RGB color space.

    Args:
    - image: A numpy array representing the image.

    Returns:
    - None
    """
    r, g, b = image[:, :, 0].flatten(), image[:, :, 1].flatten(), image[:, :, 2].flatten()
    colors = image.reshape((-1, 3)) / 255.0
    fig = plt.figure()
    ax = fig.add_subplot(111, projection='3d')
    ax.scatter(r, g, b, c=colors, marker='o')
    ax.set_xlabel('Red')
    ax.set_ylabel('Green')
    ax.set_zlabel('Blue')
    plt.show()


def median_cut(image, num_colors=10):
    """
    Extract a color palette from the image using the median cut method.

    Args:
    - image: A numpy array representing the image.
    - num_colors: Number of colors to extract (default is 10).

    Returns:
    - A list of RGB tuples representing the colors in the palette.
    """
    def cut(pixels, level):
        if level == 0 or len(pixels) <= num_colors:
            return [np.median(pixels, axis=0)]
        ranges = np.ptp(pixels, axis=0)
        dim = np.argmax(ranges)
        median = np.median(pixels[:, dim])
        below_median = pixels[pixels[:, dim] <= median]
        above_median = pixels[pixels[:, dim] > median]
        return cut(below_median, level - 1) + cut(above_median, level - 1)

    pixels = image.reshape((-1, 3))
    colors = cut(pixels, np.ceil(np.log2(num_colors)))
    new_colors = []
    for color in colors:
        color = color.astype(int)
        new_colors.append(color)
    
    return new_colors


def extract_palette(image, num_colors=10):
    """
    Extract a color palette from the image.

    Args:
    - image: A numpy array representing the image.
    - num_colors: Number of colors to extract (default is 10).

    Returns:
    - A list of RGB tuples representing the colors in the palette.
    """
    # Reshape the image to a 2D array of pixels
    pixels = image.reshape((-1, 3))
    
    # Use KMeans clustering to group similar colors
    kmeans = KMeans(n_clusters=num_colors)
    kmeans.fit(pixels)
    
    # Get the cluster centers which represent the colors
    colors = kmeans.cluster_centers_
    
    # Scale the colors back to the range [0, 255]
    colors = colors.astype(int)
    
    return colors.tolist()

def plot_palette(palette, image):
    """
    Plot the extracted color palette.

    Args:
    - palette: A list of RGB tuples representing the colors in the palette.

    Returns:
    - None
    """
    plt.figure()
    average_color = np.mean(image, axis=(0, 1))
    palette.append([round(average_color[0]), round(average_color[1]), round(average_color[2])])
    print("average color: ", average_color, "palette color 1: ", palette[0])
    for i, color in enumerate(palette):
        plt.subplot(1, len(palette), i + 1)
        plt.imshow([[color]], extent=[0, 1, 0, 1], aspect='auto')
        plt.axis('off')
    plt.show()

def get_and_plot_opposite(palette_colors):
    # palette_normalized = []
    # for color in palette_colors:
    #     palette_normalized.append([color[0]/255.0, color[1]/255.0, color[2]/255.0])
    # # Convert palette colors from RGB to LAB color space
    # palette_lab_colors = [rgb2lab(color) for color in palette_normalized]

    # Read LAB colors from the lookup table and convert to map
    rgb_colors_map = {}
    with open('CorrespondingRGBVals.txt', 'r') as rgb_file:
        for i, line in enumerate(rgb_file):
            rgb_color = tuple(map(float, line.strip().split(',')))
            rgb_colors_map[i] = rgb_color
    print(palette_colors)
    # Find the line indices of the palette colors in the LAB colors map
    palette_rgb_indices = []
    for color in palette_colors:
        # print("palette color: ", color)
        for index, rgb_color in rgb_colors_map.items():
            thres = 3
            if abs(color[0]-rgb_color[0])<thres and abs(color[1]-rgb_color[1])<thres and abs(color[2]-rgb_color[2])<thres:
            # if np.allclose(color, rgb_color):  # Check if colors are close due to floating-point precision
                palette_rgb_indices.append(index)
                break
    print("rgb indices: ", (palette_rgb_indices))

    # Read the corresponding opposite LAB colors from lookup tables
    opposite_lab_colors = []
    with open('CandidateLABvals.txt', 'r') as lab_file: 
        for i, line in enumerate(lab_file):
            if i in palette_rgb_indices:
                opposite_lab_colors.append(tuple(map(float, line.strip().split(','))))
    # print(len(opposite_lab_colors))

    opposite_rgb_colors = []
    # convert opposite_lab_colors into RGB values
    for lab_color in opposite_lab_colors:
        rgb_color = lab2rgb(lab_color) # [0,1]
        rgb_color_rescaled = [(rgb_color[0]*255).astype(int), (rgb_color[1]*255.0).astype(int), (rgb_color[2]*255.0).astype(int)]
        opposite_rgb_colors.append(rgb_color_rescaled)
    print(opposite_rgb_colors)
    # Find the opposite RGB colors for each palette color
    # opposite_palette_colors = [tuple(opposite_rgb_colors[i]) for i in range(len(palette_colors))]

    # Plot the original palette colors and their opposite colors in a strip of squares
    plt.figure(figsize=(len(palette_colors), 2))
    for i, color in enumerate(palette_colors):
        plt.subplot(1, len(palette_colors)*2, i+1)
        plt.imshow([[color]], extent=[0, 1, 0, 1], aspect='auto')
        plt.axis('off')
        plt.title('Palette Color')
        
        plt.subplot(1, len(palette_colors)*2, len(palette_colors)+i+1)
        plt.imshow([[opposite_rgb_colors[i]]], extent=[0, 1, 0, 1], aspect='auto')
        plt.axis('off')
        plt.title('Opposite Color')

    plt.tight_layout()
    plt.show()

# -------------------------------------------------------
# image_path = "./testImages/testRainbow/rainbow.jpg" 
image_path = "./testImages/testBeach/beach.jpg"  
# image_path = "./testImages/testCluttered/city.jpg" 

image = plt.imread(image_path)
# plot_rgb_space(image)
palette = extract_palette(image, num_colors=6)
# plot_palette(palette, image)


# Find palette colors in the selected area
# then use CIE2000 to get the opposite color
original_image = np.array(Image.open(image_path))
# Define the region to be operated on
height, width, _ = np.shape(original_image)
height_start =  height // 4
height_end = height // 2
width_start =  width // 4
width_end = width // 2
# Extract the middle region
original_image_copy = copy.deepcopy(original_image)
middle_rgb = original_image_copy[height_start:height_end, width_start:width_end, :] # range [0,255]
# print(np.shape(middle_rgb))

# plot_rgb_space(middle_rgb)
palette2 = extract_palette(middle_rgb, num_colors=4)
# palette2 = median_cut(middle_rgb, num_colors=1)
# plot_palette(palette2, middle_rgb)
get_and_plot_opposite(palette2)
# plt.imshow(middle_rgb)
# plt.show()


