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
import time
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
from kornia.color import rgb_to_lab

RGB_TO_XYZ = torch.tensor([
				#    X        Y          Z
				[0.412453, 0.212671, 0.019334], # R
				[0.357580, 0.715160, 0.119193], # G
				[0.180423, 0.072169, 0.950227], # B
			]).type(torch.FloatTensor).cuda()

FXFYFZ_TO_LAB =  torch.tensor([
		#  l       a       b
		[  0.0,  500.0,    0.0], # fx
		[116.0, -500.0,  200.0], # fy
		[  0.0,    0.0, -200.0], # fz
	]).type(torch.FloatTensor).cuda()

C1 = torch.tensor([1/0.950456, 1.0, 1/1.088754]).type(torch.FloatTensor).cuda()
C2 = torch.tensor([-16.0, 0.0, 0.0]).type(torch.FloatTensor).cuda()

def rgb_to_lab_own(srgb): # srgb: an image of size [..., 3]
	srgb_pixels = torch.reshape(srgb, [-1, 3]).cuda()

	linear_mask = (srgb_pixels <= 0.04045).type(torch.FloatTensor).cuda()

	exponential_mask = (srgb_pixels > 0.04045).type(torch.FloatTensor).cuda()
	rgb_pixels = (srgb_pixels / 12.92 * linear_mask) + (((srgb_pixels + 0.055) / 1.055) ** 2.4) * exponential_mask

	# rgb_to_xyz = torch.tensor([
	# 			#    X        Y          Z
	# 			[0.412453, 0.212671, 0.019334], # R
	# 			[0.357580, 0.715160, 0.119193], # G
	# 			[0.180423, 0.072169, 0.950227], # B
	# 		]).type(torch.FloatTensor).cuda()

	rgb_to_xyz = RGB_TO_XYZ

	xyz_pixels = torch.mm(rgb_pixels, rgb_to_xyz)
	
	# XYZ to Lab
	# xyz_normalized_pixels = torch.mul(xyz_pixels, torch.tensor([1/0.950456, 1.0, 1/1.088754]).type(torch.FloatTensor).cuda())
	xyz_normalized_pixels = torch.mul(xyz_pixels, C1)
	epsilon = 6.0/29.0

	linear_mask = (xyz_normalized_pixels <= (epsilon**3)).type(torch.FloatTensor).cuda()

	exponential_mask = (xyz_normalized_pixels > (epsilon**3)).type(torch.FloatTensor).cuda()

	fxfyfz_pixels = (xyz_normalized_pixels / (3 * epsilon**2) + 4.0/29.0) * linear_mask + ((xyz_normalized_pixels+0.000001) ** (1.0/3.0)) * exponential_mask
    # convert to lab
	# fxfyfz_to_lab = torch.tensor([
	# 	#  l       a       b
	# 	[  0.0,  500.0,    0.0], # fx
	# 	[116.0, -500.0,  200.0], # fy
	# 	[  0.0,    0.0, -200.0], # fz
	# ]).type(torch.FloatTensor).cuda()

	fxfyfz_to_lab = FXFYFZ_TO_LAB
	# lab_pixels = torch.mm(fxfyfz_pixels, fxfyfz_to_lab) + torch.tensor([-16.0, 0.0, 0.0]).type(torch.FloatTensor).cuda()
	lab_pixels = torch.mm(fxfyfz_pixels, fxfyfz_to_lab) + C2
	#return tf.reshape(lab_pixels, tf.shape(srgb))
	return torch.reshape(lab_pixels, srgb.shape)

def delta_e_cie76_orig(lab1, lab2):
    # print(lab1.shape, lab2.shape)
    l1, a1, b1 = lab1
    l2, a2, b2 = lab2
    deltaL = l2-l1
    deltaB = b2-b1
    deltaA = a2-a1
    
    return (deltaA**2 + deltaB**2 + deltaL**2)

def delta_e_cie76(lab1, lab2):
    # print(lab1.shape, lab2.shape)
    # print(torch.isnan(lab1).any(), torch.isnan(lab2).any())
    sq = torch.square(torch.sub(lab1, lab2))
    delta_e_matrix = torch.sum(sq, dim=0)
    # print("delta_e_matrix nan: ", torch.isnan(delta_e_matrix).any())
    
    return delta_e_matrix

