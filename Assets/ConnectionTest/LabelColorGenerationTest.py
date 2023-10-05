# 1. Library imports
import torch
import io, json
import base64
import PIL.Image as Image
import numpy as np
import lpips
import torchvision
from torch.autograd import Variable

loss_fn = lpips.LPIPS(net='vgg',version=0.1) #changed from alex to vgg based on this documentation: https://pypi.org/project/lpips/#b-backpropping-through-the-metric
loss_fn.cuda()

b_path = "./test2.jpg"
b_w_l_path = "./test2AndLabel.jpg"
backgroundImg = np.asarray(Image.open(b_path))
backgroundAndLabelImg = np.asarray(Image.open(b_w_l_path))

# # Check the size of the image before backpropping
# width, height = Image.open(b_w_l_path).size
# print(f"Image width: {width}px, Image height: {height}px")

MAX_ITER = 1000
costList = []


class Model(torch.nn.Module):

    def __init__(self):
        super(Model, self).__init__()

    def forward(self, x): # returns the input image, because we're directly modifying the input image
        return x

# Initialize the model (this is just to connect the input image to the gradient descent function)
model = Model()



# Convert the image with the background only to a tensor
backgroundImgAsTensor = lpips.im2tensor(backgroundImg)

# The predicted image
pred = Variable(lpips.im2tensor(lpips.load_image(b_w_l_path)), requires_grad=True)

# stochastic gradient descent-based optimizer
# optimizer = torch.optim.SGD([torch.from_numpy(backgroundAndLabelImg)], lr=0.01, momentum=0.9)
# optimizer = torch.optim.SGD([pred], lr=0.05, momentum=0.9)

# Adam optimizer, original lr=1e-4
optimizer = torch.optim.Adam([pred,], lr=0.01, betas=(0.9, 0.999))

# Optimize
for iter in range(MAX_ITER): 
    if type(backgroundAndLabelImg) == torch.Tensor:
        backgroundAndLabelImg = backgroundAndLabelImg.numpy()

    # This should just be the output image from the previous iteration
    # backgroundAndLabelImg = model(torch.from_numpy(backgroundAndLabelImg)).numpy()

    # Calculate the LPIPS loss (1 - LPIPS distance between the current backgroundAndLabelImage and the backgroundImage)
    # LPIPSLoss = 1 - loss_fn.forward(lpips.im2tensor(backgroundAndLabelImg).cuda(), backgroundImgAsTensor.cuda())
    LPIPSLoss = loss_fn.forward(pred.cuda(), backgroundImgAsTensor.cuda())
    neg_loss = - LPIPSLoss
    
    # Clear the gradient for a new calculation
    optimizer.zero_grad()

    # Do backpropagation based on the LPIPS loss above
    neg_loss.backward()

    # append the current loss term to costList to plot them later
    costList.append(LPIPSLoss[0][0][0][0].item())

    optimizer.step() # based on backpropagation implemented in lpips_loss.py

    # Print out losses/distances
    if iter % 100 == 0:
         print('iter %d, dist %.3g' % (iter, LPIPSLoss.view(-1).data.cpu().numpy()[0]))

    # Save the output image
    if iter == MAX_ITER - 1:
         print('Reached the last iteration')
         pred.data = torch.clamp(pred.data, -1, 1)
         pred_img = lpips.tensor2im(pred.data)
         print(type(pred_img))
         output_path = "./test2_adam.jpg"
         Image.fromarray(pred_img).save(output_path)

        #  # Check the size of the image after backpropping
        #  width, height = Image.open(output_path).size
        #  print(f"Image width: {width}px, Image height: {height}px")



# Save the output image
# if type(backgroundAndLabelImg) == torch.Tensor:
#         backgroundAndLabelImg = backgroundAndLabelImg.numpy()
# finalBackgroundAndLabel = Image.fromarray(backgroundAndLabelImg)
# finalBackgroundAndLabel.save("finalBackgroundAndLabel.jpg")