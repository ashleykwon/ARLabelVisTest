# 1. Library imports
import uvicorn
from fastapi import FastAPI, Request, Response
from pydantic import BaseModel
import torch
import io, json
import base64
import PIL.Image as Image
import numpy as np
import lpips
import scipy
import scipy.optimize


loss_fn = lpips.LPIPS(net='alex',version=0.1)

b_path = "/Users/ARDesigner/Desktop/ARLabelVisTest/Assets/background.jpg"
b_w_l_path = "/Users/ARDesigner/Desktop/ARLabelVisTest/Assets/backgroundAndLabel.jpg"
backgroundImg = np.asarray(Image.open(b_path))
backgroundAndLabelImg = np.asarray(Image.open(b_w_l_path))


# A wrapper func for lpips that takes in one numpy array (background concatenated with background_label and outputs a float representing the distance
# To fit the parameter of scipy.optimize
def lpips_helper(backgroundAndLabelImg, backgroundImg, originalShape):
    background = lpips.im2tensor(backgroundImg)
    backgroundAndLabel = lpips.im2tensor(backgroundAndLabelImg.reshape(originalShape))
    return - loss_fn.forward(backgroundAndLabel, background)[0][0][0][0].item()  # reverse the sign here so that it's actually maximizing the distance (vs. reciprocal/log??)


# Add the optimizer function here and send the resulting array of pixels back to the headset
originalShape = backgroundAndLabelImg.shape
backgroundAndLabelImg_flattened =  backgroundAndLabelImg.flatten()
minimized = scipy.optimize.minimize(lpips_helper, backgroundAndLabelImg_flattened, args=(backgroundImg,originalShape), bounds=[(0,255)]*len(backgroundAndLabelImg_flattened))
minimized_res = minimized.x.reshape(originalShape)

image = Image.fromarray(minimized_res)
output_path = "/Users/ARDesigner/Desktop/ARLabelVisTest/Assets/OptimizerTestOutput/output.jpg"
image.save(output_path)