class DeltaELoss(torch.nn.Module):
    def __init__(self):
        super(DeltaELoss, self).__init__()
    
    def forward(self, image):
        # # # # --------LAB version of delta E----------------
        # image = 0.5*torch.add(image, torch.ones(image.shape)) # adjust the image to range [0,1]
        # image = image.unsqueeze(-1) # add one dimension to make the tensor [1,3,numLabelPixels,1]
        # # input image size : [1,3,H,W] in range of [0,1] --> now the input labelVar is of size [1,3,numLabelPixels]
        # image_lab = rgb_to_lab(image) # The L channel values are in the range 0..100. a and b are in the range -128..127
        # # print(torch.isnan(image_lab).any(), torch.isnan(image).any())
        # mean_lab = torch.mean(image_lab, dim = 2).squeeze()
        # image_lab = image_lab.squeeze(-1) # now image_lab has size [1,3,numLabelPixels]

        # # Reshape mean_lab to have the same shape as image_lab for broadcasting
        # image_lab = image_lab.squeeze(0) # [3, numLabelPixels]
        # mean_lab = mean_lab.unsqueeze(-1).expand_as(image_lab)   # [3, numLabelPixels]
        # # Calculate Delta E using matrix operations
        # delta_e_matrix = delta_e_cie76(mean_lab, image_lab)
        # # Calculate the mean of the individual results
        # mean_result = torch.mean(delta_e_matrix)
        # # print("deltaE: " , mean_result)

        # return mean_result

        # # # -----------rgb version of deltaE--------------
        image = 0.5*torch.add(image, torch.ones(image.shape)) # adjust the image to range [0,1]
        distanceAsTensor = torch.square(torch.sub(image, torch.ones(image.shape)*torch.mean(image)))
        # print("result ", torch.mean(distanceAsTensor))
        return torch.mean(distanceAsTensor)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description='Label color generation')
    parser.add_argument('--lr', type=float, default=0.08, help='Learning rate')
    parser.add_argument('--sigma', type=float, default=10, help='Gaussian Blur sigma')
    parser.add_argument('--itr', type=int, default=200, help='Number of iterations')
    parser.add_argument('--image_paths', nargs='+',         default=[
                                                                     './testImages/testSingleColor/blue.jpg'
                                                                     ,'./testImages/testSingleColor/red.jpg'
                                                                     ,'./testImages/testRainbow/rainbow.jpg' 
                                                                     ,'./testImages/testCurry/curry.jpg'          
                                                                     ,'./testImages/testRiver/river.jpg'
                                                                     #  ,'./testImages/testRiver/river_white.jpg'
                                                                     ,'./testImages/testBeach/beach.jpg'
                                                                    #  ,'./testImages/testSingleColor/blueRG.jpg'
                                                                     ,
                                                                     './testImages/testCluttered/city_black.jpg'
                                                                     ,'./testImages/testCluttered/city_white.jpg'
                                                                    #  ,'./testImages/testCluttered/city_rgb.jpg'
                                                                     ,'./testImages/testCluttered/classroom_white.jpg'
                                                                    #  ,'./testImages/testCluttered/classroom_bw.jpg'
                                                                    ], 
                                                                     help='Paths to input images')
    parser.add_argument('--imageAndLabel_paths', nargs='+', default=[
                                                                    './testImages/testSingleColor/blueAndLabel.jpg'
                                                                     ,'./testImages/testSingleColor/redAndLabel.jpg'
                                                                     ,'./testImages/testRainbow/rainbowAndLabel.jpg'
                                                                     ,'./testImages/testCurry/curryAndLabel_white.jpg'
                                                                     ,'./testImages/testRiver/riverAndLabel.jpg'
                                                                    #  ,'./testImages/testRiver/riverAndLabel_white.jpg'
                                                                     ,'./testImages/testBeach/beachAndLabel_purple.jpg'
                                                                    #  ,'./testImages/testSingleColor/blueAndRGLabel.jpg'
                                                                    
                                                                     ,
                                                                     './testImages/testCluttered/cityAndLabel_black.jpg'
                                                                     ,'./testImages/testCluttered/cityAndLabel_white.jpg'
                                                                    #  ,'./testImages/testCluttered/cityAndLabel_rgb.jpg'
                                                                     ,'./testImages/testCluttered/classroomAndLabel_white.jpg'
                                                                    #  ,'./testImages/testCluttered/classroomAndLabel_bw.jpg'
                                                                    ], 
                                                                     help='Paths to input images with labels')
    parser.add_argument('--mask_paths', nargs='+',          default=[
                                                                    './testImages/testSingleColor/mask.jpg'
                                                                     ,'./testImages/testSingleColor/mask.jpg'
                                                                     ,'./testImages/testRainbow/rainbowMask.jpg'
                                                                     ,'./testImages/testCurry/curryMask.jpg'        
                                                                     ,'./testImages/testRiver/riverMask.jpg'
                                                                    #  ,'./testImages/testRiver/riverMask.jpg'
                                                                      ,'./testImages/testBeach/beachMask.jpg'
                                                                    #  ,'./testImages/testSingleColor/blueAndRGLabelMask.jpg'
                                                                   
                                                                     ,
                                                                     './testImages/testCluttered/cityMask.jpg'
                                                                     ,'./testImages/testCluttered/cityMask.jpg'
                                                                    #  ,'./testImages/testCluttered/cityMask.jpg'
                                                                     ,'./testImages/testCluttered/classroomMask.jpg'
                                                                    #  ,'./testImages/testCluttered/classroomMask.jpg'
                                                                    ], 
                                                                     help='Paths to masks for input images')
    parser.add_argument('--blur',  default=False, help='Apply blur to background')
    parser.add_argument('--deltaE',  default=False, help='Add delta E to loss')

    parser.add_argument('--metric', choices=['lpips', 'ssim', 'mssim', 'psnr'], default='lpips', help='Distance calculation method')
    parser.add_argument('--metrics', nargs='+', choices=['lpips', 'ssim', 'mssim', 'psnr'], default=['lpips'], help='Distance calculation method')
    args = parser.parse_args()

    conditions = [
        # # ----LPIPS condition set 1: change #iterations and deltaE------
        # {'metric': 'lpips', 'iterations': 50, 'deltaE': False},
        # {'metric': 'lpips', 'iterations': 200, 'deltaE': False},
        # {'metric': 'lpips', 'iterations': 200, 'deltaE': True, 'weight': 1}, # 'weight' is the weight on deltaE loss
        # {'metric': 'lpips', 'iterations': 200, 'deltaE': True, 'weight': 5},
        # ----LPIPS condition set 2: with and without initial label color----
        {'metric': 'lpips', 'iterations': 200, 'deltaE': False, 'init_color': True}, # 'ini_color'=True -> has initial label color
        {'metric': 'lpips', 'iterations': 200, 'deltaE': False, 'init_color': False},
        {'metric': 'lpips', 'iterations': 1000, 'deltaE': False, 'init_color': False},
        # # ----SSIM condition set 1: change k1, k2----
        # {'metric': 'ssim', 'iterations': 100, 'k1': 0.01, 'k2': 0.03, 'deltaE': False}, # default 0.01, 0.03, default sigma = 1.5
        # {'metric': 'ssim', 'iterations': 100, 'k1': 0.3, 'k2': 0.03, 'deltaE': False},
        # {'metric': 'ssim', 'iterations': 100, 'k1': 0.01, 'k2': 0.3, 'deltaE': False}, # default 0.01, 0.03, default sigma = 1.5
        # {'metric': 'ssim', 'iterations': 100, 'k1': 0.3, 'k2': 0.3, 'deltaE': False},
        # # ----SSIM condition set 2: change alpha, beta, gamma----
        # {'metric': 'ssim', 'iterations': 100, 'alpha': 1, 'beta': 1, 'gamma': 1, 'deltaE': False}, # default 1, 1, 1
        # {'metric': 'ssim', 'iterations': 100, 'alpha': 1, 'beta': 10, 'gamma': 1, 'deltaE': False},
        # {'metric': 'ssim', 'iterations': 100, 'alpha': 1, 'beta': 1, 'gamma': 10, 'deltaE': False},
        # {'metric': 'ssim', 'iterations': 100, 'alpha': 10, 'beta': 1, 'gamma': 10, 'deltaE': False},
        # # #----SSIM condition set 3: with or without deltaE, and with mssim----
        # {'metric': 'ssim', 'iterations': 100, 'deltaE': False},
        # {'metric': 'ssim', 'iterations': 100, 'deltaE': True, 'weight': 0.1},
        # {'metric': 'ssim', 'iterations': 100, 'deltaE': True, 'weight': 1},
        # {'metric': 'ssim', 'iterations': 100, 'deltaE': True, 'weight': 5},
        # {'metric': 'mssim', 'iterations': 100, 'deltaE': False}, 
        # {'metric': 'mssim', 'iterations': 100, 'deltaE': True, 'weight': 1},
        # Add more conditions as needed
    ]
    k1 = 0.01; k2 = 0.03 # default values
    alpha = 1; beta = 1; gamma = 1 
    init_color = True # default is to use the color designated by the backgroundAndLabel image
    useDeltaE = False # default to not use DeltaE
    results_map = {}
    for condition in conditions:
        metric = condition['metric']
        iterations = condition['iterations']
        useDeltaE = condition['deltaE']
        condition_key = f"{metric}_itr_{iterations}_dE_{useDeltaE}"
        if 'weight' in condition:
            weight = condition['weight']
            condition_key += f"_w_{weight}"
        if 'k1' in condition and 'k2' in condition:
            k1 = condition['k1']
            k2 = condition['k2']
            condition_key += f"_k1_{k1}_k2_{k2}"
        if 'alpha' in condition:
            alpha = condition['alpha']
            beta = condition['beta']
            gamma = condition['gamma']
            condition_key += f"_a_{alpha}_b_{beta}_c_{gamma}"
        if 'init_color' in condition:
            init_color = condition['init_color'] # Set to True or False
            condition_key += f"_initLabelColor_{init_color}"
        
        results_map['Initial Images'] =  {'losses': [], 'images': []}
        for imageAndLabel_path in args.imageAndLabel_paths:
            results_map['Initial Images']['images'].append(np.array(Image.open(imageAndLabel_path)))

        results_map[condition_key] = {'losses': [], 'images': []}

        device = 'cuda' if torch.cuda.is_available() else 'cpu'
        if metric == 'lpips':
            loss_fn = lpips.LPIPS(net='vgg', version=0.1) #changed from alex to vgg based on this documentation: https://pypi.org/project/lpips/#b-backpropping-through-the-metric
            loss_fn.cuda()
        elif metric == 'ssim':
            pass
            # ssim = StructuralSimilarityIndexMeasure(sigma=ssim_sigma, k1=k1, k2=k2).to(device)
        elif metric == 'mssim':
            mssim = MultiScaleStructuralSimilarityIndexMeasure().to(device)
        elif metric == 'psnr':
            psnr = PeakSignalNoiseRatio().to(device)

        if useDeltaE:
            deltaE_loss = DeltaELoss()

        for image_path, imageAndLabel_path, mask_path in zip(args.image_paths, args.imageAndLabel_paths, args.mask_paths):
            b_path = image_path
            b_w_l_path = imageAndLabel_path  # Adjust this as needed
            labelMask_path = mask_path
            labelMaskImg = (1/255) * np.asarray(Image.open(labelMask_path))
            sigma = args.sigma

            # Header for log file
            image_name = os.path.splitext(os.path.basename(image_path))[0]  # Extract the base name without the extension

            # # --------------Load images and convert them to tensors---------------------------------------------------------------------------------
            # Convert the images to tensors
            backgroundImgAsTensor = lpips.im2tensor(lpips.load_image(b_path))
            backgroundAndLabelImgAsTensor = lpips.im2tensor(lpips.load_image(b_w_l_path))
            height = backgroundImgAsTensor.size(2)
            width = backgroundImgAsTensor.size(3)
            numPixels = height * width  # 512*1024 for AR screenshots

            # # --------------Separate label and background and set different requires_grad values for them -> only gradient descent on label pixels-------------------------
            # create label mask (boolean mask with True where label pixels are)
            labelMaskAsTensor = lpips.im2tensor(lpips.load_image(labelMask_path)) > 0
            numLabelPixels = labelMaskAsTensor[0,0].sum().item() # number of label pixels

            # Flatten image and mask to apply the mask
            if init_color: # if use the initial color designated by backgroundAndLabelImg
                imageFlat = backgroundAndLabelImgAsTensor.view(1,3,-1) #[1,3,numPixels]
            else: # if use the background pixels as original label color
                imageFlat = backgroundImgAsTensor.view(1,3,-1) #[1,3,numPixels]
            # print("shape of imageFlat:", imageFlat.size())
            maskFlat = labelMaskAsTensor.view(1,3,-1) #[1,3,numPixels]

            # Separate background and label pixels
            labelFlat = imageFlat[:, maskFlat[0]] #[1,3*numLabelPixels] : collapsed 3 color channels
            labelFlat2 = imageFlat[:, :, maskFlat[0][0]] #[1,3,numLabelPixels]
            # labelFlat2.fill_(1.0) # fill with all white values

            # Set them to torch variables for back-propagation
            labelVar = Variable(labelFlat2, requires_grad=True) # now labelVar size is [1,3,numLabelPixels]
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
            MAX_ITER = iterations
            costList = []

            for iter in range(MAX_ITER): 
                # initialize to the origianl full image (does not matter what the pixels in label region are bc they will be overwritten later)
                if args.blur:
                    full_img = imageBlurred  # torch.Size([1, 3, 524288])
                else: 
                    full_img = imageFlat # background not blurred

                # Overwrite the label pixels using the updated results
                labelVarFlat = labelVar.view(1,-1) # [1,3,numPixels] -> [1, numLabelPixels]
                full_img.index_put_(labelIndices, labelVar.view(1,-1).reshape(labelVarFlat.size()[1]))  # labelVar size: torch.Size([1, numLabelPixels]) -> reshape it to [numLabelPixels] to fit in index_put_()
                full_img_flat = full_img # still flat: [1, 3, numPixels]
                full_img = full_img.view(1,3,height,width) # restore its shape to match the original image's shape : [1, 3, width, height]
                full_img.data = torch.clamp(full_img.data, -1, 1)

                if metric == 'lpips': 
                    # LPIPS loss: LPIPS distance between the current backgroundAndLabelImage and the backgroundImage
                    LPIPSLoss = loss_fn.forward(full_img.cuda(), backgroundImgAsTensor.cuda())
                    # Negate the loss to make the image more and more different from the original one
                    neg_loss = - LPIPSLoss

                elif metric == 'ssim':
                    # SSIM loss
                    ssim_loss = ssim(full_img.cuda(), backgroundImgAsTensor.cuda(), data_range=2.0, K=(k1, k2), exp=(alpha, beta, gamma))
                    neg_loss = ssim_loss  # use 1 - ssim for decreasing the distance (denoising), so just ssim for our purposes
                    
                elif metric == 'mssim':
                    # MSSIM loss
                    mssim_loss = mssim(full_img.cuda(), backgroundImgAsTensor.cuda())
                    neg_loss = mssim_loss  # use 1 - ssim for decreasing the distance (denoising), so just ssim for our purposes
                elif metric == 'psnr':
                    # PSNR loss
                    psnr_loss = psnr(full_img.cuda(), backgroundImgAsTensor.cuda())
                    neg_loss = psnr_loss  # want lower signal-noise ratio -- lower quality
                
                # weight = 1
                if useDeltaE:
                    # add delta-E to the loss term
                    delta_e = deltaE_loss(labelVar) # want to minimize this average delta_e within the label region
                    neg_loss += delta_e*weight # this *10 here is to give more weight to the delta e loss, but this can change
                    # neg_loss = delta_e
            

                # Clear the gradient for a new calculation
                optimizer.zero_grad()

                # Do backpropagation based on the LPIPS loss above
                neg_loss.backward(retain_graph=True)

                # append the current loss term to costList to plot them later
                # costList.append(LPIPSLoss[0][0][0][0].item())

                optimizer.step() # based on backpropagation implemented in lpips_loss.py
                
                # # Print out losses/distances
                # if iter % 100 == 0:
                #     if args.metric == 'lpips':
                #         loss = LPIPSLoss.view(-1).data.cpu().numpy()[0]
                #     elif args.metric == 'ssim':
                #         loss = ssim_loss.item()
                #     elif args.metric == 'mssim':
                #         loss = mssim_loss.item()
                #     elif args.metric == 'psnr':
                #         loss = psnr_loss.item()
                #     print('iter %d, dist %.3g' % (iter, loss))

                #     if args.deltaE:
                #         print('deltaE:' , delta_e.item())
                #         print('total loss:' , neg_loss.item())
                            
                # Last iteration
                # Save the output image
                if (iter == MAX_ITER - 1): 
                    if metric == 'lpips':
                        loss = LPIPSLoss.view(-1).data.cpu().numpy()[0]
                    elif metric == 'ssim':
                        loss = ssim_loss.item()
                    elif metric == 'mssim':
                        loss = mssim_loss.item()
                    elif metric == 'psnr':
                        loss = psnr_loss.item()
                    if useDeltaE:
                        print('Final iteration: iter %d, dist %.3g, deltaE %.4g' % (iter, loss, delta_e.item()))
                    else:
                        print('Final iteration: iter %d, dist %.3g' % (iter, loss))
                    results_map[condition_key]['losses'].append(loss)

                    # print('Final iteration: iter %d, dist %.3g' % (iter, LPIPSLoss.view(-1).data.cpu().numpy()[0]))
                    # Get the unblurred background + overlay with label
                    full_img = imageFlat # a tensor
                    labelVarFlat = labelVar.view(1,-1) # [1,3,numPixels] -> [1, numLabelPixels]
                    full_img.index_put_(labelIndices, labelVar.view(1,-1).reshape(labelVarFlat.size()[1]))  # labelVar size: torch.Size([1, numLabelPixels]) -> reshape it to [numLabelPixels] to fit in index_put_()                full_img = full_img.view(1,3,height,width) # restore its shape to match the original image's shape -- this is unblurred label with unblurred background
                    full_img = full_img.view(1,3,height,width) # restore its shape to match the original image's shape
                    full_img.data = torch.clamp(full_img.data, -1, 1)

                    pred_img = lpips.tensor2im(full_img.data)
                    results_map[condition_key]['images'].append(pred_img)

                    # # Save the final result
                    # pred_img = lpips.tensor2im(full_img.data)
                    # image_name = os.path.splitext(os.path.basename(image_path))[0]  # Extract the base name without the extension
                    # if args.blur:
                    #     output_path = f"./test20231205_ssim_exponents/{image_name}_weight-{weight}_{args.metric}_blurredBG_sigma{args.sigma}_itr{args.itr}_lr{args.lr}_deltaE-{args.deltaE}.jpg"
                    # else:
                    #     if args.metric == 'ssim':
                    #         output_path = f"./test20231205_ssim_exponents/{image_name}_weight-{weight}_{args.metric}_a-{alpha}b-{beta}c-{gamma}_unblurredBG_itr{args.itr}_lr{args.lr}_deltaE-{args.deltaE}.jpg"
                    #         # output_path = f"./test20231205_ssim_exponents/{image_name}_weight-{weight}_{args.metric}_s-{ssim_sigma}k1-{k1}k2-{k2}_unblurredBG_itr{args.itr}_lr{args.lr}_deltaE-{args.deltaE}.jpg"
                    #     else:
                    #         output_path = f"./test20231205_ssim_exponents/{image_name}_weight-{weight}_{args.metric}_unblurredBG_itr{args.itr}_lr{args.lr}_deltaE-{args.deltaE}.jpg"
                    # Image.fromarray(pred_img).save(output_path)
                    break
        
    # Plotting results for different conditions and input images
    num_images = len(args.image_paths) 
    num_conditions = len(conditions) + 1

    fig, axes = plt.subplots(num_conditions, num_images, figsize=(10, 6))

    for idx, (condition_key, results) in enumerate(results_map.items()):
        row = idx
        col_start = 0

        losses = results['losses']
        images = results['images']

        if idx == 0:
            for i, image in enumerate(images):
                col = col_start + i 
                axes[row, col].imshow(image)  # Assuming image is a valid array/image
                axes[row, col].axis('off')
                axes[row, col].set_title(f'Initial Image', fontsize=4.0)
        else:
            for i, (loss, image) in enumerate(zip(losses, images)):
                col = col_start + i 

                axes[row, col].imshow(image)  # Assuming image is a valid array/image
                axes[row, col].axis('off')
                axes[row, col].set_title(f'{condition_key} \nLoss: {loss:.2f}', fontsize=3.0)

    plt.tight_layout()
    plt.savefig('./testResults/test20231210/plot_LPIPS_2', dpi=2000)
    plt.show()

    











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