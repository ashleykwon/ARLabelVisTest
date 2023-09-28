# 1. Library imports
import torch
import io, json
import base64
import PIL.Image as Image
import numpy as np
import lpips
import torchvision

loss_fn = lpips.LPIPS(net='vgg',version=0.1) #changed from alex to vgg based on this documentation: https://pypi.org/project/lpips/#b-backpropping-through-the-metric
loss_fn.cuda()

b_path = "./background.jpg"
b_w_l_path = "./backgroundAndLabel.jpg"
backgroundImg = np.asarray(Image.open(b_path))
backgroundAndLabelImg = np.asarray(Image.open(b_w_l_path))

MAX_ITER = 1000
costList = []


class Model(torch.nn.Module):

    def __init__(self):
        super(Model, self).__init__()

    def forward(self, x): # returns the input image, because we're directly modifying the input image
        return x

# Initialize the model (this is just to connect the input image to the gradient descent function)
model = Model()

# stochastic gradient descent-based optimizer
optimizer = torch.optim.SGD([torch.from_numpy(backgroundAndLabelImg)], lr=0.01, momentum=0.9)

# Convert the image with the background only to a tensor
backgroundImgAsTensor = lpips.im2tensor(backgroundImg)

# Optimize
for iter in range(MAX_ITER): 
    if type(backgroundAndLabelImg) == torch.Tensor:
        backgroundAndLabelImg = backgroundAndLabelImg.numpy()

    # This should just be the output image from the previous iteration
    backgroundAndLabelImg = model(torch.from_numpy(backgroundAndLabelImg)).numpy()

    # Calculate the LPIPS loss (1 - LPIPS distance between the current backgroundAndLabelImage and the backgroundImage)
    LPIPSLoss = 1 - loss_fn.forward(lpips.im2tensor(backgroundAndLabelImg).cuda(), backgroundImgAsTensor.cuda())
    
    # Clear the gradient for a new calculation
    optimizer.zero_grad()

    # Do backpropagation based on the LPIPS loss above
    LPIPSLoss.backward()

    # append the current loss term to costList to plot them later
    costList.append(LPIPSLoss[0][0][0][0].item())

    optimizer.step() # based on backpropagation implemented in lpips_loss.py


# Save the output image
if type(backgroundAndLabelImg) == torch.Tensor:
        backgroundAndLabelImg = backgroundAndLabelImg.numpy()
finalBackgroundAndLabel = Image.fromarray(backgroundAndLabelImg)
finalBackgroundAndLabel.save("finalBackgroundAndLabel.jpg")