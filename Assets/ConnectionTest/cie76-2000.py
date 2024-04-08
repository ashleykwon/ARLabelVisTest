import numpy as np
from colormath.color_objects import LabColor
from colormath.color_diff import delta_e_cie2000, _get_lab_color1_vector, _get_lab_color2_matrix
from colormath import color_diff_matrix
import matplotlib.pyplot as plt
from mpl_toolkits.mplot3d import Axes3D
from scipy.linalg import det, solve

def delta_e_cie2000_redefined(color1, color2, Kl=1, Kc=1, Kh=1):
    """
    Calculates the Delta E (CIE2000) of two colors.
    """
    color1_vector = _get_lab_color1_vector(color1)
    color2_matrix = _get_lab_color2_matrix(color2)
    
    delta_e = color_diff_matrix.delta_e_cie2000(
        color1_vector, color2_matrix, Kl=Kl, Kc=Kc, Kh=Kh)[0]
    # print(type(delta_e), delta_e)
    return delta_e

def array_delta_e_cie2000(vec1, vec2, Kl=2, Kc=1, Kh=1):
    color1 = LabColor(lab_l=vec1[0], lab_a=vec1[1], lab_b=vec1[2])
    color2 = LabColor(lab_l=vec2[0], lab_a=vec2[1], lab_b=vec2[2])
    
    res = delta_e_cie2000_redefined(color1, color2, Kl, Kc, Kh)
    return res

def getSideplanes():
    # The cornerpoints of the gamut
    Cornerpoints = np.array([
        [0, 0, 0],
        [33, 80, -109],
        [67, 105, -52],
        [61, 90, 75],
        [97, -23, 105],
        [83, -138, 91],
        [87, -78, -21],
        [100, 0, 0]
    ])

    # Calculate the formulas for the sideplanes of the gamut.
    # Here the i-th row of Sideplanes generates the formula the following way:
    # "Sideplanes(i, 0:2) dot (x, y, z) = Sideplanes(i, 3)"
    Sideplanes = np.zeros((12, 4))

    # 1: ABG
    normal = np.cross((Cornerpoints[0, :] - Cornerpoints[1, :]), (Cornerpoints[0, :] - Cornerpoints[7, :]))
    Sideplanes[0, :] = np.concatenate((normal, [np.dot(normal, Cornerpoints[1, :])]))

    # 2: AFG
    normal = np.cross((Cornerpoints[0, :] - Cornerpoints[6, :]), (Cornerpoints[0, :] - Cornerpoints[7, :]))
    Sideplanes[1, :] = np.concatenate((-normal, [-np.dot(normal, Cornerpoints[0, :])]))

    # 3: ABD
    normal = np.cross((Cornerpoints[0, :] - Cornerpoints[1, :]), (Cornerpoints[0, :] - Cornerpoints[4, :]))
    Sideplanes[2, :] = np.concatenate((-normal, [-np.dot(normal, Cornerpoints[0, :])]))

    # 4: BDC
    normal = np.cross((Cornerpoints[1, :] - Cornerpoints[2, :]), (Cornerpoints[1, :] - Cornerpoints[4, :]))
    Sideplanes[3, :] = np.concatenate((-normal, [-np.dot(normal, Cornerpoints[1, :])]))

    # 5: ADF
    normal = np.cross((Cornerpoints[0, :] - Cornerpoints[4, :]), (Cornerpoints[0, :] - Cornerpoints[6, :]))
    Sideplanes[4, :] = np.concatenate((-normal, [-np.dot(normal, Cornerpoints[0, :])]))

    # 6: DEF
    normal = np.cross((Cornerpoints[4, :] - Cornerpoints[5, :]), (Cornerpoints[4, :] - Cornerpoints[6, :]))
    Sideplanes[5, :] = np.concatenate((-normal, [-np.dot(normal, Cornerpoints[4, :])]))

    # 7 - 12: HBC, HCD, HDE, HEF, HFG, HGB
    for i in range(6):
        normal = np.cross(Cornerpoints[7, :] - Cornerpoints[1 + i, :], Cornerpoints[7, :] - Cornerpoints[2 + (i % 6), :])
        Sideplanes[6 + i, :] = np.concatenate((normal, [np.dot(normal, Cornerpoints[7, :])]))

    print("Sideplanes:", Sideplanes)
    return Sideplanes

