import numpy as np
from colormath.color_objects import LabColor
from colormath.color_diff import delta_e_cie2000, _get_lab_color1_vector, _get_lab_color2_matrix
from colormath import color_diff_matrix
import matplotlib.pyplot as plt
from mpl_toolkits.mplot3d import Axes3D

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


end_count = 0

def minafstand(x, soort, Points, sideplanes):
    # Test whether x is in the gamut
    error = 0
    for i in range(12):
        if np.sign(np.dot(x, sideplanes[i, :3]) - sideplanes[i, 3]) != np.sign(np.dot([50, 0, 0], sideplanes[i, :3]) - sideplanes[i, 3]):
            print("ending point not in gamut")
            error = 1

    if error == 0: end_count += 1
    # Set an initial value for "distance", this should be bigger than the maximal distance
    distance = 100
    number = 0
    for m in range(len(a)):
        # "soort 1" is ciede2000
        if soort == 1:
            distance1 = array_delta_e_cie2000(x, Points[m, :], 2, 1, 1)
        # "soort 2" is the Euclidean norm
        elif soort == 2:
            distance1 = np.linalg.norm(x - Points[m, :])

        # See if the distance is smaller than the distance to all points before
        if distance1 <= distance:
            distance = distance1
            # denote the closest point
            number = m

    minimumafstand = distance

    return minimumafstand, error, number


L = np.array([52, 56, 34, 61, 41, 76, 43, 86, 71, 80, 56, 93, 99, 13])
a = np.array([74, -45, 5, -16, 6, 24, 64, 4, 0, -26, -21, -8, 0, 2])
b = np.array([53, 26, -49, -42, 27, 67, -24, 85, -2, 68, -29, -9, 0, 0])
L = np.array([52])
a = np.array([74])
b = np.array([53])
Points = np.column_stack((L, a, b))

# Calculate the sideplanes
sideplanes = getSideplanes2()  # You need to implement this function

# Set the initial values for our new point
dx1 = np.array([0, 0, 0])
ddx1 = np.array([0, 0, 0])

# Set the values of the constants
k = 10
gamma = 0.1
c = 0.2
dt = 0.1
bestdistance = 0
bestpoint = np.array([50, 0, 0])

count = 0
# Run the algorithm for 300 random starting points
for start_point_num in range(1):
    
    buiten = 0
    x1 = np.array([100 * np.random.rand(), 200 * np.random.rand()-100, 200 * np.random.rand()-100])
    x1 = np.array([72, 0, -2])
    # print("x1: ", x1)
    # See whether the starting point is within the gamut
    for i in range(12):
        if np.sign(np.dot(x1, sideplanes[i, :3]) - sideplanes[i, 3]) != np.sign(np.dot([50, 0, 0], sideplanes[i, :3]) - sideplanes[i, 3]):
            # print("starting point not in gamut")
            
            buiten = 1

    if buiten == 0:
        count += 1
        # If the starting point is in the gamut, let the coulomb repulsion algorithm run for 2000 timesteps
        num_itr = 20
        for it in range(num_itr):
            x2 = x1 + dx1 * dt
            dx2 = dx1 + ddx1 * dt

            # Calculate the contribution of the old points to ddx2
            sum_term = np.zeros(3)
            for i in range(len(a)):
                # colormath.color_diff.delta_e_cie2000(color1, color2, Kl=1, Kc=1, Kh=1)
                sum_term += (1 / np.power(array_delta_e_cie2000(x2, Points[i, :], 2, 1, 1), 3)) * (x2 - Points[i, :])

            # Calculate the closest boundary point
            mindistance = 300
            for j in range(12):
                time = np.dot(sideplanes[j, :3], x2) + sideplanes[j, 3] / np.dot(sideplanes[j, :3], sideplanes[j, :3])
                b = x2 + time * sideplanes[j, :3]
                distance = array_delta_e_cie2000(x2, b, 2, 1, 1)
                if distance <= mindistance:
                    mindistance = distance

            ddx2 = -gamma * dx2 + k * sum_term + k * c / np.power(array_delta_e_cie2000(x2, b, 2, 1, 1), 3) * (x2 - b)

            # Set the new values for x, dx, and ddx
            x1 = x2
            dx1 = dx2
            ddx1 = ddx2
            if start_point_num % 100 == 0 or it % 10 == 0:
                print("x2: ", x2, "dx2: ", dx2, "ddx2: ", ddx2)

        # Calculate the minimal distance of the point obtained
        min_distance, fout, number = minafstand(x1, 1, Points, sideplanes)
        if fout == 0:
            if min_distance >= bestdistance:
                bestpoint = x1
                bestdistance = min_distance
                print(f"Best Point: {bestpoint}, Best Distance: {bestdistance}, Index: {number}")

print("Final Best Point:", bestpoint)
print("Final Best Distance:", bestdistance)
print("count: ", count) # number of points in the gamut
print("end_count: ", end_count)





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
    # ax.legend()
    plt.show()

# Call the function to get Cornerpoints and Sideplanes
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

# Sideplanes = getSideplanes2()

# Call the visualization function
# plot_gamut(Cornerpoints, Sideplanes)

# Given data
a = np.array([74, -45, 5, -16, 6, 24, 64, 4, 0, -26, -21, -8, 0, 2, -138])
b = np.array([53, 26, -49, -42, 27, 67, -24, 85, -2, 68, -29, -9, 0, 0, 91])
L = np.array([52, 56, 34, 61, 41, 76, 43, 86, 71, 80, 56, 93, 100, 13, 83])
C = np.array([
    [0.95, 0.07, 0.174],
    [0, 0.6, 0.36],
    [0, 0.33, 0.61],
    [0, 0.62, 0.84],
    [0.46, 0.36, 0.22],
    [1, 0.66, 0.3],
    [0.73, 0.13, 0.54],
    [1, 0.82, 0.28],
    [0.67, 0.68, 0.69],
    [0.69, 0.82, 0.33],
    [0, 0.58, 0.71],
    [0.82, 0.94, 0.98],
    [0, 0, 0],
    [0.13, 0.12, 0.13],
    [0, 1, 0]
])

# Create a 3D scatter plot
fig = plt.figure()
ax = fig.add_subplot(111, projection='3d')
ax.scatter(a, b, L, c=C, marker='o', s=60, depthshade=True)

# Set labels and title
ax.set_xlabel('a')
ax.set_ylabel('b')
ax.set_zlabel('L')
ax.set_title('Points in Cielab space')

# plt.show()