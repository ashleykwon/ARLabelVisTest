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
from tqdm import tqdm

loss_fn = lpips.LPIPS(net='vgg',version=0.1) #changed from alex to vgg based on this documentation: https://pypi.org/project/lpips/#b-backpropping-through-the-metric
# loss_fn.cuda()

# b_path = "./test2.jpg"
# b_w_l_path = "./test2AndLabel.jpg"
# backgroundImg = np.asarray(Image.open(b_path))
# backgroundAndLabelImg = np.asarray(Image.open(b_w_l_path))


b_path = "./background.jpg"
# b_w_l_path = "./backgroundAndLabel.jpg"
b_w_l_path = "./test_backgroundAndLabel.jpg"
labelMask_path = "./test_mask.jpg"
backgroundImg = np.asarray(Image.open(b_path))
backgroundAndLabelImg = np.asarray(Image.open(b_w_l_path))
labelMaskImg = (1/255)*np.asarray(Image.open(labelMask_path))

# # Check the size of the image before backpropping
# width, height = Image.open(b_w_l_path).size
# print(f"Image width: {width}px, Image height: {height}px")

MAX_ITER = 1000
costList = []

# Convert the image with the background only to a tensor
backgroundImgAsTensor = lpips.im2tensor(lpips.load_image(b_path))

# The predicted image
pred = Variable(lpips.im2tensor(lpips.load_image(b_w_l_path)), requires_grad=True)

# label mask (boolean mask with True where label pixels are)
labelMaskAsTensor = lpips.im2tensor(lpips.load_image(labelMask_path)) > 0

# stochastic gradient descent-based optimizer
# optimizer = torch.optim.SGD([torch.from_numpy(backgroundAndLabelImg)], lr=0.01, momentum=0.9)
# optimizer = torch.optim.SGD([pred], lr=0.05, momentum=0.9)

# Adam optimizer, original lr=1e-4
optimizer = torch.optim.Adam([pred,], lr=0.01, betas=(0.9, 0.999))

distanceThreshold = 0.23

# Optimize
for iter in tqdm(range(MAX_ITER)):
    if type(backgroundAndLabelImg) == torch.Tensor:
        backgroundAndLabelImg = backgroundAndLabelImg.numpy()

    # Apply the boolean mask for label pixels
    # maskedLabelTensor = torch.mul(pred, labelMaskAsTensor)
    maskedLabelTensor = pred.where(labelMaskAsTensor, -1) # sets all non-label pixels to 1

    # Calculate the LPIPS loss: -1 * LPIPS distance between the current backgroundAndLabelImage and the backgroundImage
    LPIPSLoss = loss_fn.forward(maskedLabelTensor.cuda(), backgroundImgAsTensor.cuda())
    neg_loss = - LPIPSLoss
    neg_loss = - pixel_distance_loss(maskedLabelTensor)
    
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
    if (iter == MAX_ITER - 1): 
        print('Reached the last iteration')
        pred.data = torch.clamp(pred.data, -1, 1)
        pred_img = lpips.tensor2im(pred.data)
        print(type(pred_img))
        output_path = "./final_result_adam.jpg"
        Image.fromarray(pred_img).save(output_path)
        break

        #  # Check the size of the image after backpropping
        #  width, height = Image.open(output_path).size
        #  print(f"Image width: {width}px, Image height: {height}px")


def pixel_distance_loss(maskedLabel, weigth = 1.0):
  image = maskedLabel.permute(2, 3, 1, 0).detach().numpy()
  image = image.squeeze()
  # plt.imshow(image)
  image = np.where(image < 0, 0, image)

  diff = np.sum(np.abs(np.diff(image, axis=0))) + np.sum(np.abs(np.diff(image, axis=1)))
  diff_weight = weight * diff / image.size

  x = torch.tensor([diff_weight])
  return x.cuda()
