from PIL import Image
import numpy as np
import matplotlib.pyplot as plt
from skimage.color import rgb2hsv, hsv2rgb
import copy
import colorsys

def shiftHue_naive(cur, target_hue, amount): 
    if (abs(cur - target_hue) <= amount): # if already close enough to target color then just use target color?
        return target_hue
    opposite_hue = (target_hue + 0.5) % 1.0
    if (target_hue < opposite_hue):
        if (cur > target_hue and cur < opposite_hue):
            return cur - amount
        else:
            return (cur + amount) % 1.0
    else:
        if (cur > opposite_hue and cur < target_hue):
            return cur + amount
        else:
            return (cur + 1.0 - amount) % 1.0

# operates on a single pixel's hue
# target_hue and amount are both in [0,1]
def shiftHue(cur, target_hue, amount): 
    # if (abs(cur - target_hue) <= amount): # if already close enough to target color then just use target color?
    #     return target_hue
    opposite_hue = (target_hue + 0.5) % 1.0
    if (target_hue < opposite_hue):
        if (cur > target_hue and cur < opposite_hue):
            if (abs(cur - target_hue) <= amount):
                return cur - abs(cur - target_hue)*amount # scale the shift by amount
            return cur - amount
        else:
            if (abs(cur - target_hue) <= amount):
                return (cur + abs(cur - target_hue)*amount) % 1.0
            return (cur + amount) % 1.0
    else:
        if (cur > opposite_hue and cur < target_hue):
            if (abs(cur - target_hue) <= amount):
                return cur + abs(cur - target_hue)*amount
            return cur + amount
        else:
            if (abs(cur - target_hue) <= amount):
                return (cur + 1.0 - abs(cur - target_hue)*amount) % 1.0
            return (cur + 1.0 - amount) % 1.0

# assume that the image is already in hsv color space
# returns an image still in hsv space
def HSV_colorShift(image, target_hue, shift_amount):
    # img_array = np.array(image) / 255.0  # Normalize to the range [0, 1]
    # # Convert RGB to HSV color space
    # img_hsv = rgb2hsv(img_array)  # HSV range is [0,1] for all 3 channels
    
    # Shift the hue component towards red -- red has hue of 0
    # target_hue = 0.0 # should be extracted from target_color, use red=0.0 for now
    # print(np.shape(img_hsv[:,:,0]))     # (640, 426)
    shiftHue_vectorized = np.vectorize(shiftHue)
    hue_array_shifted = shiftHue_vectorized(image[:,:,0], target_hue, shift_amount)
    image[:,:,0] = hue_array_shifted

    # # Convert back to RGB color space
    # shifted_image = hsv2rgb(img_hsv)
    # # Clip values to the valid range [0, 1]
    # shifted_image = np.clip(shifted_image, 0, 1)
    # shifted_img = Image.fromarray((shifted_image * 255).astype(np.uint8))
    return image


# image_path = "./testImages/testRainbow/rainbow.jpg" 
image_path = "./testImages/testBeach/beach.jpg"  
# image_path = "./testImages/testCluttered/city.jpg" 
target_color = [255, 0, 0]  # use the opposite hue of the avg color in label area
# use CIE2000 to get the opposite color

red_rgb = [1, 0, 0]
shift_percentage_hsv = 0.7
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
print(np.shape(middle_rgb))
# get avg RGB color of middle_rgb
average_color = np.mean(middle_rgb, axis=(0, 1))
print(average_color)


middle_rgb = np.array(middle_rgb) / 255.0  # Normalize to the range [0, 1]
middle_hsv = rgb2hsv(middle_rgb)

# calculate target hue: opposite hue from average_color's hue
average_color = average_color / 255.0
avg_hsv = colorsys.rgb_to_hsv(average_color[0], average_color[1], average_color[2])
target_hue = (avg_hsv[0] + 0.5) % 1.0

shifted_middle_hsv = HSV_colorShift(middle_hsv, target_hue, shift_percentage_hsv)
shifted_middle_rgb = hsv2rgb(shifted_middle_hsv)
# Clip values to the valid range [0, 1]
shifted_middle_rgb = np.clip(shifted_middle_rgb, 0, 1)
shifted_middle_rgb = (shifted_middle_rgb * 255).astype(np.uint8)
# Update the original RGB image with the modified middle region
original_image_hsv = copy.deepcopy(original_image)
original_image[height_start:height_end, width_start:width_end, :] = shifted_middle_rgb
HSVshifted_image = original_image


#----------Plots-------------------------------------
# Plot the original and shifted images side by side
fig, axes = plt.subplots(1, 2, figsize=(10, 5))

# Plot the original image
axes[0].imshow(np.array(original_image_copy))
axes[0].set_title('Original Image')
axes[0].axis('off')

# Plot the HSV shifted image
axes[1].imshow(np.array(HSVshifted_image))
axes[1].set_title('HSV shifted Image')
axes[1].axis('off')


# Display the plot
plt.show()