David Correia 93576

## This Repository contains the files related to my Thesis Final Delivery

The components are:

* Defesa da Tese.pptx - The PowerPoint used to defend the thesis without the videos, due to size limits
* Projects - Folder containing the entire project including the 3 performed experiments and all the used assets
* Thesis_EyeDepth_Hands_free_Selection_in_VR.pdf - PDF containing the final version of the thesis

---

# Project Explanation:

This project was made using **Varjo Aero**.

All the scripts for the experiments controllers and selection interfaces behaviour are used as components the **Main Camera** GameObject located in 'XR Origin (XR Rig) > Camera Offset > Main Camera' inside the Scene.

---

## Part 1 - Running program

### Important

Before running any experiment, read Part 2

To run Scenes inside unity, open File > Build Settings and select between one of the two main options (only one at a time):

* PointMatrixScene - To execute the first experiment, used for the VergenceStudy
* TestVergenceTechnique - To execute the second and third experiments, used for testing the different confirmation and selection interfaces that were developed.
  * Second experiment execution: select 'Pilot Test Controller.cs' script and set it to active while deactivating the 'Final Test Controller.cs'
  * Third experiment execution: select 'Final Test Controller.cs' script and set it to active while deactivating the 'Pilot Test Controller.cs'

---

## Part 2 - Public variables in each Script

Inside Main Camera there are some scripts that need to be edited manually.

For the VarjoVergenceHandlerPrototypeOutlines.cs script:

* OutLine Scale Factor - Size of the outline when selecting an object, 1.15 was the one used for the experiments.
* Selection Method and Confirmation Method - Not important if any of the Experiment Controllers are in use
* Debug Objects - Open the Debug Objects folder and drag each of the gameObjects to the correct public variable by matching the names
* Circular Plane - Same as Debug Objects but inside Environment folder
* Mean Position Count - Number of eye gaze data collected used when applying the algorithm. During the experiments, 5 was the number of data samples used at each instant for the mean
* On to Target - All these are the colors used for different objects (some were used for testing only during development). The ones used in order are: FF0000; FFB6E3; FF7600; D44900; FAEFCD; DAB9B9; C6CCE9; FBFBFB; B1B1B1; 000E9F; 00FF00
* Outline Materials - These 3 materials are located in 'Assets > PointMatrix > Materials' folder inside the Project. Like before just drag the matching names
* Text Mesh - Used for debugging during development. Inside the Debug Objects folder the 'Debug Text' object can be found and dragged to this field
* Objects Offset - This field contains the experimental scenario, the DNA Molecule. Drag the GameObject 'Objects Offset' to this field and make sure it is not activating when using one of the experiment controllers and vice-versa
* Debug Sound - This sound was used for debugging during development and can be found inside the Debug Objects folder

For the FinalTestController.cs script:

* Rate Menu to ShortBreakScreen - These Transforms are located in Environment inside the Scene and are used to guide users during the experiment tasks. Just drag the matching names.
* Objects Offset - This field contains the experimental scenario, the DNA Molecule. Drag the GameObject 'Objects Offset' to this field.
* Block A-D - These Transforms are used to identify the current Block during the experiment. They are located in Block inside the Scene.

---

## Part 3 - How to use a different VR Head Mounted Display (HMD)

Some sections of VarjoVergenceHandlerPrototypeOutlines.cs need to be changed since this script was made to work with Varjo HMDs specifically. The following sections in the script need to be changed if the used HMD is not a Varjo product:

* Lines 211-215 - This section makes sure that the gaze tracking is working like intended. It can be simply be adapted to the new library being used.
* Lines 290-337 - This section gets the neccessary data from the eyes using the Varjo Library. That section need to be changed according to the new library being used. In line 337 the function calculateDistance() applies the algorithm and gets all the necessary data for the rest of the script. Make sure that the following data is sent as input in this order:
  * left eye origin point in the 3D space
  * right eye origin point in the 3D space
  * left eye gaze direction
  * right eye gaze direction



