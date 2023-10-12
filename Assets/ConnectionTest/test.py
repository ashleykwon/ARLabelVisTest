# 1. Library imports
import torch
import io, json
import base64
import PIL.Image as Image
import numpy as np
import lpips
import torchvision
from torch.autograd import Variable


b_path = "./background.jpg"
# b_w_l_path = "./backgroundAndLabel.jpg"
b_w_l_path = "./test_backgroundAndLabel.jpg"
labelMask_path = "./test_mask.jpg"
backgroundImg = np.asarray(Image.open(b_path))
backgroundAndLabelImg = np.asarray(Image.open(b_w_l_path))

labelMaskImg = (1/255)*np.asarray(Image.open(labelMask_path))
labelMaskOrigAsTensor = lpips.im2tensor(lpips.load_image(labelMask_path))
labelMaskAsTensor = lpips.im2tensor(lpips.load_image(labelMask_path)) > 0
backgroundAndLabelImgAsTensor = lpips.im2tensor(lpips.load_image(b_w_l_path))

maskOrigFlat = labelMaskOrigAsTensor.view(1,3,-1) #[1,3,524288], rgb values
maskFlat = labelMaskAsTensor.view(1,3,-1) #[1,3,524288], true or false values
imageFlat = backgroundAndLabelImgAsTensor.view(1,3,-1)


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


a = torch.zeros(2, 3)
a.index_put_([torch.tensor([1, 0]), torch.tensor([1, 1])], torch.tensor(1.))
print(a)