import numpy as np
import math
import matplotlib.pyplot as plt
import matplotlib as mpl

def LAB2RGB(LAB):
    L = LAB[0]
    A = LAB[1]
    B = LAB[2]

    Xr = 95.047; 
    Yr = 100.0
    Zr = 108.883

    var_Y = (L + 16.0) / 116.0
    var_X = A / 500 + var_Y
    var_Z = var_Y - B / 200.0

    if (var_Y**3  > 0.008856):
            var_Y = math.pow(var_Y, 3.0)
    else:
        var_Y = (var_Y - 16 / 116) / 7.787

    if (math.pow(var_X, 3)  > 0.008856):
            var_X = math.pow(var_X, 3.0) 
    else:
            var_X = (var_X - 16 / 116) / 7.787
    if (math.pow(var_Z, 3)  > 0.008856):
            var_Z = math.pow(var_Z, 3.0)
    else:
            var_Z = (var_Z - 16.0 / 116.0) / 7.787
            
    X = var_X * Xr
    Y = var_Y * Yr
    Z = var_Z * Zr

    X /= 100.0
    Y /= 100.0
    Z /= 100.0

    var_R = var_X *  3.2406 + var_Y * -1.5372 + var_Z * -0.4986
    var_G = var_X * -0.9689 + var_Y *  1.8758 + var_Z *  0.0415
    var_B = var_X *  0.0557 + var_Y * -0.2040 + var_Z *  1.0570

    if (var_R > 0.0031308):
        var_R = 1.055 * (math.pow(var_R, (1 / 2.4))) - 0.055
        
    else:
        var_R = 12.92 * var_R
        
            
    if (var_G > 0.0031308):
            var_G = 1.055 * (math.pow(var_G, (1 / 2.4))) - 0.055    
    else:
        var_G = 12.92 * var_G

    if (var_B > 0.0031308):
        var_B = 1.055 * (math.pow(var_B, (1 / 2.4))) - 0.055  
    else:
        var_B = 12.92 * var_B
        

    finalR = (int) (max(min(var_R*255, 255), 0))
    finalG = (int) (max(min(var_G*255, 255), 0))
    finalB = (int) (max(min(var_B*255, 255), 0))

    return np.asarray([finalR, finalG, finalB])

RGB_file = open("Fixed/AllCorrespondingRGBVals_FixedRGB_20000_100Voxel_D76_sigma2_nn.txt", "r")
LAB_file = open("Fixed/AllCandidateLABvals_FixedRGB_20000_100Voxel_D76_sigma2_nn.txt", "r")

rgbVals = RGB_file.readlines()
labVals = LAB_file.readlines()

rgbVals = [np.asarray(rgb.strip("\n").split(",")).astype(np.uint8) for rgb in rgbVals]
labVals = [np.asarray(lab.strip("\n").split(",")).astype('float') for lab in labVals]

temp = np.zeros((256, 256, 256, 3))
labVals = np.asarray(labVals)
rgbVals = np.asarray(rgbVals)


for rgb, lab in zip(rgbVals, labVals):
        temp[rgb[0], rgb[1], rgb[2]] = LAB2RGB(lab)

grad_x, grad_y, grad_z, grad_c = np.gradient(temp)

magnitude = np.sqrt(grad_x**2 + grad_y**2 + grad_z**2)

# print("Maximum: ")
# print(np.max(magnitude))
# print("Minimum: ")
# print(np.min(magnitude))
# print("Mean: ")
# print(np.mean(magnitude))
# print("Sum: ")
# print(np.sum(magnitude))

new_file = open("Fixed/gradients.txt", "w")

for r in range(0, 256):
    for g in range(0, 256):
        for b in range(0, 256):           
            new_file.write(str(magnitude[r][g][b][0]) + "," + str(magnitude[r][g][b][1]) + "," + str(magnitude[r][g][b][2]) + "\n")

new_file.close()