# Experiments with area labels
# This file uses original background color as label color, i.e., do not assign a color to the label from the beginning
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
from kornia.color import rgb_to_lab

background = cv2.imread('./testImages/testFruit/fruit.jpg')
mask = cv2.imread('./testImages/testFruit/areaMask.jpg', cv2.IMREAD_GRAYSCALE)
_, mask = cv2.threshold(mask, 1, 255, cv2.THRESH_BINARY)
# print(np.shape(mask))

overlay_color =  np.array([255,0,0])
overlay = np.zeros_like(background)
# print(np.shape(overlay))
overlay[:, :] = overlay_color # now overlay should be a pure red image with size the same as background
# want overlay to only have values in the masked area
overlay[mask == 0] = 0
background = background.astype(np.float32)
overlay = overlay.astype(np.float32) #/ 255.0
result = cv2.addWeighted(background, 1.0, overlay, 0.5, 0)
# cv2.imshow('Hightlighted', result)
cv2.imwrite('./testResults/test20240201_noInitialColor/fruit_opcityResult.jpg', result)

###------------Another example------------------------------------------------------------
background2 = cv2.imread('./testImages/testCluttered/city_board.jpg')
mask2 = cv2.imread('./testImages/testCluttered/cityAreaMask_board.jpg', cv2.IMREAD_GRAYSCALE)
_, mask2 = cv2.threshold(mask2, 1, 255, cv2.THRESH_BINARY)
mask2[mask2 <= 100] = 0
# print(np.min(mask2))
# print(np.count_nonzero(mask2), 512*825)

overlay_color =  np.array([255,0,0])
overlay2 = np.zeros_like(background2)
# print(np.shape(overlay2))
overlay2[:, :] = overlay_color # now overlay should be a pure red image with size the same as background
# want overlay to only have values in the masked area
overlay2[mask2 == 0] = 0
background2 = background2.astype(np.float32)
overlay2 = overlay2.astype(np.float32) #/ 255.0
result2 = cv2.addWeighted(background2, 1.0, overlay2, 1, 0)
# cv2.imshow('Hightlighted', result)
cv2.imwrite('./testResults/test20240201_noInitialColor/board_opcityResult_1.jpg', result2)