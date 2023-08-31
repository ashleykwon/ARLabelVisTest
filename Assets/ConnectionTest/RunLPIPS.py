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

# 2. Initiate server
app = FastAPI()
# print(torch.cuda.is_available()) # uncomment this line to check if cuda is available

# 3. Initiate lpips
loss_fn = lpips.LPIPS(net='alex',version=0.1)
loss_fn.cuda() #comment out if not using GPU

class Input(BaseModel): # should be in the same format as the object initiated in Unity
    background_and_label_rgb_base64: str
    background_rgb_base64: str



@app.put("/predict")
def predict(input:Input):
    # load the input image with background and label
    backgroundAndLabelString = input.background_and_label_rgb_base64
    backgroundAndLabelImg = np.asarray(Image.open(io.BytesIO(base64.b64decode(backgroundAndLabelString))))
    backgroundAndLabel = lpips.im2tensor(backgroundAndLabelImg)
    # backgroundAndLabel.cuda() #comment out if not using GPU

    # load the input image with background only
    backgroundString = input.background_rgb_base64
    backgroundImg = np.asarray(Image.open(io.BytesIO(base64.b64decode(backgroundString))))
    background = lpips.im2tensor(backgroundImg)
    # background.cuda() #comment out if not using GPU

    # run LPIPS to calculate the difference between Background vs. Background+Label
    LPIPSDistance = loss_fn.forward(backgroundAndLabel.cuda(), background.cuda())[0][0][0][0].item()

    # for debugging purposes only
    jsonData = json.dumps("LPIPS distance: " + str(LPIPSDistance))
    return jsonData
    
# Run the API with uvicorn
#    Will run on http://Your IP Address:8000
if __name__ == '__main__':
    uvicorn.run(app, host='10.38.23.43', port=8000)