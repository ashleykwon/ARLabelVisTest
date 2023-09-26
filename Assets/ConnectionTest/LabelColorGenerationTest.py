# 1. Library imports
import torch
import io, json
import base64
import PIL.Image as Image
import numpy as np
import lpips
import torchvision

loss_fn = lpips.LPIPS(net='alex',version=0.1)
loss_fn.cuda()

b_path = "./background.jpg"
b_w_l_path = "./backgroundAndLabel.jpg"
backgroundImg = np.asarray(Image.open(b_path))
backgroundAndLabelImg = np.asarray(Image.open(b_w_l_path))


# custom loss function based on LPIPS distance and difference between background and background+label
def loss(backgroundAndLabelImg, backgroundImg):
    # convert the background image and the background+label image to tensors
    background = lpips.im2tensor(backgroundImg)
    backgroundAndLabel = lpips.im2tensor(backgroundAndLabelImg)
    
    # get the LPIPS loss
    lpipsLoss = -1 * loss_fn.forward(backgroundAndLabel.cuda(), background.cuda())[0][0][0][0].item() 

    # get the contrast between each label pixel and its neighboring background pixels
    blurredBackground = torchvision.transforms.GaussianBlur(5, sigma=(0.1, 2.0))(lpips.im2tensor(backgroundImg)).T.numpy().reshape(backgroundAndLabelImg.shape)
    grayscaleBackgroundImg = (1/3)*np.absolute(blurredBackground[:, :, 0] + blurredBackground[:, :, 1] + backgroundImg[:, :, 2])
    grayscaleBackgroundAndLabelImg = (1/3)*np.absolute(backgroundAndLabelImg[:, :, 0] + backgroundAndLabelImg[:, :, 1] + backgroundAndLabelImg[:, :, 2])
    contrastDifferenceMatrix = np.absolute(np.subtract(grayscaleBackgroundImg, grayscaleBackgroundAndLabelImg))
    contrastLoss = -1 * np.sum(contrastDifferenceMatrix)/backgroundImg.size # average contrast

    # sum up the lpips and contrast losses
    return lpipsLoss + contrastLoss

# Add the optimizer function here and send the resulting array of pixels back to the headset
loss(backgroundAndLabelImg, backgroundImg)

# how should we modify the image based on these error terms? 
# what other error terms should we add?
# should we use another neural network, along with LPIPS, in the label color generation process?