def getSideplanes2():
    # The cornerpoints of the gamut
    Cornerpoints = np.array([
        [0, 0, 0],      # A
        [33, 80, -109], # B
        [67, 105, -52], # C
        [61, 90, 75],   # D
        [97, -23, 105], # E
        [83, -138, 91], # F
        [87, -78, -21], # G
        [100, 0, 0]     # H
    ])

    # Calculate the formulas for the sideplanes of the gamut.
    # Here the i-th row of Sideplanes generates the formula the following way:
    # "Sideplanes(i, 0:2) dot (x, y, z) = Sideplanes(i, 3)" -- a*x + b*y + c*z = d
    Sideplanes = np.zeros((12, 4))

    # 1: ABG
    normal = np.cross((Cornerpoints[0, :] - Cornerpoints[1, :]), (Cornerpoints[0, :] - Cornerpoints[6, :]))
    Sideplanes[0, :] = np.concatenate((normal, [np.dot(normal, Cornerpoints[1, :])]))

    # 2: AFG
    normal = np.cross((Cornerpoints[0, :] - Cornerpoints[5, :]), (Cornerpoints[0, :] - Cornerpoints[6, :]))
    Sideplanes[1, :] = np.concatenate((-normal, [-np.dot(normal, Cornerpoints[0, :])]))

    # 3: ABD
    normal = np.cross((Cornerpoints[0, :] - Cornerpoints[1, :]), (Cornerpoints[0, :] - Cornerpoints[3, :]))
    Sideplanes[2, :] = np.concatenate((-normal, [-np.dot(normal, Cornerpoints[0, :])]))

    # 4: BDC
    normal = np.cross((Cornerpoints[1, :] - Cornerpoints[2, :]), (Cornerpoints[1, :] - Cornerpoints[3, :]))
    Sideplanes[3, :] = np.concatenate((-normal, [-np.dot(normal, Cornerpoints[1, :])]))

    # 5: ADF
    normal = np.cross((Cornerpoints[0, :] - Cornerpoints[3, :]), (Cornerpoints[0, :] - Cornerpoints[5, :]))
    Sideplanes[4, :] = np.concatenate((-normal, [-np.dot(normal, Cornerpoints[0, :])]))

    # 6: DEF
    normal = np.cross((Cornerpoints[3, :] - Cornerpoints[4, :]), (Cornerpoints[3, :] - Cornerpoints[5, :]))
    Sideplanes[5, :] = np.concatenate((-normal, [-np.dot(normal, Cornerpoints[3, :])]))

    # 7 - 12: HBC, HCD, HDE, HEF, HFG, HGB
    for i in range(7, 13):
        normal = np.cross(Cornerpoints[7, :] - Cornerpoints[i - 6, :], Cornerpoints[7, :] - Cornerpoints[1 + ((i-6) % 6), :])
        Sideplanes[i-1, :] = np.concatenate((normal, [np.dot(normal, Cornerpoints[7, :])]))

    print("Sideplanes:", Sideplanes)
    return Sideplanes

# The coordinates of the color-points in CIElab
L = np.array([52, 56, 35, 61, 41, 76, 43, 86, 71, 80, 56, 93, 99, 13])
a = np.array([74, -45, 7, -16, 6, 24, 64, 4, 0, -26, -21, -8, 0, 2])
b = np.array([53, 26, -43, -42, 27, 67, -24, 85, -2, 68, -29, -9, 0, 0])
Points = np.column_stack((L, a, b))

# Calculate the sideplanes
sideplanes = getSideplanes2()  

# Test whether all points are in the gamut
for n in range(len(L)):
    for i in range(12):
        if np.sign(np.dot(Points[n, :], sideplanes[i, :3]) - sideplanes[i, 3]) != \
           np.sign(np.dot([50, 0, 0], sideplanes[i, :3]) - sideplanes[i, 3]):
            print(f"Point {n+1} is outside sideplane {i+1}")

# Ask the user for which point he/she wants to calculate the corner points
for n in range(len(L)):
    print(f"\nCalculating corner points for Point {n+1}")
    
    # Calculate the bisectors and store them in Planefunction
    Planefunction = np.zeros((len(L), 4))
    for i in range(len(L)):
        if i != n:
            middel = (Points[n, :] + Points[i, :]) / 2
            normal = Points[n, :] - Points[i, :]
            Planefunction[i, :] = np.concatenate((normal, [np.dot(normal, middel)]))

    # Put together Planefunction and Sideplanes
    Planes = np.concatenate((Planefunction, sideplanes), axis=0)

    # Calculate all intersecting points of three planes
    for i in range(len(L) + 12 - 2):
        if i != n:
            for j in range(i + 1, len(L) + 12 - 1):
                if j != n:
                    for k in range(j + 1, len(L) + 12):
                        if k != n:
                            A = Planes[[i, j, k], :3]
                            D = det(A)
                            if np.abs(D) >= 1e-10:
                                b = Planes[[i, j, k], 3]
                                x = solve(A, b)
                                error = 0
                                l = 1

                                while l <= (len(L) + 12) and error <= 1:
                                    if l != n:
                                        errorx = np.sign(np.round(100 * (np.dot(Planes[l, :3], x) - Planes[l, 3])))
                                        errorn = np.sign(np.dot(Planes[l, :3], Points[n, :]) - Planes[l, 3])
                                        error = np.abs(errorx - errorn)
                                    l += 1

                                if error <= 1:
                                    distance1 = 100
                                    distance = 100
                                    for m in range(len(L)):
                                        # distance1 = np.linalg.norm(x - Points[m, :], ord=2)
                                        distance1 = array_delta_e_cie2000(x , Points[m, :] ,2, 1, 1)
                                        if distance1 <= distance:
                                            distance = distance1
                                            number = m + 1
                                    if distance >= 30:
                                        print(f"Point {number}:", x, "Distance:", distance)

