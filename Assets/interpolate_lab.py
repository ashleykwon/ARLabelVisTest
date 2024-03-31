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

def RGBToLAB(RGB):
    RGB = np.array(RGB) / 255.0
    mask = RGB > 0.04045

    RGB[mask] = ((RGB[mask] + 0.055) / 1.055) ** 2.4
    RGB[~mask] /= 12.92
    RGB *= 100


    XYZ = np.dot(RGB, np.array([[0.4124, 0.3576, 0.1805],
                                [0.2126, 0.7152, 0.0722],
                                [0.0193, 0.1192, 0.9505]]))

    XYZ /= np.array([95.047, 100.0, 108.883])
    mask = XYZ > 0.008856
    XYZ[mask] = XYZ[mask] ** (1/3)
    XYZ[~mask] = (7.787 * XYZ[~mask]) + (16/116)

    L = (116 * XYZ[1]) - 16
    a = 500 * (XYZ[0] - XYZ[1])
    b = 200 * (XYZ[1] - XYZ[2])

    return np.asarray([L, a, b])


def interpolation2D(lab1, lab2, diff):
    l = lab1.l*(1-diff) + lab2.l*diff
    a = lab1.a*(1-diff) + lab2.a*diff
    b = lab1.b*(1-diff) + lab2.b*diff
    interpolatedLAB = LABData()
    interpolatedLAB.l = l
    interpolatedLAB.a = a
    interpolatedLAB.b = b
    return interpolatedLAB


def trilinearInterpolation(LookupTexture, rgbAsIndices, lookupTableStepSize):
    rIdx = rgbAsIndices[0]
    gIdx = rgbAsIndices[1]
    bIdx = rgbAsIndices[2]
    # lookupTableStepSize = 4

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


# def modifiedInterpolation(LookupTexture, rgbAsIndices, lookupTableStepSize):
#     rIdx = rgbAsIndices[0]
#     gIdx = rgbAsIndices[1]
#     bIdx = rgbAsIndices[2]
#     # lookupTableStepSize = 4

#     rLowerBound = int(rIdx // lookupTableStepSize) * lookupTableStepSize
#     rUpperBound = min(rLowerBound + lookupTableStepSize, 252)

#     gLowerBound = int(gIdx // lookupTableStepSize) * lookupTableStepSize
#     gUpperBound = min(gLowerBound + lookupTableStepSize, 252)

#     bLowerBound = int(bIdx // lookupTableStepSize) * lookupTableStepSize
#     bUpperBound = min(bLowerBound + lookupTableStepSize, 252)

#     C000 = [rLowerBound, gLowerBound, bLowerBound]
#     C100 = [rUpperBound, gLowerBound, bLowerBound]
#     C010 = [rLowerBound, gUpperBound, bLowerBound]
#     C110 = [rUpperBound, gUpperBound, bLowerBound]
#     C001 = [rLowerBound, gLowerBound, bUpperBound]
#     C101 = [rUpperBound, gLowerBound, bUpperBound]
#     C011 = [rLowerBound, gUpperBound, bUpperBound]
#     C111 = [rUpperBound, gUpperBound, bUpperBound]

#     neighboringPoints = [C000, C100, C010, C110, C001, C101, C011, C111]
    
#     diff = np.asarray([0, 0, 0])
#     nearestRGB = np.asarray([0, 0, 0])
#     currentRGBToLAB = RGBToLAB(rgbAsIndices)
#     minDistance = 100000 # initial value
#     for i in range(len(neighboringPoints)):
#         neighborLAB = RGBToLAB(neighboringPoints[i])
#         currentDiff = currentRGBToLAB - neighborLAB
#         distance = np.linalg.norm(currentDiff)
#         if distance < minDistance:
#             minDistance = distance
#             diff = currentDiff
#             nearestRGB = neighboringPoints[i]

#     farthestLAB = LookupTexture[nearestRGB[0]][nearestRGB[1]][nearestRGB[2]]
#     currentRGBsFarthestLAB = [farthestLAB.l + diff[0], farthestLAB.a + diff[1], farthestLAB.b + diff[2]]

#     return np.asarray(currentRGBsFarthestLAB)

