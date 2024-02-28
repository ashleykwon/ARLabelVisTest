from PIL import Image
import numpy as np
import matplotlib.pyplot as plt
from skimage.color import rgb2hsv, hsv2rgb

def RGB_colorShift(image_path, target_color, shift_percentage):
    # Open the image
    img = Image.open(image_path)
    # Convert the image to a NumPy array
    img_array = np.array(img)
    # Normalize the color values to the range [0, 1]
    img_normalized = img_array / 255.0
    target_color_normalized = np.array(target_color) / 255.0
    # Calculate the difference between each pixel's color and the target color
    color_difference = target_color_normalized - img_normalized
    # Shift each pixel's color towards the target color by a certain percentage
    shifted_image_normalized = img_normalized + color_difference * shift_percentage
    # Clip the values to ensure they stay in the valid range [0, 1]
    shifted_image_normalized = np.clip(shifted_image_normalized, 0, 1)
    # Convert back to the original scale (0-255)
    shifted_image = (shifted_image_normalized * 255).astype(np.uint8)
    shifted_img = Image.fromarray(shifted_image)
    return shifted_img

def HSV_colorShift(image_path, target_color, shift_percentage):
    img = Image.open(image_path)
    # Convert the image to a NumPy array
    img_array = np.array(img) / 255.0  # Normalize to the range [0, 1]
    # Convert RGB to HSV color space
    img_hsv = rgb2hsv(img_array)
    # Shift the hue component towards red
    img_hsv[:,:,0] = (img_hsv[:,:,0] + shift_percentage) % 1.0
    # Convert back to RGB color space
    shifted_image = hsv2rgb(img_hsv)
    # Clip values to the valid range [0, 1]
    shifted_image = np.clip(shifted_image, 0, 1)
    # Create a new Pillow image from the shifted NumPy array
    shifted_img = Image.fromarray((shifted_image * 255).astype(np.uint8))
    return shifted_img


image_path = "../testImages/testRainbow/rainbow.jpg"  # Replace with the actual path to your image
target_color = [255, 0, 0]  # Red color
shift_percentage = 0.2  # Adjust as needed

original_image = Image.open(image_path)
shifted_image = HSV_colorShift(image_path, target_color, shift_percentage)

# Plot the original and shifted images side by side
fig, axes = plt.subplots(1, 2, figsize=(10, 5))

# Plot the original image
axes[0].imshow(np.array(original_image))
axes[0].set_title('Original Image')
axes[0].axis('off')

# Plot the shifted image
axes[1].imshow(np.array(shifted_image))
axes[1].set_title('Shifted Image')
axes[1].axis('off')

# Display the plot
plt.show()
