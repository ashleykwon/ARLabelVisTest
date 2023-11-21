# Library imports
import torch
import io, json
import base64
import PIL.Image as Image
import numpy as np
import lpips
import cv2
import scipy
import os
import torchvision
import argparse
import datetime
from torch.autograd import Variable
from skimage import color

from ssim import ssim, ms_ssim, SSIM, MS_SSIM
from torchmetrics.image import StructuralSimilarityIndexMeasure
from torchmetrics.image import MultiScaleStructuralSimilarityIndexMeasure
from torchmetrics.image import PeakSignalNoiseRatio
from colormath.color_objects import LabColor
from colormath.color_diff import delta_e_cie1976
from matplotlib import pyplot as plt # for debugging purposes

def rgb_to_lab(srgb): # srgb: and image of size [..., 3]
	srgb_pixels = torch.reshape(srgb, [-1, 3]).cuda()

	linear_mask = (srgb_pixels <= 0.04045).type(torch.FloatTensor).cuda()
	exponential_mask = (srgb_pixels > 0.04045).type(torch.FloatTensor).cuda()
	rgb_pixels = (srgb_pixels / 12.92 * linear_mask) + (((srgb_pixels + 0.055) / 1.055) ** 2.4) * exponential_mask
	
	rgb_to_xyz = torch.tensor([
				#    X        Y          Z
				[0.412453, 0.212671, 0.019334], # R
				[0.357580, 0.715160, 0.119193], # G
				[0.180423, 0.072169, 0.950227], # B
			]).type(torch.FloatTensor).cuda()
	
	xyz_pixels = torch.mm(rgb_pixels, rgb_to_xyz)
	
	# XYZ to Lab
	xyz_normalized_pixels = torch.mul(xyz_pixels, torch.tensor([1/0.950456, 1.0, 1/1.088754]).type(torch.FloatTensor).cuda())

	epsilon = 6.0/29.0

	linear_mask = (xyz_normalized_pixels <= (epsilon**3)).type(torch.FloatTensor).cuda()

	exponential_mask = (xyz_normalized_pixels > (epsilon**3)).type(torch.FloatTensor).cuda()

	fxfyfz_pixels = (xyz_normalized_pixels / (3 * epsilon**2) + 4.0/29.0) * linear_mask + ((xyz_normalized_pixels+0.000001) ** (1.0/3.0)) * exponential_mask
	# convert to lab
	fxfyfz_to_lab = torch.tensor([
		#  l       a       b
		[  0.0,  500.0,    0.0], # fx
		[116.0, -500.0,  200.0], # fy
		[  0.0,    0.0, -200.0], # fz
	]).type(torch.FloatTensor).cuda()
	lab_pixels = torch.mm(fxfyfz_pixels, fxfyfz_to_lab) + torch.tensor([-16.0, 0.0, 0.0]).type(torch.FloatTensor).cuda()
	#return tf.reshape(lab_pixels, tf.shape(srgb))
	return torch.reshape(lab_pixels, srgb.shape)


def delta_e_cie76(lab1, lab2):
    l1, a1, b1 = lab1
    l2, a2, b2 = lab2
    deltaL = l2-l1
    deltaB = b2-b1
    deltaA = a2-a1
    return torch.sqrt(deltaA**2 + deltaB**2 + deltaL**2)

# def avg_delta_e(image):
#     image = image.detach().numpy()
#     image = ((image + 1) / 2) 
#     # print("image range: ", np.min(image), np.max(image))
#     image = image.reshape(3,-1).T # reshape the image to be (3, numPixels)
#     image = torch.tensor(image).cuda()
#     # image_lab = color.rgb2lab(image) # all colors are in lab for image_lab
#     image_lab = rgb_to_lab(image)
#     mean_lab = torch.mean(image_lab, axis = 0).cuda()
#     # print("mean_lab: ", mean_lab)
#     deltaE = [delta_e_cie76(mean_lab, lab) for lab in image_lab]
#     mean_deltaE = torch.mean(deltaE).cuda()
#     # print("mean_deltaE: ", mean_deltaE)
#     return mean_deltaE


