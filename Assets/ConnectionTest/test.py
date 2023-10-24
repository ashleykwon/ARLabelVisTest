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


# b_path = "./background.jpg"
# # b_w_l_path = "./backgroundAndLabel.jpg"
# b_w_l_path = "./test_backgroundAndLabel.jpg"
# labelMask_path = "./test_mask.jpg"
# backgroundImg = np.asarray(Image.open(b_path))
# backgroundAndLabelImg = np.asarray(Image.open(b_w_l_path))

# Test images downloaded from online sources
# b_path = "./testImg2/test2.jpg"
# # b_w_l_path = "./testImg2/test2AndLabel.jpg"
# b_w_l_path = "./testImg2/blurred_adam_lr0.08.jpg"
# labelMask_path = "./testImg2/test2AndLabel_mask.jpg"
b_path = "./testGrey/greyBars.jpg"
b_w_l_path = "./testGrey/blurredBG_lr0.08.jpg"
labelMask_path = "./testGrey/greyBarsMask.jpg"
labelMaskImg = (1/255)*np.asarray(Image.open(labelMask_path))


labelMaskOrigAsTensor = lpips.im2tensor(lpips.load_image(labelMask_path))
labelMaskAsTensor = lpips.im2tensor(lpips.load_image(labelMask_path)) > 0
# Convert the images to tensors
backgroundImgAsTensor = lpips.im2tensor(lpips.load_image(b_path))
backgroundAndLabelImgAsTensor = lpips.im2tensor(lpips.load_image(b_w_l_path))
height = backgroundImgAsTensor.size(2)
width = backgroundImgAsTensor.size(3)
numPixels = height * width  # 512*1024 for AR screenshots


maskOrigFlat = labelMaskOrigAsTensor.view(1,3,-1) #[1,3,524288], rgb values
maskFlat = labelMaskAsTensor.view(1,3,-1) #[1,3,524288], true or false values
imageFlat = backgroundAndLabelImgAsTensor.view(1,3,-1)

backgroundImgAsTensor = scipy.ndimage.gaussian_filter(backgroundImgAsTensor, sigma=(0, 0, 30, 30))
backgroundImgAsTensor = torch.from_numpy(backgroundImgAsTensor)
pred_img = lpips.tensor2im(backgroundImgAsTensor.data)

# ---------------------Try blurring the label-------------------------------------------
full_img = cv2.imread(b_w_l_path)
mask = (1/255)*cv2.imread(labelMask_path) 

blurred_label = full_img
blurred_label[mask > 0.5] = scipy.ndimage.gaussian_filter(full_img, sigma=(3, 3, 0))[mask > 0.5]
combined = full_img
combined[mask > 0.5] = blurred_label[mask > 0.5]
# output_img =  lpips.tensor2im(combined.data)

output_path = "./testGrey/blurredLabel_lr0.08.jpg"
cv2.imwrite(output_path, combined)
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
