# 1. Library imports
import torch
import io, json
import base64
import PIL.Image as Image
import numpy as np
import lpips
import torchvision
from torch.autograd import Variable
from matplotlib import pyplot as plt # for debugging purposes

loss_fn = lpips.LPIPS(net='vgg',version=0.1) #changed from alex to vgg based on this documentation: https://pypi.org/project/lpips/#b-backpropping-through-the-metric
loss_fn.cuda()

# Test images downloaded from online sources
b_path = "./testImg2/test2.jpg"
b_w_l_path = "./testImg2/test2AndLabel.jpg"
labelMask_path = "./testImg2/test2AndLabel_mask.jpg"
labelMaskImg = (1/255)*np.asarray(Image.open(labelMask_path))

# # Screenshots from the AR headset
# b_path = "./background.jpg"
# # b_w_l_path = "./backgroundAndLabel.jpg"
# b_w_l_path = "./test_backgroundAndLabel.jpg"
# labelMask_path = "./test_mask.jpg"
# labelMaskImg = (1/255)*np.asarray(Image.open(labelMask_path))

# # Check the size of the image before backpropping
# width, height = Image.open(b_w_l_path).size
# print(f"Image width: {width}px, Image height: {height}px")

MAX_ITER = 1000
costList = []

# # --------------Load images and convert them to tensors-----------------------------------------------------
# Convert the images to tensors
backgroundImgAsTensor = lpips.im2tensor(lpips.load_image(b_path))
backgroundAndLabelImgAsTensor = lpips.im2tensor(lpips.load_image(b_w_l_path))
height = backgroundImgAsTensor.size(2)
width = backgroundImgAsTensor.size(3)
numPixels = height * width  # 512*1024 for AR screenshots
# print("image size: ", backgroundImgAsTensor.size())

# The predicted image
# pred = Variable(backgroundAndLabelImgAsTensor, requires_grad=True)


# # --------------Separate label and background and set different requires_grad values for them -> only gradient descent on label pixels-------------------------
# create label mask (boolean mask with True where label pixels are)
labelMaskAsTensor = lpips.im2tensor(lpips.load_image(labelMask_path)) > 0
numLabelPixels = labelMaskAsTensor[0,0].sum().item() # number of label pixels

# Flatten image and mask to apply the mask
imageFlat = backgroundAndLabelImgAsTensor.view(1,3,-1) #[1,3,numPixels]
maskFlat = labelMaskAsTensor.view(1,3,-1) #[1,3,numPixels]

# Separate background and label pixels
labelFlat = imageFlat[:, maskFlat[0]] #[1,numLabelPixels]
backgroundFlat = imageFlat[ :, ~maskFlat[0]]

# Set them to torch variables for back-propagation
labelVar = Variable(labelFlat, requires_grad=True)
backgroundVar = Variable(imageFlat, requires_grad=False)

# Get indices of label pixels as a tuple of d tensors where d is the number of dimensions -> ([x1, x2, x3, ..., xn], [y1, y2, y3, ..., yn], ...)
labelIndices = torch.nonzero(maskFlat, as_tuple=True) # Here, d = 3 and n = 3*numLabelPixels

# # --------------Set optimizer and start iterating--------------------------------------------------------------------------
# stochastic gradient descent-based optimizer
# optimizer = torch.optim.SGD([torch.from_numpy(backgroundAndLabelImg)], lr=0.01, momentum=0.9)
# optimizer = torch.optim.SGD([pred], lr=0.05, momentum=0.9)

# Adam optimizer, original lr=1e-4
optimizer = torch.optim.Adam([labelVar,], lr=0.2, betas=(0.9, 0.999))

distanceThreshold = 0.23

# Optimize
for iter in range(MAX_ITER): 
    # if type(backgroundAndLabelImg) == torch.Tensor:
    #     backgroundAndLabelImg = backgroundAndLabelImg.numpy()

    # Apply the boolean mask for label pixels
    # maskedLabelTensor = torch.mul(pred, labelMaskAsTensor)
    # maskedLabelTensor = pred.where(torch.logical_not(labelMaskAsTensor), -1) # don't use this # sets all non-label pixels to -1

    # initialize to the origianl full image (does not matter what the pixels in label region are bc they will be overwritten later)
    full_img = imageFlat  # torch.Size([1, 3, 524288])

    # Overwrite the label pixels using the updated results
    full_img.index_put_(labelIndices, labelVar.reshape(labelVar.size()[1]))  # labelVar size: torch.Size([1, numLabelPixels]) -> reshape it to [numLabelPixels] to fit in index_put_()
    full_img = full_img.view(1,3,height,width) # restore its shape to match the original image's shape
    full_img.data = torch.clamp(full_img.data, -1, 1)

    # Calculate the LPIPS loss: LPIPS distance between the current backgroundAndLabelImage and the backgroundImage
    LPIPSLoss = loss_fn.forward(full_img.cuda(), backgroundImgAsTensor.cuda())

    # Negate the loss to make the image more and more different from the original one
    neg_loss = - LPIPSLoss
    
    # Clear the gradient for a new calculation
    optimizer.zero_grad()

    # Do backpropagation based on the LPIPS loss above
    neg_loss.backward(retain_graph=True)

    # append the current loss term to costList to plot them later
    # costList.append(LPIPSLoss[0][0][0][0].item())

    optimizer.step() # based on backpropagation implemented in lpips_loss.py

    # Print out losses/distances
    if iter % 100 == 0:
        print('iter %d, dist %.3g' % (iter, LPIPSLoss.view(-1).data.cpu().numpy()[0]))        

    # Save the output image
    if (iter == MAX_ITER - 1): 
        print('Reached the last iteration')
        full_img.data = torch.clamp(full_img.data, -1, 1)
        pred_img = lpips.tensor2im(full_img.data)
        print(type(pred_img))
        output_path = "./testImg2/final_result_adam_lr0.2.jpg"
        # output_path = "./final_result_adam_lr0.08.jpg"
        Image.fromarray(pred_img).save(output_path)
        break

        #  # Check the size of the image after backpropping
        #  width, height = Image.open(output_path).size
        #  print(f"Image width: {width}px, Image height: {height}px")