class DeltaELoss(torch.nn.Module):
    def __init__(self):
        super(DeltaELoss, self).__init__()
    
    def forward(self, image):
        # image = image.detach().numpy()
        image = 0.5*torch.add(image, torch.ones(image.shape))
        # ((image + 1) / 2)  # range [0,1]
        # print("image range: ", np.min(image), np.max(image))
        # image = torch.resh
        # image.reshape(3,-1).T # reshape the image to be (3, numPixels)
        # image = torch.tensor(image)

        distanceAsTensor = torch.square(torch.sub(image, torch.ones(image.shape)*torch.mean(image)))

        # mean_deltaE = torch.mean(image) # try just taking the mean without using other functions
        # # image_lab = color.rgb2lab(image) # all colors are in lab for image_lab
        # image_lab = rgb_to_lab(image) # This step takes a long time
        # mean_lab = torch.mean(image_lab, axis = 0)
        # # print("mean_lab: ", mean_lab)
        # deltaE = torch.tensor([delta_e_cie76(mean_lab, lab) for lab in image_lab])
        # mean_deltaE = torch.mean(deltaE)
        # # print("mean_deltaE: ", mean_deltaE)
        return torch.mean(distanceAsTensor)



if __name__ == '__main__':
    parser = argparse.ArgumentParser(description='Label color generation')
    parser.add_argument('--lr', type=float, default=0.08, help='Learning rate')
    parser.add_argument('--sigma', type=float, default=10, help='Gaussian Blur sigma')
    parser.add_argument('--itr', type=int, default=200, help='Number of iterations')
    parser.add_argument('--image_paths', nargs='+',         default=[
                                                                    './testCurry/curry.jpg'          
                                                                     ,'./testRiver/river.jpg'
                                                                     ,'./testRiver/river_white.jpg'
                                                                     ,'./testSingleColor/blue.jpg'
                                                                     ,'./testRainbow/rainbow.jpg' 
                                                                     ,'./testSingleColor/blueRG.jpg'

                                                                    ], 
                                                                     help='Paths to input images')
    parser.add_argument('--imageAndLabel_paths', nargs='+', default=[
                                                                    './testCurry/curryAndLabel_white.jpg'
                                                                     ,'./testRiver/riverAndLabel.jpg'
                                                                     ,'./testRiver/riverAndLabel_white.jpg'
                                                                     ,'./testSingleColor/blueAndLabel.jpg'
                                                                     ,'./testRainbow/rainbowAndLabel.jpg'
                                                                     ,'./testSingleColor/blueAndRGLabel.jpg'
                                                                    ], 
                                                                     help='Paths to input images with labels')
    parser.add_argument('--mask_paths', nargs='+',          default=[
                                                                    './testCurry/curryMask.jpg'        
                                                                     ,'./testRiver/riverMask.jpg'
                                                                     ,'./testRiver/riverMask.jpg'
                                                                     ,'./testSingleColor/mask.jpg'
                                                                     ,'./testRainbow/rainbowMask.jpg'
                                                                     ,'./testSingleColor/blueAndRGLabelMask.jpg'
                                                                    ], 
                                                                     help='Paths to masks for input images')
    parser.add_argument('--blur',  default=False, help='Apply blur to background')
    parser.add_argument('--deltaE',  default=False, help='Add delta E to loss')

    parser.add_argument('--metric', choices=['lpips', 'ssim', 'mssim', 'psnr'], default='lpips', help='Distance calculation method')
    args = parser.parse_args()

    ssim_sigma = 1.5 # default 1.5
    k1 = 0.01 ; k2 = 0.03 # default 0.01, 0.03
    alpha = 1; beta = 1; gamma = 1

    device = 'cuda' if torch.cuda.is_available() else 'cpu'
    if args.metric == 'lpips':
        loss_fn = lpips.LPIPS(net='vgg', version=0.1) #changed from alex to vgg based on this documentation: https://pypi.org/project/lpips/#b-backpropping-through-the-metric
        loss_fn.cuda()
    elif args.metric == 'ssim':
        pass
        # ssim = StructuralSimilarityIndexMeasure(sigma=ssim_sigma, k1=k1, k2=k2).to(device)
    elif args.metric == 'mssim':
        mssim = MultiScaleStructuralSimilarityIndexMeasure().to(device)
    elif args.metric == 'psnr':
        psnr = PeakSignalNoiseRatio().to(device)

    if args.deltaE:
        deltaE_loss = DeltaELoss()


    timestamp = datetime.datetime.now().strftime("%Y-%m-%d_%H-%M-%S")
    log_file_name = f"./log_files/loss_log_{timestamp}.txt"
    log_file = open(log_file_name, "w") # use "a" to append instead of overwritting

    for image_path, imageAndLabel_path, mask_path in zip(args.image_paths, args.imageAndLabel_paths, args.mask_paths):
        b_path = image_path
        b_w_l_path = imageAndLabel_path  # Adjust this as needed
        labelMask_path = mask_path
        labelMaskImg = (1/255) * np.asarray(Image.open(labelMask_path))
    
        sigma = args.sigma

        # Header for log file
        image_name = os.path.splitext(os.path.basename(image_path))[0]  # Extract the base name without the extension
        if args.blur:
            log_file.write(f"-----------{image_name}_blurredBG_sigma{args.sigma}_itr{args.itr}_lr{args.lr}---------------\n")
        else:
            log_file.write(f"-----------{image_name}_unblurredBG_itr{args.itr}_lr{args.lr}---------------\n")

        # # --------------Load images and convert them to tensors---------------------------------------------------------------------------------
        # Convert the images to tensors
        backgroundImgAsTensor = lpips.im2tensor(lpips.load_image(b_path))
        backgroundAndLabelImgAsTensor = lpips.im2tensor(lpips.load_image(b_w_l_path))
        height = backgroundImgAsTensor.size(2)
        width = backgroundImgAsTensor.size(3)
        numPixels = height * width  # 512*1024 for AR screenshots
        # print("numPixels:", numPixels)

        # # --------------Separate label and background and set different requires_grad values for them -> only gradient descent on label pixels-------------------------
        # create label mask (boolean mask with True where label pixels are)
        labelMaskAsTensor = lpips.im2tensor(lpips.load_image(labelMask_path)) > 0
        numLabelPixels = labelMaskAsTensor[0,0].sum().item() # number of label pixels

        # Flatten image and mask to apply the mask
        imageFlat = backgroundAndLabelImgAsTensor.view(1,3,-1) #[1,3,numPixels]
        # print("shape of imageFlat:", imageFlat.size())
        maskFlat = labelMaskAsTensor.view(1,3,-1) #[1,3,numPixels]

        # Separate background and label pixels
        labelFlat = imageFlat[:, maskFlat[0]] #[1,3*numLabelPixels] : collapsed 3 color channels
        labelFlat2 = imageFlat[:, :, maskFlat[0][0]] #[1,3,numLabelPixels]

        # Set them to torch variables for back-propagation
        labelVar = Variable(labelFlat, requires_grad=True)
        # backgroundVar = Variable(imageFlat, requires_grad=False) # not used

        # Get indices of label pixels as a tuple of d tensors where d is the number of dimensions -> ([x1, x2, x3, ..., xn], [y1, y2, y3, ..., yn], ...)
        labelIndices = torch.nonzero(maskFlat, as_tuple=True) # Here, d = 3 and n = 3*numLabelPixels

        # # --------------Try blurring the background image -- Gaussian blur --------------------------------------------------------------------
        if args.blur:
            backgroundImgAsTensor = scipy.ndimage.gaussian_filter(backgroundImgAsTensor, sigma=(0, 0, sigma, sigma), radius=None)
            backgroundImgAsTensor = torch.from_numpy(backgroundImgAsTensor)

            imgReshaped = imageFlat.view(1,3,height,width)
            imgReshaped = scipy.ndimage.gaussian_filter(imgReshaped, sigma=(0, 0, sigma, sigma), radius=None)
            imgReshaped = torch.from_numpy(imgReshaped)
            imageBlurred = imgReshaped.view(1,3,-1)
        
        # # --------------Set optimizer and start iterating---------------------------------------------------------------------------------------
        optimizer = torch.optim.Adam([labelVar,], lr=args.lr, betas=(0.9, 0.999))

        distanceThreshold = 0.23
        MAX_ITER = args.itr
        costList = []

        for iter in range(MAX_ITER): 
            # initialize to the origianl full image (does not matter what the pixels in label region are bc they will be overwritten later)
            if args.blur:
                full_img = imageBlurred  # torch.Size([1, 3, 524288])
            else: 
                full_img = imageFlat # background not blurred

            # Overwrite the label pixels using the updated results
            full_img.index_put_(labelIndices, labelVar.reshape(labelVar.size()[1]))  # labelVar size: torch.Size([1, numLabelPixels]) -> reshape it to [numLabelPixels] to fit in index_put_()
            full_img_flat = full_img # still flat: [1, 3, numPixels]
            full_img = full_img.view(1,3,height,width) # restore its shape to match the original image's shape : [1, 3, width, height]
            full_img.data = torch.clamp(full_img.data, -1, 1)

            if args.metric == 'lpips': 
                # LPIPS loss: LPIPS distance between the current backgroundAndLabelImage and the backgroundImage
                LPIPSLoss = loss_fn.forward(full_img.cuda(), backgroundImgAsTensor.cuda())
                # Negate the loss to make the image more and more different from the original one
                neg_loss = - LPIPSLoss
                # if iter%100 == 0:
                #     print("LPIPS loss:" , neg_loss.item())
            elif args.metric == 'ssim':
                # SSIM loss
                ssim_loss = ssim(full_img.cuda(), backgroundImgAsTensor.cuda(), data_range=2.0, exp=(alpha, beta, gamma))
                neg_loss = ssim_loss  # use 1 - ssim for decreasing the distance (denoising), so just ssim for our purposes
            elif args.metric == 'mssim':
                # MSSIM loss
                mssim_loss = mssim(full_img.cuda(), backgroundImgAsTensor.cuda())
                neg_loss = mssim_loss  # use 1 - ssim for decreasing the distance (denoising), so just ssim for our purposes
            elif args.metric == 'psnr':
                # PSNR loss
                psnr_loss = psnr(full_img.cuda(), backgroundImgAsTensor.cuda())
                neg_loss = psnr_loss  # want lower signal-noise ratio -- lower quality
            
            weight = 10
            if args.deltaE:
                # add delta-E to the loss term
                labelFlat2 = full_img_flat[:, :, maskFlat[0][0]] #[1,3,numLabelPixelsPerChannel]
                labelVar2 = Variable(labelFlat2, requires_grad=True)
                delta_e = deltaE_loss(labelVar) # want to minimize this average delta_e within the label region
                # delta_e.requires_grad = True
                # delta_e_tensor = torch.tensor(delta_e, dtype=torch.float, requires_grad=True)
                neg_loss += delta_e*weight # this *10 here is to give more weight to the delta e loss, but this can change
                # neg_loss = delta_e
                # if iter%100 == 0:
                    # print('delta e loss:', delta_e.item())
                    # print('total loss:' , neg_loss.item())
                    # print('\n')
        

            # Clear the gradient for a new calculation
            optimizer.zero_grad()

            # Do backpropagation based on the LPIPS loss above
            neg_loss.backward(retain_graph=True)

            # append the current loss term to costList to plot them later
            # costList.append(LPIPSLoss[0][0][0][0].item())

            optimizer.step() # based on backpropagation implemented in lpips_loss.py

            # Print out losses/distances
            if iter % 100 == 0:
                if args.metric == 'lpips':
                    loss = LPIPSLoss.view(-1).data.cpu().numpy()[0]
                elif args.metric == 'ssim':
                    loss = ssim_loss.item()
                elif args.metric == 'mssim':
                    loss = mssim_loss.item()
                elif args.metric == 'psnr':
                    loss = psnr_loss.item()
                print('iter %d, dist %.3g' % (iter, loss))
                if args.deltaE:
                     print('deltaE:' , delta_e.item())
                     print('total loss:' , neg_loss.item())
                     log_file.write(f'iter {iter}, dist {loss: .3g}, deltaE {delta_e.item()}\n')
                else:
                    log_file.write(f'iter {iter}, dist {loss: .3g}\n')       

            # Save the output image
            if (iter == MAX_ITER - 1): 
                if args.metric == 'lpips':
                    loss = LPIPSLoss.view(-1).data.cpu().numpy()[0]
                elif args.metric == 'ssim':
                    loss = ssim_loss.item()
                elif args.metric == 'mssim':
                    loss = mssim_loss.item()
                elif args.metric == 'psnr':
                    loss = psnr_loss.item()
                if args.deltaE:
                    print('Final iteration: iter %d, dist %.3g, deltaE %.4g' % (iter, loss, delta_e.item()))
                    log_file.write(f'iter {iter}, dist {loss: .3g}, deltaE {delta_e.item()}\n')
                else:
                    print('Final iteration: iter %d, dist %.3g' % (iter, loss))
                    log_file.write(f'iter {iter}, dist {loss: .3g}\n')
                # print('Final iteration: iter %d, dist %.3g' % (iter, LPIPSLoss.view(-1).data.cpu().numpy()[0]))
                # Get the unblurred background + overlay with label
                full_img = imageFlat # a tensor
                full_img.index_put_(labelIndices, labelVar.reshape(labelVar.size()[1]))  # labelVar size: torch.Size([1, numLabelPixels]) -> reshape it to [numLabelPixels] to fit in index_put_()
                full_img = full_img.view(1,3,height,width) # restore its shape to match the original image's shape -- this is unblurred label with unblurred background
                full_img.data = torch.clamp(full_img.data, -1, 1)

                # Save the final result
                pred_img = lpips.tensor2im(full_img.data)
                image_name = os.path.splitext(os.path.basename(image_path))[0]  # Extract the base name without the extension
                if args.blur:
                    output_path = f"./testResults_20231121/{image_name}_weight-{weight}_{args.metric}_blurredBG_sigma{args.sigma}_itr{args.itr}_lr{args.lr}_deltaE-{args.deltaE}.jpg"
                else:
                    if args.metric == 'ssim':
                        output_path = f"./testResults_20231121/{image_name}_weight-{weight}_{args.metric}_a-{alpha}b-{beta}c-{gamma}_unblurredBG_itr{args.itr}_lr{args.lr}_deltaE-{args.deltaE}.jpg"
                        # output_path = f"./testResults_20231121/{image_name}_weight-{weight}_{args.metric}_s-{ssim_sigma}k1-{k1}k2-{k2}_unblurredBG_itr{args.itr}_lr{args.lr}_deltaE-{args.deltaE}.jpg"
                    else:
                        output_path = f"./testResults_20231121/{image_name}_weight-{weight}_{args.metric}_unblurredBG_itr{args.itr}_lr{args.lr}_deltaE-{args.deltaE}.jpg"
                Image.fromarray(pred_img).save(output_path)
                break

    
    log_file.close()                        











