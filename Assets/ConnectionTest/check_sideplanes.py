import numpy as np
from colormath.color_objects import LabColor
from colormath.color_diff import delta_e_cie2000, _get_lab_color1_vector, _get_lab_color2_matrix
from colormath import color_diff_matrix
import matplotlib.pyplot as plt
from mpl_toolkits.mplot3d import Axes3D

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

def getSideplanes2():
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


def plot_gamut(Cornerpoints, Sideplanes):
    fig = plt.figure()
    ax = fig.add_subplot(111, projection='3d')

    # Plot the cornerpoints
    ax.scatter(Cornerpoints[:, 0], Cornerpoints[:, 1], Cornerpoints[:, 2], c='r', marker='o', label='Cornerpoints')

    # Plot the sideplanes
    for i in range(len(Sideplanes)):
        sideplane = Sideplanes[i, :]
        normal = sideplane[:3]
        d = sideplane[3]
        
        # Generate a meshgrid for the sideplane
        x, y = np.meshgrid(np.linspace(min(Cornerpoints[:, 0]), max(Cornerpoints[:, 0]), 50),
                           np.linspace(min(Cornerpoints[:, 1]), max(Cornerpoints[:, 1]), 50))
        z = (-normal[0] * x - normal[1] * y + d) * 1.0 / normal[2]

        # Plot the sideplane surface
        ax.plot_surface(x, y, z, alpha=0.5, label=f'Sideplane {i+1}')

    ax.set_xlabel('X')
    ax.set_ylabel('Y')
    ax.set_zlabel('Z')
    ax.set_title('3D Gamut Visualization')
    ax.axes.set_xlim3d(left=-200, right=200) 
    ax.axes.set_ylim3d(bottom=-200, top=200) 
    ax.axes.set_zlim3d(bottom=-200, top=200)
    # ax.legend()
    plt.show()


sideplanes = getSideplanes2()

x1 = np.array([72, 0, -2])
# See whether the starting point is within the gamut
for i in range(12):
    if np.sign(np.dot(x1, sideplanes[i, :3]) - sideplanes[i, 3]) != np.sign(np.dot([50, 0, 0], sideplanes[i, :3]) - sideplanes[i, 3]):
        print("starting point not in gamut")
# Call the visualization function
# plot_gamut(Cornerpoints, sideplanes)