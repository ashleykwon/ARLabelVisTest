
# SampleScene explained:
## General ##
This scene contains 2 layers, Label and Default, each of which is used by Main Camera and Camera to render the label plane or everything else without the plane


<h2>Scene components</h2> 
* Plane: this is the plane onto which the label and the background are rendered. This belongs to the Label layer, so it can’t be seen by Camera.
* Spheres: Yuanbo attached Materials to these to test label color generation when there’re objects with different colors in the scene. These belong to the Default layer, so they are seen by Camera.
* Player: this is an empty object that contains Main Camera and Camera
* Main Camera: this camera only renders Plane
* Camera: this camera does the following:
  1. Takes screenshot of the scene
  2. Uses the screenshot to generate label colors
  3. Renders the generated label colors + background from 2 on Plane 