"""
######-------------------------Original code------------------------------------------------
loss_fn = lpips.LPIPS(net='vgg',version=0.1) #changed from alex to vgg based on this documentation: https://pypi.org/project/lpips/#b-backpropping-through-the-metric
loss_fn.cuda()

# Test images downloaded from online sources
# b_path = "./testImg2/test2.jpg"
# b_w_l_path = "./testImg2/test2AndLabel.jpg"
# labelMask_path = "./testImg2/test2AndLabel_mask.jpg"

b_path = "./testCurry/curry.jpg"
b_w_l_path = "./testCurry/curryAndLabel_white.jpg"
labelMask_path = "./testCurry/curryMask.jpg"
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
# backgroundFlat = imageFlat[ :, ~maskFlat[0]] # not used

# Set them to torch variables for back-propagation
labelVar = Variable(labelFlat, requires_grad=True)
# backgroundVar = Variable(imageFlat, requires_grad=False) # not used

# Get indices of label pixels as a tuple of d tensors where d is the number of dimensions -> ([x1, x2, x3, ..., xn], [y1, y2, y3, ..., yn], ...)
labelIndices = torch.nonzero(maskFlat, as_tuple=True) # Here, d = 3 and n = 3*numLabelPixels


# # --------------Try blurring the background image -- Gaussian blur ---------------------------------------------------------
sigma = 15
# r = 60
backgroundImgAsTensor = scipy.ndimage.gaussian_filter(backgroundImgAsTensor, sigma=(0, 0, sigma, sigma), radius=None)
backgroundImgAsTensor = torch.from_numpy(backgroundImgAsTensor)

imgReshaped = imageFlat.view(1,3,height,width)
imgReshaped = scipy.ndimage.gaussian_filter(imgReshaped, sigma=(0, 0, sigma, sigma), radius=None)
# print(type(imgReshaped))
imgReshaped = torch.from_numpy(imgReshaped)
imageBlurred = imgReshaped.view(1,3,-1)


# # --------------Set optimizer and start iterating--------------------------------------------------------------------------
# stochastic gradient descent-based optimizer
# optimizer = torch.optim.SGD([torch.from_numpy(backgroundAndLabelImg)], lr=0.01, momentum=0.9)
# optimizer = torch.optim.SGD([pred], lr=0.05, momentum=0.9)

# Adam optimizer, original lr=1e-4
optimizer = torch.optim.Adam([labelVar,], lr=0.08, betas=(0.9, 0.999))

distanceThreshold = 0.23

MAX_ITER = 1000
costList = []

# Optimize
for iter in range(MAX_ITER): 
    # if type(backgroundAndLabelImg) == torch.Tensor:
    #     backgroundAndLabelImg = backgroundAndLabelImg.numpy()

    # Apply the boolean mask for label pixels
    # maskedLabelTensor = torch.mul(pred, labelMaskAsTensor)
    # maskedLabelTensor = pred.where(torch.logical_not(labelMaskAsTensor), -1) # don't use this # sets all non-label pixels to -1

    # initialize to the origianl full image (does not matter what the pixels in label region are bc they will be overwritten later)
    full_img = imageBlurred  # torch.Size([1, 3, 524288])
    # full_img = imageFlat # background not blurred

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
        # Get the unblurred background + overlay with label
        full_img = imageFlat # a tensor
        full_img.index_put_(labelIndices, labelVar.reshape(labelVar.size()[1]))  # labelVar size: torch.Size([1, numLabelPixels]) -> reshape it to [numLabelPixels] to fit in index_put_()
        full_img = full_img.view(1,3,height,width) # restore its shape to match the original image's shape -- this is unblurred label with unblurred background
        full_img.data = torch.clamp(full_img.data, -1, 1)

        # # ---------------------Try blurring the label-------------------------------------------
        # full_img = cv2.imread(b_w_l_path)
        # mask = (1/255)*cv2.imread(labelMask_path) 

        # blurred_label = full_img
        # blurred_label[mask > 0.5] = scipy.ndimage.gaussian_filter(full_img, sigma=(3, 3, 0))[mask > 0.5]
        # combined = full_img
        # combined[mask > 0.5] = blurred_label[mask > 0.5]
        # # output_img =  lpips.tensor2im(combined.data)

        # output_path = "./tests/test003.jpg"
        # cv2.imwrite(output_path, combined)

        
        
        pred_img = lpips.tensor2im(full_img.data)
        print(type(pred_img))
        output_path = "./testCurry/blurredBG_lr0.08_sigma15_itr1000_white.jpg"
        # output_path = "./final_result_adam_lr0.08.jpg"
        Image.fromarray(pred_img).save(output_path)
        break

        #  # Check the size of the image after backpropping
        #  width, height = Image.open(output_path).size
        #  print(f"Image width: {width}px, Image height: {height}px")


"""