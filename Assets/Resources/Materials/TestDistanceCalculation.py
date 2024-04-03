import numpy as np
from PIL import Image
import cv2

def great_circle_distance(target_pixel, source_pixel, image_width, image_height):
    # Assuming the image represents a full 360 degree view of the sphere
    # Convert pixel coordinates to longitude and latitude
    lon1, lat1 = pixel_to_coord(target_pixel, image_width, image_height)
    lon2, lat2 = pixel_to_coord(source_pixel, image_width, image_height)
    
    # Convert degrees to radians
    lon1, lat1, lon2, lat2 = map(np.radians, [lon1, lat1, lon2, lat2])
    
    # Haversine formula
    dlon = lon2 - lon1
    dlat = lat2 - lat1
    a = np.sin(dlat/2)**2 + np.cos(lat1) * np.cos(lat2) * np.sin(dlon/2)**2
    c = 2 * np.arcsin(np.sqrt(a))
    
    # Radius of Earth in kilometers (mean radius)
    R = image_width/(2*np.pi)
    distance = R * c
    
    return distance

def pixel_to_coord(pixel, image_width, image_height):
    # Convert pixel position to spherical coordinates
    lon = (pixel[0] / image_width) * 360 - 180
    lat = 90 - (pixel[1] / image_height) * 180
    return lon, lat


# background_filepath = './360Images/HotelParanal2D.jpeg'
# background = np.asarray(Image.open(background_filepath))

label_mask_filepath = 'Train_Polygon.jpeg'
label_mask = np.asarray(Image.open(label_mask_filepath))
# labelCenterIdx = np.zeros(2)
# labelCenterCount = 0
# for i in range(label_mask.shape[0]):
#     for j in range(label_mask.shape[1]):
#         currentPixel = label_mask[i,j,:]
#         if currentPixel[0] == 255 and currentPixel[1] == 0 and currentPixel[2] == 0:
#             labelCenterIdx[0] = i
#             labelCenterIdx[1] = j
#             labelCenterCount += 1
# print(labelCenterCount)

labelCenterIdx = [2530, 10571]
# [3366, 6870]
distances = np.zeros((label_mask.shape[0], label_mask.shape[1]))
for i in range(label_mask.shape[0]):
    for j in range(label_mask.shape[1]):
        distances[i,j] = great_circle_distance(labelCenterIdx, [i,j], label_mask.shape[0], label_mask.shape[1])
distancesMask = distances > 1000
distances[distancesMask] = 0
# distanceAsImage = Image.fromarray(distances) # max = 3035.0, min = 0.0
cv2.imwrite("Train_Polygon2D_BG.jpeg", distances)
# distanceAsImage.save("HeptTrue2D_BG.jpeg")

