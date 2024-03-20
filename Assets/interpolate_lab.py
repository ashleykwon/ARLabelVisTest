import numpy as np
import matplotlib.pyplot as plt
from mpl_toolkits import mplot3d
from skimage.color import deltaE_cie76, deltaE_ciede94, deltaE_ciede2000, lab2rgb 
from tqdm import tqdm
import os
from multiprocessing import Pool
import time
import scipy


RGB_file = open("CorrespondingRGBVals.txt", "r")
LAB_file = open("CandidateLABvals.txt", "r")

rgbVals = RGB_file.readlines()
labVals = LAB_file.readlines()

rgbVals = [np.asarray(rgb.strip("\n").split(",")).astype(np.uint8) for rgb in rgbVals]
labVals = [np.asarray(lab.strip("\n").split(",")).astype('float') for lab in labVals]

new_RGB_file = open("NewCorrespondingRGBVals.txt", "w")
new_LAB_file = open("NewCandidateLABvals.txt", "w")
labVals_flat = np.asarray(labVals).flatten()
x = np.arange(0, 255, 4)
y = np.arange(0, 255, 4)
z = np.arange(0, 255, 4)
X, Y, Z = np.meshgrid(x, y, z)

interp_func = scipy.interpolate.RegularGridInterpolator(rgbVals, labVals_flat)

x1 = np.linspace(0,255, num=256)
y1 = np.linspace(0,255, num=256)
z1 = np.linspace(0,255, num=256)
X1,Y1,Z1= np.meshgrid(x1,y1,z1)
interpolated_values = interp_func(np.column_stack((X1.flatten(), Y1.flatten(), Z1.flatten())))
interpolated_values = interpolated_values.reshape(X1.shape)


for r in range(256):
    for g in range(256):
        for b in range(256):
            if r%4 == 0 and g%4 == 0 and b%4 == 0:
                continue
            else:
                interpolatedLAB = scipy.interpolate.RegularGridInterpolator(rgbVals, labVals)
                new_RGB_file.write(str(r)+"," + str(g)+"," + str(b)+"\n")
                new_LAB_file.write(str(interpolatedLAB[0]) + "," + str(interpolatedLAB[1]) + "," + str(interpolatedLAB[2]) + "\n")

new_RGB_file.close()
new_LAB_file.close()