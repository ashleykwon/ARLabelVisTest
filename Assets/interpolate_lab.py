import numpy as np
import matplotlib.pyplot as plt
from mpl_toolkits import mplot3d
from skimage.color import deltaE_cie76, deltaE_ciede94, deltaE_ciede2000, lab2rgb 
from tqdm import tqdm
import os
from multiprocessing import Pool
import time
import scipy

class LABData:
    l: float
    a: float
    b: float

def InitializeLookupTexture(a, b, c):
    lst = [[ [LABData for col in range(a)] for col in range(b)] for row in range(c)]
    return lst

def interpolation2D(lab1, lab2, diff):
    l = lab1.l*(1-diff) + lab2.l*diff
    a = lab1.a*(1-diff) + lab2.a*diff
    b = lab1.b*(1-diff) + lab2.b*diff
    interpolatedLAB = LABData()
    interpolatedLAB.l = l
    interpolatedLAB.a = a
    interpolatedLAB.b = b
    return interpolatedLAB


def trilinearInterpolation(LookupTexture, rgbAsIndices):
    rIdx = rgbAsIndices[0]
    gIdx = rgbAsIndices[1]
    bIdx = rgbAsIndices[2]
    lookupTableStepSize = 4

    rLowerBound = int(rIdx // lookupTableStepSize) * lookupTableStepSize
    rUpperBound = min(rLowerBound + lookupTableStepSize, 252)

    gLowerBound = int(gIdx // lookupTableStepSize) * lookupTableStepSize
    gUpperBound = min(gLowerBound + lookupTableStepSize, 252)

    bLowerBound = int(bIdx // lookupTableStepSize) * lookupTableStepSize
    bUpperBound = min(bLowerBound + lookupTableStepSize, 252)

    rDiff = 0
    gDiff = 0
    bDiff = 0

    if (rUpperBound - rLowerBound > 0):
        rDiff = (rIdx - rLowerBound)/(rUpperBound - rLowerBound)
    if (gUpperBound - gLowerBound > 0):
        gDiff = (gIdx - gLowerBound)/(gUpperBound - gLowerBound)
    if (bUpperBound - bLowerBound > 0):
        bDiff = (bIdx - bLowerBound)/(bUpperBound - bLowerBound)

    C000 = LookupTexture[rLowerBound][gLowerBound][bLowerBound]
    C100 = LookupTexture[rUpperBound][gLowerBound][bLowerBound]
    C010 = LookupTexture[rLowerBound][gUpperBound][bLowerBound]
    C110 = LookupTexture[rUpperBound][gUpperBound][bLowerBound]
    C001 = LookupTexture[rLowerBound][gLowerBound][bUpperBound]
    C101 = LookupTexture[rUpperBound][gLowerBound][bUpperBound]
    C011 = LookupTexture[rLowerBound][gUpperBound][bUpperBound]
    C111 = LookupTexture[rUpperBound][gUpperBound][bUpperBound]

    C00 = interpolation2D(C000, C100, rDiff)
    C01 = interpolation2D(C001, C101, rDiff)
    C10 = interpolation2D(C010, C110, rDiff)
    C11 = interpolation2D(C011, C111, rDiff)

    C0 = interpolation2D(C00, C10, gDiff)
    C1 = interpolation2D(C10, C11, gDiff)
    
    interpolatedColor = interpolation2D(C0, C1, bDiff)
    interpolatedColorAsNumpyArr = np.asarray([interpolatedColor.l, interpolatedColor.a, interpolatedColor.b])
    return interpolatedColorAsNumpyArr



RGB_file = open("CorrespondingRGBVals.txt", "r")
LAB_file = open("CandidateLABvals.txt", "r")

rgbVals = RGB_file.readlines()
labVals = LAB_file.readlines()

rgbVals = [np.asarray(rgb.strip("\n").split(",")).astype(np.uint8) for rgb in rgbVals]
labVals = [np.asarray(lab.strip("\n").split(",")).astype('float') for lab in labVals]

new_RGB_file = open("AllCorrespondingRGBVals.txt", "w")
new_LAB_file = open("AllCandidateLABvals.txt", "w")

LookupTexture =  InitializeLookupTexture(256, 256, 256)

# Write existing rgb and lab values into the lookup texture
for idx in range(len(rgbVals)):
    rgb = rgbVals[idx]
    lab = labVals[idx]
    labPoint = LABData()
    labPoint.l = lab[0]
    labPoint.a = lab[1]
    labPoint.b = lab[2]

    LookupTexture[rgb[0]][rgb[1]][rgb[2]] = labPoint

for r in range(256):
    for g in range(256):
        for b in range(256):
            if r%4 == 0 and g%4 == 0 and b%4 == 0:
                labVal = LookupTexture[r][g][b]
                new_RGB_file.write(str(r)+"," + str(g)+"," + str(b)+"\n")
                new_LAB_file.write(str(labVal.l) + "," + str(labVal.a) + "," + str(labVal.b) + "\n")
            else:
                interpolatedLAB = trilinearInterpolation(LookupTexture, np.asarray([r, g, b]))
                new_RGB_file.write(str(r)+"," + str(g)+"," + str(b)+"\n")
                new_LAB_file.write(str(interpolatedLAB[0]) + "," + str(interpolatedLAB[1]) + "," + str(interpolatedLAB[2]) + "\n")

new_RGB_file.close()
new_LAB_file.close()