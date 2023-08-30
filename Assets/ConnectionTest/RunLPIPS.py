# 1. Library imports
import uvicorn
from fastapi import FastAPI, Request, Response
from pydantic import BaseModel
import torch
import io, json
import base64
import PIL.Image as Image
import numpy as np

# 2. Initiate server
app = FastAPI()

class Input(BaseModel): # should be in the same format as the object initiated in Unity
    rgb_base64: str

@app.put("/predict")
def predict(d:Input):
    imgString = d.rgb_base64
    image = Image.open(io.BytesIO(base64.b64decode(imgString)))
    image.save("./screenshot.jpg") # for debugging purposes only
    recommendations = "hiiiii"
    jsonData = json.dumps(recommendations)
    return jsonData
    
# Run the API with uvicorn
#    Will run on http://Your IP Address:8000
if __name__ == '__main__':
    uvicorn.run(app, host='Your IP Address', port=8000)