# 1. Library imports
import torch
import io, json
import base64
import PIL.Image as Image
import numpy as np
import lpips
import torchvision
import scipy
import cv2
from torch.autograd import Variable
from skimage import color
from torchmetrics.image import StructuralSimilarityIndexMeasure
from colormath.color_objects import LabColor
from colormath.color_diff import delta_e_cie1976

def delta_e_cie76(lab1, lab2):
    l1, a1, b1 = lab1
    l2, a2, b2 = lab2
    deltaL = l2-l1
    deltaB = b2-b1
    deltaA = a2-a1
    return np.sqrt(deltaA**2 + deltaB**2 + deltaL**2)

def avg_delta_e(image):
    image = image.numpy()
    image = ((image + 1) / 2) 
    print("image range: ", np.min(image), np.max(image))
    image = image.reshape(3,-1).T # reshape the image to be (3, numPixels)
    # print("image after reshaping: ", image.shape)
    image_lab = color.rgb2lab(image) # all colors are in lab for image_lab
    mean_lab = np.mean(image_lab, axis = 0)
    print("image_lab shape: ", image_lab.shape)
    print("mean_lab shape: ", mean_lab.shape)
    print("mean_lab: ", mean_lab)
    deltaE = [delta_e_cie76(mean_lab, lab) for lab in image_lab]
    mean_deltaE = np.mean(deltaE)
    print("mean_deltaE: ", mean_deltaE)
    return mean_deltaE

# b_path = "./background.jpg"
# # b_w_l_path = "./backgroundAndLabel.jpg"
# b_w_l_path = "./test_backgroundAndLabel.jpg"
# labelMask_path = "./test_mask.jpg"
# backgroundImg = np.asarray(Image.open(b_path))
# backgroundAndLabelImg = np.asarray(Image.open(b_w_l_path))

# Test images downloaded from online sources
# b_path = "./testImg2/test2.jpg"
# b_w_l_path = "./testImg2/test2AndLabel.jpg"
# # b_w_l_path = "./testImg2/blurred_adam_lr0.08.jpg"
# labelMask_path = "./testImg2/test2AndLabel_mask.jpg"
# b_path = "./testRiver/river.jpg"
# b_w_l_path = "./testRiver/riverAndLabel.jpg"
# labelMask_path = "./testRiver/riverMask.jpg"
b_path = "./testCurry/curry.jpg"
b_w_l_path = "./testCurry/curryAndLabel.jpg"
labelMask_path = "./testCurry/curryMask.jpg"
labelMaskImg = (1/255)*np.asarray(Image.open(labelMask_path))


labelMaskOrigAsTensor = lpips.im2tensor(lpips.load_image(labelMask_path))
labelMaskAsTensor = lpips.im2tensor(lpips.load_image(labelMask_path)) > 0
# Convert the images to tensors
backgroundImgAsTensor = lpips.im2tensor(lpips.load_image(b_path))
backgroundAndLabelImgAsTensor = lpips.im2tensor(lpips.load_image(b_w_l_path)) # range -1 to 1
height = backgroundImgAsTensor.size(2)
width = backgroundImgAsTensor.size(3)
numPixels = height * width  # 512*1024 for AR screenshots


maskOrigFlat = labelMaskOrigAsTensor.view(1,3,-1) #[1,3,524288], rgb values
maskFlat = labelMaskAsTensor.view(1,3,-1) #[1,3,524288], true or false values
imageFlat = backgroundAndLabelImgAsTensor.view(1,3,-1)

labelFlat = imageFlat[:, maskFlat[0]]
print("shape of labelFlat:", labelFlat.size())
labelFlat2 = imageFlat[:, :, maskFlat[0][0]]
print("shape of labelFlat2:", labelFlat2.size())
print("delta e for label: ", avg_delta_e(labelFlat2))







# sigma = 15
# backgroundImgAsTensor = scipy.ndimage.gaussian_filter(backgroundImgAsTensor, sigma=(0, 0, sigma, sigma), radius=None)
# backgroundImgAsTensor = torch.from_numpy(backgroundImgAsTensor)
# pred_img = lpips.tensor2im(backgroundImgAsTensor.data)
# output_path = "./tests/river_blur_r15.jpg"
# Image.fromarray(pred_img).save(output_path)

# # ---------------------Try blurring the label-------------------------------------------
# full_img = cv2.imread(b_w_l_path)
# mask = (1/255)*cv2.imread(labelMask_path) 

# blurred_label = full_img
# blurred_label[mask > 0.5] = scipy.ndimage.gaussian_filter(full_img, sigma=(3, 3, 0))[mask > 0.5]
# combined = full_img
# combined[mask > 0.5] = blurred_label[mask > 0.5]
# # output_img =  lpips.tensor2im(combined.data)

# output_path = "./testGrey/blurredLabel_lr0.08.jpg"
# cv2.imwrite(output_path, combined)

# cv2.imwrite("./tests/test_mask.jpg", mask)
# Image.fromarray(output_img).save(output_path)



"""
# Debugging for index_put
# Get indices of label pixels as a tuple of d tensors where d is the number of dimensions -> ([x1, x2, x3, ...], [y1, y2, y3, ...], [z1, z2, z3, ...])
labelIndices = torch.nonzero(maskFlat, as_tuple=True) # labelMaskAsTensor size: [1, 3, 512, 1024]
print("----------Label Indices-------------")
print("labelIndices size: ", labelIndices[0].size()) # labelIndices size:  torch.Size([15519])
print("labelIndices length: ", len(labelIndices))
print((labelIndices))


labelFlat = maskOrigFlat[:, maskFlat[0]]
print("==== Random pixel from maskOrigFlat: ", maskOrigFlat[0][0][0])
print("==== Random pixel from maskOrigFlat: ", maskOrigFlat[0][0][116189])

# initialize it to the origianl full image (does not matter what the label pixels are bc they will be overwritten later)
full_img = imageFlat  # torch.Size([1, 3, 524288])
# Overwrite the label pixels using the propagated results
# labelVar size:  torch.Size([1, 3, 5173])
print(labelFlat.reshape(15519))
full_img.index_put_(labelIndices, torch.tensor(-1.))  
full_img = full_img.view(1,3,512,1024)

# print("full img type: ", type(full_img))
pred_img = lpips.tensor2im(full_img.data)
output_path = "./test001.jpg"
Image.fromarray(pred_img).save(output_path)
"""
