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

# 2. Initiate server
app = FastAPI()
# print(torch.cuda.is_available()) # uncomment this line to check if cuda is available

# 3. Initiate lpips
loss_fn = lpips.LPIPS(net='alex',version=0.1)
# loss_fn.cuda() #comment out if not using GPU

class Input(BaseModel): # should be in the same format as the object initiated in Unity
    background_and_label_rgb_base64: str
    background_rgb_base64: str

# A wrapper func for lpips that takes in one numpy array (background concatenated with background_label and outputs a float representing the distance
# To fit the parameter of scipy.optimize
def lpips_helper(backgroundAndLabelImg, backgroundImg, originalShape):
    background = lpips.im2tensor(backgroundImg)
    backgroundAndLabel = lpips.im2tensor(backgroundAndLabelImg.reshape(originalShape))
    return - loss_fn.forward(backgroundAndLabel, background)[0][0][0][0].item()  # reverse the sign here so that it's actually maximizing the distance (vs. reciprocal/log??)


@app.put("/predict")
def predict(input:Input):
    # load the input image with background and label
    backgroundAndLabelString = input.background_and_label_rgb_base64
    if backgroundAndLabelString != "":
        backgroundAndLabelImg = np.asarray(Image.open(io.BytesIO(base64.b64decode(backgroundAndLabelString))))
        backgroundAndLabel = lpips.im2tensor(backgroundAndLabelImg)
        # bgAndlb = Image.fromarray(backgroundAndLabelImg) #for debugging purposes only
        # bgAndlb.save("backgroundAndLabel.jpg") #for debugging purposes only

        # load the input image with background only
        backgroundString = input.background_rgb_base64
        backgroundImg = np.asarray(Image.open(io.BytesIO(base64.b64decode(backgroundString))))
        background = lpips.im2tensor(backgroundImg)
        # bg = Image.fromarray(backgroundImg) #for debugging purposes only
        # bg.save("background.jpg") #for debugging purposes only

        # run LPIPS to calculate the difference between Background vs. Background+Label
        LPIPSDistance = loss_fn.forward(backgroundAndLabel, background)[0][0][0][0].item() # remove .cuda() if not using GPU

        # # Concatenate the two np arrays to pass them into the optimize function as inputs 
        # combined_images = np.concatenate((backgroundImg, backgroundAndLabelImg), axis=0)

        # # Add the optimizer function here and send the resulting array of pixels back to the headset
        # originalShape = backgroundAndLabelImg.shape
        # backgroundAndLabelImg_flattened =  backgroundAndLabelImg.flatten()
        # minimized = scipy.optimize.minimize(lpips_helper, backgroundAndLabelImg_flattened, args=(backgroundImg,originalShape), bounds=[(0,255)]*len(backgroundAndLabelImg_flattened))

        # # encode and send the result of the minimization (by writing to a json file)
        # encoded_result = base64.b64encode(minimized.x)
        # jsonData = json.dumps(encoded_result)

        # only sends the LPIPS distance
        # for debugging purposes only
        jsonData = json.dumps("LPIPS distance: " + str(LPIPSDistance))
        backgroundAndLabelString = ""


    # torch.cuda.empty_cache()
    return jsonData
    
# Run the API with uvicorn
#    Will run on http://Your IP Address:8000
if __name__ == '__main__':
    uvicorn.run(app, host='127.0.0.1', port=8000)