def interpolationWithUniqueLAB(uniqueLABVals, correspondingRGBValsToLAB, rgbAsIndices):
    rgbAsLAB = RGBToLAB(rgbAsIndices)
    distances = np.asarray([np.linalg.norm(rgbAsLAB - LAB) for LAB in correspondingRGBValsToLAB])
    diffs = np.asarray([rgbAsLAB - LAB for LAB in correspondingRGBValsToLAB])
    distances = np.asarray([np.linalg.norm(diff) for diff in diffs])
    minDistanceIdx = np.argmin(distances)
    return uniqueLABVals[minDistanceIdx]+distances[minDistanceIdx]



RGB_file = open("CorrespondingRGBVals.txt", "r")
LAB_file = open("CandidateLABvals.txt", "r")

rgbVals = RGB_file.readlines()
labVals = LAB_file.readlines()

rgbVals = [np.asarray(rgb.strip("\n").split(",")).astype(np.uint8) for rgb in rgbVals]
labVals = [np.asarray(lab.strip("\n").split(",")).astype('float') for lab in labVals]

labVals = np.asarray(labVals)
uniqueLABVals, uniqueLABValsIndices = np.unique(labVals, axis=0, return_index = True)
correspondingRGBVals = np.take(rgbVals, uniqueLABValsIndices, axis=0)
correspondingRGBValsToLAB = np.asarray([RGBToLAB(rgb) for rgb in correspondingRGBVals])

new_RGB_file = open("AllCorrespondingRGBVals2.txt", "w")
new_LAB_file = open("AllCandidateLABvals2.txt", "w")

LookupTexture =  InitializeLookupTexture(256, 256, 256)

# Write existing rgb and lab values into the lookup texture
# for idx in range(len(rgbVals)):
#     rgb = rgbVals[idx]
#     lab = labVals[idx]
#     labPoint = LABData()
#     labPoint.l = lab[0]
#     labPoint.a = lab[1]
#     labPoint.b = lab[2]

#     LookupTexture[rgb[0]][rgb[1]][rgb[2]] = labPoint

# Save unique LAB values to the lookup texture 
for idx in range(len(correspondingRGBVals)):
    rgb = correspondingRGBVals[idx]
    lab = uniqueLABVals[idx]
    labPoint = LABData()
    labPoint.l = lab[0]
    labPoint.a = lab[1]
    labPoint.b = lab[2]

    LookupTexture[rgb[0]][rgb[1]][rgb[2]] = labPoint

# for i in range(len(labVals)):
#     if labVals[i][0] >= 50 and labVals[i][0] < 57 and labVals[i][1] < 100 and labVals[i][1] > -100 and labVals[i][2] < 100 and labVals[i][2] > -100:
#         print("candidate center LAB val: " +str(labVals[i]))
#         print("candidate center RGB val: " +str(rgbVals[i]))
#         print("\n")


for r in range(0, 256, 128):
    for g in range(0, 256, 128):
        for b in range(0, 256, 128):
            if hasattr(LookupTexture[r][g][b],'l') and hasattr(LookupTexture[r][g][b],'a') and hasattr(LookupTexture[r][g][b],'b')  :
                labVal = LookupTexture[r][g][b]
                new_RGB_file.write(str(r)+"," + str(g)+"," + str(b)+"\n")
                new_LAB_file.write(str(labVal.l) + "," + str(labVal.a) + "," + str(labVal.b) + "\n")
            else:
                interpolatedLAB = interpolationWithUniqueLAB(uniqueLABVals, correspondingRGBValsToLAB, np.asarray([r, g, b]))
                new_RGB_file.write(str(r)+"," + str(g)+"," + str(b)+"\n")
                new_LAB_file.write(str(interpolatedLAB[0]) + "," + str(interpolatedLAB[1]) + "," + str(interpolatedLAB[2]) + "\n")




# for r in range(256):
#     for g in range(256):
#         for b in range(256):
#             if r%4 == 0 and g%4 == 0 and b%4 == 0:
#                 labVal = LookupTexture[r][g][b]
#                 new_RGB_file.write(str(r)+"," + str(g)+"," + str(b)+"\n")
#                 new_LAB_file.write(str(labVal.l) + "," + str(labVal.a) + "," + str(labVal.b) + "\n")
#             else:
#                 interpolatedLAB = trilinearInterpolation(LookupTexture, np.asarray([r, g, b]), 4)
#                 new_RGB_file.write(str(r)+"," + str(g)+"," + str(b)+"\n")
#                 new_LAB_file.write(str(interpolatedLAB[0]) + "," + str(interpolatedLAB[1]) + "," + str(interpolatedLAB[2]) + "\n")


new_RGB_file.close()
new_LAB_file.close()