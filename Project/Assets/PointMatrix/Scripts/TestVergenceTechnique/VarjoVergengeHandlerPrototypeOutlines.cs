using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Shapes;
using Varjo.XR;
using static Varjo.XR.VarjoEyeTracking;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.AffordanceSystem.Receiver.Primitives;
using VInspector;
using Unity.VisualScripting;
using System.Linq;
using UnityEngine.InputSystem.HID;
using static UnityEngine.GraphicsBuffer;
using System;

public class VarjoVergengeHandlerPrototypeOutlines : MonoBehaviour
{
    private int doubleBlinked = 0;
    private float sphereCastRadius = 1f; // Radius of the sphere cast
    private float sphereOverlapRadius = 1f; // Radius of the sphere Overlap
    [SerializeField]
    [Range(1.01f, 1.5f)]
    private float outlineScaleFactor = 1.05f;
    private float thresholdGaze = 0.93f;
    private string filePath;
    private string filepath;
    private string csvResults;
    private List<Material> changedMaterials = new();
    private List<Transform> changedTransforms = new();
    private List<Transform> selectedObjects = new();
    private MeshRenderer lastSelectedObject = null;
    private MeshRenderer lastSelectedTarget = null;
    private EyeState currentEyeState = EyeState.Open;
    private TransitionState currentTransitionState = TransitionState.Idle;
    public SelectionMethod selectionMethod = SelectionMethod.RayCast;
    public ConfirmationMethod confirmationMethod = ConfirmationMethod.Winking;

    //PILOT TEST AND FINAL TEST
    private PilotTestController pilotTestController;
    private FinalTestController finalTestController;
    private Transform currentTarget = null;
    private int tries = 0;
    private int counter = 0;
    [HideInInspector]
    public bool testStarted = false;

    //DOUBLE BLINK PARAMETERS
    private bool blinkedOnce = false;
    private bool keepTracking = true;
    //private float blinkingTime = 0;
    private float betweenBlinkingTime = 0;
    private float noTargetTime = 0;
    private int blinkCount = 0;
    private int blinkCountTest = 0;

    //DWELL PARAMETERS
    private float dwellTime = 0;
    private float dwellLeeway = 0;

    //WINKING PARAMETERS
    private WinkingEye winkingState = WinkingEye.None;
    private int blinkConeCount = 0;

    //CONECAST PARAMETERS
    private bool coneCastLocked = false;
    private List<GameObject> coneHits = new();
    private List<GameObject> miniClones = new();
    private LineRenderer ogCloneLink = null;
    private bool coneCastHitsExist = false;
    private bool circularPlaneIsGone = false;
    private bool lookingAtPlane = false;
    private bool usedCameraCoordinates = false;
    private string something = null;
    private bool useWink = true;
    private Vector3 lockedHeadPosition;
    private Vector3 lockedDirection;
    private Vector3 lockedHeadPosition2;
    private Vector3 lockedDirection2;
    private List<LineRenderer> debugRays = new();

    //PUBLIC
    public Transform debugSphere = null;
    public LineRenderer debugGazeRay = null;
    public GameObject debugCone = null;
    public Transform circularPlane = null;
    public int meanPositionCount;
    public Color on;
    public Color off;
    public Color onClone;
    public Color offClone;
    public Color set1;
    public Color set2;
    public Color set3;
    public Color set4;
    public Color set5;
    public Color confirmed;
    public Color target;    
    public Material outlineMaterial;
    public Material outlineMaterial2;
    public Material outlineMaterialClones;
    public TextMeshPro textMesh;
    public Transform objectsOffset;
    public AudioSource debugSound;

    //CONST
    private const float foveaAngleDegrees = 2.0f;
    private const float foveaAngleRadians = Mathf.Deg2Rad * foveaAngleDegrees;
    private const float coneAngle = 21f;
    private const float maxDistance = 3f;
    private const float timeForConsciousBlink = 0.1f; //Can vary betweem 100-400ms according to this work
                                                   //"Blink and wink detection as a control tool in multimodal interaction"
                                                   //Can be founf in my pdf "Ideias para técnica de seleção" end of page 4
    private const float timeToForgetTarget = 0.7f;

    private float left;
    private float right;
    private List<string> gazeFrameData = new List<string>();
    private LinkedList<float> recentRegisteredData = new();
    private Vector3 previousHeadPosition = Vector3.zero;
    private Vector3 previousDirection = Vector3.zero;
    private bool isFirstReading = true;
    private LinkedList<Vector3> recentRegisteredHeadPosition = new();
    private LinkedList<Vector3> recentRegisteredGazeDirection = new();

    private enum EyeState
    {
        Open,
        Closed
    }
    private enum TransitionState
    {
        Idle,
        Opening,
        Closing,
        StartingToClose
    }
    public enum WinkingEye
    {
        None,
        Right,
        Left,
        Both,
        Winked
    }
    public enum SelectionMethod
    {
        OverlapSphere,
        SphereCast,
        RayCast,
        ConeCast
    }
    public enum ConfirmationMethod
    {
        DoubleBlinking,
        Dwell,
        Winking,
        CircularPlane
    }
    

    private Dictionary<float, float> horizontalErrors = new Dictionary<float, float>()
    {
        { 0.3f, 0.151f },
        { 0.6f, 0.061f },
        { 0.9f, 0.033f },
        { 1.2f, 0.029f },
        { 1.5f, 0.028f }

    };

    private Dictionary<float, float> verticalErrors = new Dictionary<float, float>
    {
        { 0.3f, 0.096f },
        { 0.6f, 0.066f },
        { 0.9f, 0.063f },
        { 1.2f, 0.047f },
        { 1.5f, 0.057f }
    };

    private Dictionary<float, float> depthErrors = new Dictionary<float, float>
    {
        { 0.3f, 0.213f },
        { 0.6f, 0.316f },
        { 0.9f, 0.553f },
        { 1.2f, 0.657f },
        { 1.5f, 0.715f }
    };

    //{ 0.3f, 0.033f },
    //{ 0.6f, 0.084f },
    //{ 0.9f, 0.120f },
    //{ 1.2f, 0.345f },
    //{ 1.5f, 0.513f }

    //Check if everything is working
    void Start()
    {
        filePath = Path.Combine(Application.persistentDataPath, "DetectNoise.csv");
        filepath = Path.Combine(Application.persistentDataPath, "FPS.txt");
        File.WriteAllText(filepath, "FPS Log\n");
        if (!File.Exists(filePath))
        {
            csvResults = "Frame|leftOriginX|leftOriginY|leftOriginZ|rightOriginX|rightOriginY|rightOriginZ|leftGazeX|leftGazeY|leftGazeZ|rightGazeX|rightGazeY|rightGazeZ\n";
            csvResults = csvResults.Replace("|", ",");
            System.IO.File.WriteAllText(filePath, csvResults);
            csvResults = "";
        }

        // Check if eye tracking is supported
        if (!IsGazeAllowed())
        {
            Debug.LogError("Varjo eye tracking is not supported on this device.");
            return;
        }
        //Register all detectable Objects in environment
        cacheObjects();
        pilotTestController = GetComponent<PilotTestController>();
        finalTestController = GetComponent<FinalTestController>();

        textMesh.transform.position = new Vector3(0f, 1.3f, 3f);  
        textMesh.text = "Frame Count: " + meanPositionCount.ToString(); //check how many frames are used for the mean calculated results

        if (debugCone != null)
        {
            debugCone.GetComponent<MeshFilter>().mesh = GenerateConeMesh(1f, 1f, 16);
        }

        //setup
        int circularPlaneLayer = LayerMask.NameToLayer("CircularPlane");
        circularPlane.gameObject.layer = circularPlaneLayer;
        debugGazeRay.startWidth = 0.001f;

        MeshFilter mf = circularPlane.GetComponent<MeshFilter>();
        MeshCollider mc = circularPlane.GetComponent<MeshCollider>();

        // Make circularPlane hitbox bigger
        // 1) Clone the mesh so you don’t blow away your visual asset
        Mesh scaledMesh = Instantiate(mf.sharedMesh);

        // 2) Scale its vertices
        Vector3[] V = scaledMesh.vertices;
        float pad = 1.2f; // SCALING
        for (int i = 0; i < V.Length; i++)
            V[i] *= pad;
        scaledMesh.vertices = V;
        scaledMesh.RecalculateBounds();
        scaledMesh.RecalculateNormals();

        // 3) Assign it to the collider
        mc.sharedMesh = scaledMesh;
    }

    void Update()
    {
        float fps = 1 / Time.deltaTime;
        string logLine = Time.time.ToString("F2") + "s - FPS: " + fps.ToString("F1") + "\n";

        File.AppendAllText(filepath, logLine);
        int num1 = finalTestController.getCurrentTarget();
        textMesh.text = "Frame Count: " + meanPositionCount.ToString() + "ConeCast hits" + miniClones.Count.ToString() + "DoubleBlinks: " + doubleBlinked.ToString();
        textMesh.text += "\nCurrentPosition: " + num1.ToString() + " number of tries: " + tries.ToString() + " number of tutorial tries: " + 
            finalTestController.getTutorialOn().ToString() + counter.ToString();
        textMesh.text += "\nLeft: " + left + "Right: " + right;
        if (circularPlane.GetComponent<MeshRenderer>().enabled) { textMesh.text = "\nCircularPlane should be VISIBLE"; }
        textMesh.text += "\nIs circularPlane loop gone?: " + circularPlaneIsGone + "coneCastLocked: " + coneCastLocked;
        textMesh.text += "\nBlinks so far/count: " + blinkCountTest + "/" + blinkCount;
        textMesh.text += "\nSelected Object Exists: ";
        if (lastSelectedObject != null) { textMesh.text += "yes"; } else { textMesh.text += "no"; }
        textMesh.text += "\nNo Target Timer: " + noTargetTime;
        textMesh.text += "\nBetween Blinking time: " + betweenBlinkingTime;
        if (blinkedOnce) { textMesh.text += "True"; } else { textMesh.text += "False"; }
        textMesh.text += "\nSelect Method use is:" + selectionMethod;
        textMesh.text += "\nConfirmation Method use is:" + confirmationMethod;
        textMesh.text += "\nIs the circular Plane being intersected" + something; 
        

        //when second blink doesn't happen reset blink count
        if (blinkedOnce)
        {
            betweenBlinkingTime += Time.deltaTime;
            if (betweenBlinkingTime >= timeToForgetTarget) 
            {
                betweenBlinkingTime -= timeToForgetTarget;
                blinkCount = 0;
                blinkedOnce = false;
            }
        }

        if (IsGazeAvailable())
        {
            int numberOfRecords = GetGazeList(out List<GazeData> gazeDatas, out List<EyeMeasurements> eyeMeasurements);

            for (int i = 0; i < numberOfRecords; i++)
            {
                gazeFrameData.Clear();

                var measureGaze = gazeDatas[i];
                var measureEye = eyeMeasurements[i];
                left = measureEye.leftEyeOpenness;
                right = measureEye.rightEyeOpenness;

                #region DetectObject
                //Gaze Measures per frame
                Debug.Log(measureGaze.frameNumber);
                Debug.Log(measureGaze.captureTime);
                Debug.Log(measureGaze.status); // value for the eye tracking status of the headset(HS): 0-User doesn't have HS; 1 - User has HS but not calibrated; 2 - Data is valid
                Debug.Log(measureGaze.gaze); //Gaze ray combined from both eyes
                Debug.Log(measureGaze.focusDistance); //The distance between eye and focus point in meters. Values are between 0 and 2 meters.
                Debug.Log(measureGaze.focusStability); //Value are between 0.0 and 1.0, where 0.0 indicates least stable focus and 1.0 most stable
                Debug.Log(measureGaze.leftStatus); //0-eye is shut; 1-eye is visible but not reliably tracked; 2- tracked but quality comprimised; 3-eye is tracked
                Debug.Log(measureGaze.left); //Gaze ray for the left eye
                Debug.Log(measureGaze.rightStatus);
                Debug.Log(measureGaze.right);


                gazeFrameData.Add(measureGaze.frameNumber.ToString());
                gazeFrameData.Add(measureGaze.captureTime.ToString());

                gazeFrameData.Add(measureGaze.status.ToString());
                gazeFrameData.Add(measureGaze.gaze.origin.x.ToString());
                gazeFrameData.Add(measureGaze.gaze.origin.y.ToString());
                gazeFrameData.Add(measureGaze.gaze.origin.z.ToString());
                gazeFrameData.Add(measureGaze.gaze.forward.x.ToString());
                gazeFrameData.Add(measureGaze.gaze.forward.y.ToString());
                gazeFrameData.Add(measureGaze.gaze.forward.z.ToString());

                var leftGaze = measureGaze.left.forward;
                var leftOrigin = measureGaze.left.origin;
                var rightGaze = measureGaze.right.forward;
                var rightOrigin = measureGaze.right.origin;
                //csvResults = $"{measureGaze.frameNumber},{leftOrigin.x},{leftOrigin.y},{leftOrigin.z},{rightOrigin.x},{rightOrigin.y},{rightOrigin.z}," +
                //    $"{leftGaze.x},{leftGaze.y},{leftGaze.z},{rightGaze.x},{rightGaze.y},{rightGaze.z}\n";
                float calculatedDistance;
                Vector3 Pm;
                Vector3 headPosition;
                calculateDistance(leftOrigin, rightOrigin, leftGaze, rightGaze, out calculatedDistance, out Pm, out headPosition);
                csvResults = $"{calculatedDistance}\n";
                System.IO.File.AppendAllText(filePath, csvResults);

                //Keep a mean of the last readings for the calculatedDistance
                if (calculatedDistance < 5f)
                {
                    recentRegisteredData.AddFirst(calculatedDistance);

                    if (recentRegisteredData.Count > meanPositionCount)
                    {
                        recentRegisteredData.RemoveLast();
                    }
                }
                calculatedDistance = meanCalculatedDistance(recentRegisteredData);

                //calculate size of detection spheres
                sphereOverlapRadius = getSphereSize(calculatedDistance);
                sphereCastRadius = getSphereSize(calculatedDistance);
                headPosition = transform.rotation * headPosition + transform.position;
                if (isFirstReading) { previousHeadPosition = headPosition; }

                //minimum size for detection
                if (sphereOverlapRadius < 0.0697f) sphereOverlapRadius = 0.0697f; //size of biggest target

                Vector3 positionCurrent = transform.rotation * Pm + transform.position;
                Vector3 direction = (positionCurrent - transform.position).normalized;
                if (isFirstReading) { previousDirection = direction; isFirstReading = false; }
                // bool isExpected = Vector3.Dot(direction, previousDirection) > thresholdGaze;
                HashSet<Material> currentHitMaterials = new();
                HashSet<Transform> currentHitTransforms = new();

                //if (Vector3.Distance(headPosition, previousHeadPosition) < 1f)
                //{
                //    recentRegisteredHeadPosition.AddFirst(headPosition);

                //    if (recentRegisteredHeadPosition.Count > meanPositionCount)
                //    {
                //        recentRegisteredHeadPosition.RemoveLast();
                //    }
                //}

                //Keep a mean of the last readings for gazeDirection
                if (true) //Keep a mean of the last readings for the headPosition and gazeDirection
                {
                    recentRegisteredGazeDirection.AddFirst(direction);

                    if (recentRegisteredGazeDirection.Count > meanPositionCount)
                    {
                        recentRegisteredGazeDirection.RemoveLast();
                    }
                }
                //headPosition = meanCalculatedDistance(recentRegisteredHeadPosition);
                direction = meanCalculatedDistance(recentRegisteredGazeDirection);
                //previousHeadPosition = headPosition;
                previousDirection = direction;

                //METHOD 1
                if (selectionMethod == SelectionMethod.OverlapSphere)

                {
                    float clampedvalue = RemapClamped(calculatedDistance, 0.3f, 1f, 0.1f, 0.3f); //TESTING THIS METHOD
                    //float overlapScaling = 1.1f;
                    //if (calculatedDistance > 0.9f) { overlapScaling = 1.4f;  } else if (calculatedDistance > 0.6f) { overlapScaling = 1.25f; }
                    //(sphereOverlapRadius + (clampedvalue * calculatedDistance))
                    Vector3 startPosition = positionCurrent + direction * sphereOverlapRadius;
                    //+ direction * (0.1f + (clampedvalue * calculatedDistance))
                    Collider[] colliders = Physics.OverlapSphere(startPosition, sphereOverlapRadius/2);
                    //colliders = Physics.OverlapCapsule(positionCurrent, startPosition, sphereOverlapRadius);
                    MeshRenderer closestTarget = closestTargetToVergencePoint3D(colliders, startPosition);
                    //sphere used to comfirm the detection method is working properly
                    debugSphere.position = startPosition;
                    debugSphere.localScale = Vector3.one * sphereOverlapRadius;

                    if (currentEyeState == EyeState.Open && !blinkedOnce && keepTracking)   
                    {
                        if (closestTarget != null)
                        {
                            noTargetTime = 0;
                            dwellLeeway = 0;
                            Material material = closestTarget.material;
                            Transform transform = closestTarget.transform;

                            if (!changedTransforms.Contains(transform))
                            {
                                ToggleOutline(transform);
                                changedTransforms.Add(transform);
                            }
                            currentHitTransforms.Add(transform);

                            //update the current target object
                            if (lastSelectedObject != closestTarget)
                            {
                                lastSelectedObject = closestTarget;
                                //blinkingTime = 0;
                                blinkCount = 0;
                                blinkedOnce = false;
                                dwellTime = 0;
                                changeTransitionState(TransitionState.Idle);
                            }

                            Debug.Log("METHOD 3 - Closest object: " + closestTarget.gameObject);
                        }
                        else if (closestTarget == null && lastSelectedObject != null)
                        {
                            noTargetTime += Time.deltaTime;
                            dwellLeeway += Time.deltaTime;
                        }
                    }
                }


                //METHOD 2
                if (selectionMethod == SelectionMethod.SphereCast)
                {
                    float halfDistance = getErrorCalculatedDistance(calculatedDistance, true);
                    Vector3 startPosition = positionCurrent - direction * (halfDistance - 0.0697f);
                    RaycastHit[] hits = Physics.SphereCastAll(startPosition, 0.01f, direction, halfDistance * 2.0f + 0.0697f);
                    MeshRenderer closestTarget = closestTargetToVergencePoint(hits, headPosition, calculatedDistance);

                    if (currentEyeState == EyeState.Open && !blinkedOnce && keepTracking)
                    {
                        if (closestTarget != null)
                        {
                            noTargetTime = 0;
                            dwellLeeway = 0;
                            Material material = closestTarget.material;
                            Transform transform = closestTarget.transform;

                            if (!changedTransforms.Contains(transform))
                            {
                                ToggleOutline(transform);
                                changedTransforms.Add(transform);
                            }
                            currentHitTransforms.Add(transform);

                            //update the current target object
                            if (lastSelectedObject != closestTarget)
                            {
                                lastSelectedObject = closestTarget;
                                //blinkingTime = 0;
                                blinkCount = 0;
                                blinkedOnce = false;
                                dwellTime = 0;
                                changeTransitionState(TransitionState.Idle);
                            }

                            Debug.Log("METHOD 3 - Closest object: " + closestTarget.gameObject);
                        }
                        else if (closestTarget == null && lastSelectedObject != null)
                        {
                            noTargetTime += Time.deltaTime;
                            dwellLeeway += Time.deltaTime;
                        }
                    }
                }


                //METHOD 3
                if (selectionMethod == SelectionMethod.RayCast)
                {
                    //headPosition = transform.rotation * headPosition + transform.position;
                    Ray ray = new Ray(headPosition, direction);
                    RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance);

                    //debugGazeRay.SetPosition(0, headPosition);
                    //debugGazeRay.SetPosition(1, headPosition + direction.normalized * maxDistance);

                    MeshRenderer closestTarget = closestTargetToVergencePoint(hits, headPosition, calculatedDistance);

                    if (currentEyeState == EyeState.Open && !blinkedOnce && keepTracking)
                    {
                        if (closestTarget != null)
                        {
                            noTargetTime = 0;
                            dwellLeeway = 0;
                            Material material = closestTarget.material;
                            Transform transform = closestTarget.transform;

                            if (!changedTransforms.Contains(transform))
                            {
                                ToggleOutline(transform);
                                changedTransforms.Add(transform);
                            }
                            currentHitTransforms.Add(transform);

                            //update the current target object
                            if (lastSelectedObject != closestTarget)
                            {
                                lastSelectedObject = closestTarget;
                                //blinkingTime = 0;
                                blinkCount = 0;
                                blinkedOnce = false;
                                dwellTime = 0;
                                changeTransitionState(TransitionState.Idle);
                            }

                            Debug.Log("METHOD 3 - Closest object: " + closestTarget.gameObject);
                        }
                        else if (closestTarget == null && lastSelectedObject != null)
                        {
                            noTargetTime += Time.deltaTime;
                            dwellLeeway += Time.deltaTime;
                        }
                    }
                }


                //METHOD 4
                if (selectionMethod == SelectionMethod.ConeCast)
                {
                    // Depending on method different eye behaviour will be tracked
                    if (useWink) { updateWink(measureEye.leftEyeOpenness, measureEye.rightEyeOpenness); }
                    else { updateDoubleBlink(measureEye.leftEyeOpenness, measureEye.rightEyeOpenness); }
                    
                    if (!coneCastLocked)
                    {
                        confirmationMethod = ConfirmationMethod.CircularPlane;
                        circularPlaneIsGone = false;


                        // LOCK SELECTED OBJECTS
                        // WINK
                        if (winkingState == WinkingEye.Winked && coneCastHitsExist && useWink)
                        {
                            changeTransitionState(TransitionState.Idle);
                            changeWinkingState(WinkingEye.None);
                            noTargetTime = 0;
                            blinkCountTest += 1;
                            debugSound.Play();

                            // no need for second step to confirm target
                            if (coneHits.Count == 1)
                            {
                                MeshRenderer lastSelectedObject = coneHits[0].GetComponent<MeshRenderer>();
                                //lastSelectedObject.material.SetColor("_Color", confirmed);
                                //lastSelectedTarget = lastSelectedObject;
                                //changedMaterials.Add(lastSelectedObject.material);
                                targetSelectionHandler(lastSelectedObject);

                                Debug.Log("Selected Target");
                            }
                            else
                            {
                                debugCone.GetComponent<MeshRenderer>().enabled = false;
                                circularPlane.GetComponent<MeshCollider>().enabled = true;
                                circularPlane.GetComponent<MeshRenderer>().enabled = true;
                                updateCircularPlane(lockedHeadPosition, lockedDirection);
                                coneCastLocked = true;
                                placeCloneObjects(lockedHeadPosition, lockedDirection);
                            }
                        }
                        // DOUBLE BLINK
                        if (currentTransitionState == TransitionState.Opening && coneCastHitsExist && !useWink)
                        {
                            if (blinkCount == 0)
                            {
                                lockedHeadPosition2 = Camera.main.transform.position;
                                lockedDirection2 = Camera.main.transform.forward;
                                blinkedOnce = true;
                            }
                            blinkCount += 1;
                            changeTransitionState(TransitionState.Idle);
                            noTargetTime = 0;
                            blinkCountTest += 1;
                            debugSound.Play();

                            // Target Confirmed
                            if (blinkCount == 2)
                            {
                                blinkCount = 0;
                                blinkedOnce = false;
                                betweenBlinkingTime = 0;
                                changeTransitionState(TransitionState.Idle);
                                changeWinkingState(WinkingEye.None);
                                noTargetTime = 0;
                                blinkCountTest += 1;
                                debugSound.Play();

                                // no need for second step to confirm target
                                if (coneHits.Count == 1)
                                {
                                    MeshRenderer lastSelectedObject = coneHits[0].GetComponent<MeshRenderer>();
                                    //lastSelectedObject.material.SetColor("_Color", confirmed);
                                    //lastSelectedTarget = lastSelectedObject;
                                    //changedMaterials.Add(lastSelectedObject.material);
                                    targetSelectionHandler(lastSelectedObject);

                                    Debug.Log("Selected Target");
                                }
                                else
                                {
                                    debugCone.GetComponent<MeshRenderer>().enabled = false;
                                    circularPlane.GetComponent<MeshCollider>().enabled = true;
                                    circularPlane.GetComponent<MeshRenderer>().enabled = true;
                                    updateCircularPlane(lockedHeadPosition, lockedDirection);
                                    coneCastLocked = true;
                                    placeCloneObjects(lockedHeadPosition, lockedDirection);
                                }
                            }
                        }


                        //only when wink hasn't started yet
                        if (currentEyeState == EyeState.Open && !blinkedOnce && keepTracking)
                        {
                            //lock head position and gaze direction of when user decides to wink
                            lockedHeadPosition = headPosition;
                            lockedDirection = direction;
                            lockedHeadPosition2 = Camera.main.transform.position;
                            lockedDirection2 = Camera.main.transform.forward;


                            float halfDistance = getErrorCalculatedDistance(calculatedDistance, true);
                            float fullDistance = (calculatedDistance + halfDistance) + 0.05f;
                            float coneRadius = getSphereSize(fullDistance);
                            RaycastHit[] hits = Physics.SphereCastAll(headPosition, coneRadius, direction, fullDistance);
                            List<GameObject> test = detectObjects(headPosition, direction, hits);

                            if (test.Count != 0)
                            {
                                coneCastHitsExist = true;
                                coneHits = test;
                                noTargetTime = 0f;
                            }
                            else
                            {
                                noTargetTime += Time.deltaTime;
                                if (noTargetTime > 1f)
                                {
                                    coneCastHitsExist = false;
                                    coneHits.Clear();
                                    noTargetTime -= 1f;
                                }
                            }
                            //DEBUG CONE
                            debugCone.GetComponent<MeshRenderer>().enabled = true;
                            debugCone.transform.position = headPosition;
                            debugCone.transform.rotation = Quaternion.LookRotation(direction);
                            debugCone.transform.localScale = new Vector3(coneRadius, coneRadius, fullDistance);

                            // update currentHitMaterials and changedMaterials lists
                            foreach (GameObject coneHit in coneHits)
                            {
                                MeshRenderer meshRenderer = coneHit.GetComponent<MeshRenderer>();
                                if (meshRenderer != null)
                                {
                                    Transform transform = coneHit.transform;

                                    if (!changedTransforms.Contains(transform))
                                    {
                                        ToggleOutline(transform);
                                        changedTransforms.Add(transform);
                                    }
                                    currentHitTransforms.Add(transform);
                                }
                                Debug.Log("METHOD 4 - Hit object inside cone: " + coneHit);
                            }
                        }
                    }
                }


                // REVERT OBJECT COLORS
                if (!coneCastLocked)
                {
                    resetObjectColors(currentHitMaterials);
                    resetObjectColors2(currentHitTransforms);
                }
                
                #endregion

                #region EyeFrameDta
                //Eye Measures per frame
                Debug.Log(measureEye.frameNumber);
                Debug.Log(measureEye.captureTime);
                Debug.Log(measureEye.interPupillaryDistanceInMM); //Estimate of user�s inter-pupillary distance 
                Debug.Log(measureEye.leftPupilIrisDiameterRatio); //Ratio of user�s left pupil diameter estimate to estimated iris diameter
                Debug.Log(measureEye.rightPupilIrisDiameterRatio);
                Debug.Log(measureEye.leftPupilDiameterInMM); //Estimate of pupil diameter for the left eye in millimeters
                Debug.Log(measureEye.rightPupilDiameterInMM);
                Debug.Log(measureEye.leftIrisDiameterInMM); //Estimated diameter of left iris in millimeters
                Debug.Log(measureEye.rightIrisDiameterInMM);
                Debug.Log(measureEye.leftEyeOpenness); //Estimated openness ratio of the left eye (0-1)
                Debug.Log(measureEye.rightEyeOpenness);

                gazeFrameData.Add(measureEye.frameNumber.ToString());
                gazeFrameData.Add(measureEye.captureTime.ToString());
                gazeFrameData.Add(measureEye.interPupillaryDistanceInMM.ToString());
                gazeFrameData.Add(measureEye.leftPupilIrisDiameterRatio.ToString());
                gazeFrameData.Add(measureEye.rightPupilIrisDiameterRatio.ToString());
                gazeFrameData.Add(measureEye.leftPupilDiameterInMM.ToString());
                gazeFrameData.Add(measureEye.rightPupilDiameterInMM.ToString());
                gazeFrameData.Add(measureEye.leftIrisDiameterInMM.ToString());
                gazeFrameData.Add(measureEye.rightIrisDiameterInMM.ToString());
                gazeFrameData.Add(measureEye.leftEyeOpenness.ToString());
                gazeFrameData.Add(measureEye.rightEyeOpenness.ToString());
                #endregion

                #region TargetConfirmed
                // Clear old target so an accidental confirmation doesn't select a non intended target
                if (noTargetTime >= timeToForgetTarget * 2f)
                {
                    noTargetTime -= timeToForgetTarget * 2f;
                    lastSelectedObject = null;
                    //blinkingTime = 0;
                    blinkCount = 0;
                    betweenBlinkingTime = 0;
                    dwellTime = 0;
                    blinkedOnce = false;
                    changeTransitionState(TransitionState.Idle);
                }

                //METHOD1
                if (confirmationMethod == ConfirmationMethod.DoubleBlinking)
                {
                    updateDoubleBlink(measureEye.leftEyeOpenness, measureEye.rightEyeOpenness);

                    // Blinking Handler
                    if (lastSelectedObject != null)
                    {
                        // Blink is confirmed
                        if (currentTransitionState == TransitionState.Opening)
                        {
                            if (blinkCount == 0)
                            {
                                blinkedOnce = true;
                            }
                            blinkCount += 1;
                            changeTransitionState(TransitionState.Idle);
                            noTargetTime = 0;
                            //debug
                            blinkCountTest += 1;
                            debugSound.Play();

                            if (blinkCount == 2)
                            {
                                doubleBlinked++;
                                //Material material = currentTarget.GetComponent<MeshRenderer>().material;
                                //int num = pilotTestController.getCurrentTarget();
                                
                                //if (num != 10)
                                //{
                                //    resetObjectColorsAux(material);
                                //    if (selectedObjects.Contains(lastSelectedObject.transform)) { pilotTestController.writeSelectedObject(lastSelectedObject); }
                                //    nextTarget();
                                //}
                                //else if (num == 10) // switch confirmation technique
                                //{
                                //    resetObjectColorsAux(material);
                                //    if (selectedObjects.Contains(lastSelectedObject.transform)) { pilotTestController.writeSelectedObject(lastSelectedObject); }
                                //    nextTarget(true);
                                //}
                                //if (lastSelectedObject.transform == currentTarget) // check if target is hit
                                //{ 
                                //    if (num != 10)
                                //    {
                                //        nextTarget();
                                //    }
                                //    else if (num == 10) // switch confirmation technique
                                //    {
                                //        nextTarget(true);
                                //    }
                                //}
                                //lastSelectedObject.material.SetColor("_Color", confirmed);
                                //lastSelectedTarget = lastSelectedObject;
                                //isRatingDone(lastSelectedObject);
                                blinkCount = 0;
                                blinkedOnce = false;
                                betweenBlinkingTime = 0;

                                targetSelectionHandler(lastSelectedObject);
                                Debug.Log("Selected Target");
                            }
                        }
                    }
                }

                //METHOD2
                if (confirmationMethod == ConfirmationMethod.Dwell)
                {
                    keepTracking = true;
                    if (dwellLeeway >= timeToForgetTarget)
                    {
                        dwellLeeway -= timeToForgetTarget;
                        dwellTime = 0;
                    }

                    if (lastSelectedObject != null)
                    {
                        dwellTime += Time.deltaTime;

                        if (dwellTime >= 1.5f)
                        {
                            Material material = currentTarget.GetComponent<MeshRenderer>().material;
                            int num = pilotTestController.getCurrentTarget();
                            if (lastSelectedObject != lastSelectedTarget) // This makes sure I don't count multiple attempts when 
                                                                          // selecting the same target 
                            {
                                if (num != 10)
                                {
                                    //changeConfirmation(true);
                                    resetObjectColorsAux(material);
                                    if (selectedObjects.Contains(lastSelectedObject.transform)) { pilotTestController.writeSelectedObject(lastSelectedObject); }
                                    nextTarget();
                                }
                                else if (num == 10) // switch confirmation technique
                                {
                                    confirmationMethod = ConfirmationMethod.Winking;
                                    resetObjectColorsAux(material);
                                    if (selectedObjects.Contains(lastSelectedObject.transform)) { pilotTestController.writeSelectedObject(lastSelectedObject); }
                                    nextTarget(true);
                                }
                            }
                            lastSelectedObject.material.SetColor("_Color", confirmed);
                            lastSelectedTarget = lastSelectedObject;
                            isRatingDone(lastSelectedObject);
                            dwellTime -= 1.5f;

                            Debug.Log("Selected Target");
                        }
                    }
                }

                //METHOD3
                if (confirmationMethod == ConfirmationMethod.Winking)
                {
                    updateWink(measureEye.leftEyeOpenness, measureEye.rightEyeOpenness);

                    // Blinking Handler
                    if (lastSelectedObject != null)
                    {
                        // Blink is confirmed
                        if (winkingState == WinkingEye.Winked)
                        {
                            changeTransitionState(TransitionState.Idle);
                            changeWinkingState(WinkingEye.None);
                            noTargetTime = 0;
                            //debug
                            blinkCountTest += 1;
                            debugSound.Play();

                            targetSelectionHandler(lastSelectedObject);
                        }
                    }
                }

                //METHOD4
                if (confirmationMethod == ConfirmationMethod.CircularPlane)
                {
                    if (coneCastLocked)
                    {
                        MeshRenderer closestTarget = null;
                        if (currentEyeState == EyeState.Open)
                        {
                            int cloneLayer = LayerMask.NameToLayer("CloneLayer");
                            int cloneLayerMask = 1 << cloneLayer;
                            Ray ray = new Ray(headPosition, direction);
                            RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance, cloneLayerMask);

                            closestTarget = closestTargetToVergencePoint(hits, headPosition, calculatedDistance);

                            //debugGazeRay.SetPosition(0, headPosition);
                            //debugGazeRay.SetPosition(1, headPosition + direction.normalized * maxDistance);
                            if (!blinkedOnce && keepTracking)
                            {
                                lookingAtPlane = isLookingAtCirculartPlane(headPosition, direction);
                                if (closestTarget != null)
                                {
                                    noTargetTime = 0;
                                    Material material = closestTarget.material;
                                    MeshRenderer ogMesh = closestTarget.gameObject.GetComponent<CloneReference>()
                                        .originalObject.GetComponent<MeshRenderer>();
                                    Material ogMaterial = ogMesh.material;
                                    //Transform transform = closestTarget.transform;
                                    Transform ogTransform = ogMesh.transform;


                                    if (!changedMaterials.Contains(material))
                                    {
                                        // Setup line that connects both the selected clone and its original
                                        if (ogCloneLink == null)
                                        {
                                            ogCloneLink = Instantiate(debugGazeRay);
                                            ogCloneLink.AddComponent<OgCloneLinkReferences>();
                                            ogCloneLink.startWidth = 0.005f;
                                        }
                                        var link = ogCloneLink.GetComponent<OgCloneLinkReferences>();

                                        
                                        //ToggleOutline(transform);
                                        ToggleOutline(ogTransform);

                                        ogCloneLink.SetPosition(0, ogMesh.transform.position);
                                        link.startPoint = ogMaterial;
                                        ogCloneLink.SetPosition(1, closestTarget.transform.position);
                                        link.endPoint = closestTarget.gameObject;

                                        if (material.color != confirmed && material.color != target)
                                        {
                                            material.SetColor("_Color", onClone);
                                            ogMaterial.SetColor("_Color", onClone);
                                            changedMaterials.Add(material);
                                            changedMaterials.Add(ogMaterial);
                                        }
                                        //changedTransforms.Add(transform);
                                        changedTransforms.Add(ogTransform);

                                    }
                                    currentHitMaterials.Add(material);
                                    currentHitMaterials.Add(ogMaterial);
                                    //currentHitTransforms.Add(transform);
                                    currentHitTransforms.Add(ogTransform);

                                    //update the current target object
                                    if (lastSelectedObject != closestTarget)
                                    {
                                        blinkCount = 0;
                                        lastSelectedObject = closestTarget;
                                        changeTransitionState(TransitionState.Idle);
                                    }
                                    resetObjectColors(currentHitMaterials);
                                    resetObjectColors2(currentHitTransforms);

                                    Debug.Log("METHOD 3 - Closest object: " + closestTarget.gameObject);
                                }
                                else if (closestTarget == null)
                                {
                                    //var link = ogCloneLink.GetComponent<OgCloneLinkReferences>();
                                    //link.endPoint.GetComponent<MeshRenderer>().material.SetColor("_Color", offClone);
                                    if (lastSelectedObject != null)
                                    {
                                        noTargetTime += Time.deltaTime;
                                        if (noTargetTime > 1f)
                                        {
                                            noTargetTime -= 1f;
                                            blinkCount = 0;
                                            lastSelectedObject = null;
                                            betweenBlinkingTime = 0;
                                            //blinkedOnce = false;
                                            changeTransitionState(TransitionState.Idle);
                                        }
                                    }
                                }
                            }
                        }

                        // Wink clone confirmation
                        if (winkingState == WinkingEye.Winked && useWink)
                        {
                            circularPlaneIsGone = true;
                            // scenario where user wants to exit current selection
                            if (lastSelectedObject == null && !lookingAtPlane)
                            {
                                lastSelectedTarget.material.SetColor("_Color", confirmed);
                                changedMaterials.Add(lastSelectedTarget.material);
                                exitCircularPlane();
                            }
                            // scenario where user misses target or blinks by accident
                            else if (lastSelectedObject == null && lookingAtPlane)
                            {
                                continue;
                            }
                            // scenario where user confirms target
                            else
                            {
                                if (lastSelectedObject != null)
                                {
                                    CloneReference refComp = lastSelectedObject.gameObject.GetComponent<CloneReference>();
                                    MeshRenderer refOriginal = refComp.originalObject.GetComponent<MeshRenderer>();
                                    refOriginal.material.SetColor("_Color", confirmed);
                                    changedMaterials.Add(refOriginal.material);
                                    lastSelectedTarget = refOriginal;
                                }
                                else // no new target is selected, so old selected target goes back to be confirmed
                                {
                                    lastSelectedTarget.material.SetColor("_Color", confirmed);
                                    changedMaterials.Add(lastSelectedTarget.material);
                                }
                                lastSelectedObject = lastSelectedTarget;
                                exitCircularPlane();
                                targetSelectionHandler(lastSelectedObject, true);
                            }
                        }
                        // Double Blink clone confirmation
                        if (currentTransitionState == TransitionState.Opening && !useWink)
                        {
                            if (blinkCount == 0)
                            {
                                blinkedOnce = true;
                            }
                            blinkCount += 1;
                            changeTransitionState(TransitionState.Idle);
                            noTargetTime = 0;
                            blinkCountTest += 1;
                            debugSound.Play();

                            // Target Confirmed
                            if (blinkCount == 2)
                            {
                                //clearDebugRays();
                                blinkCount = 0;
                                blinkedOnce = false;
                                betweenBlinkingTime = 0;

                                circularPlaneIsGone = true;
                                // scenario where user wants to exit current selection
                                if (lastSelectedObject == null && !lookingAtPlane)
                                {
                                    exitCircularPlane();
                                    lastSelectedTarget.material.SetColor("_Color", confirmed);
                                    changedMaterials.Add(lastSelectedTarget.material);
                                }
                                // scenario where user misses target or blinks by accident
                                else if (lastSelectedObject == null && lookingAtPlane)
                                {
                                    continue;
                                }
                                // scenario where user confirms target
                                else
                                {
                                    if (lastSelectedObject != null)
                                    {
                                        CloneReference refComp = lastSelectedObject.gameObject.GetComponent<CloneReference>();
                                        MeshRenderer refOriginal = refComp.originalObject.GetComponent<MeshRenderer>();
                                        refOriginal.material.SetColor("_Color", confirmed);
                                        changedMaterials.Add(refOriginal.material);
                                        lastSelectedTarget = refOriginal;
                                    }
                                    else // no new target is selected, so old selected target goes back to be confirmed
                                    {
                                        lastSelectedTarget.material.SetColor("_Color", confirmed);
                                        changedMaterials.Add(lastSelectedTarget.material);
                                    }
                                    lastSelectedObject = lastSelectedTarget;
                                    exitCircularPlane();
                                    targetSelectionHandler(lastSelectedObject, true);
                                }
                            }
                        }
                    }
                }
                #endregion
            }
        }
    }


    // Detect all objects in the scene
    private void cacheObjects()
    {
        foreach (Transform angleFolder in objectsOffset)
        {
            foreach (Transform distanceFolder in angleFolder)
            {
                foreach (Transform sphereTransform in distanceFolder)
                {
                    selectedObjects.Add(sphereTransform);
                    string materialName = sphereTransform.GetComponent<MeshRenderer>().material.name.Replace(" (Instance)", "");
                    if (materialName == "MySphereMaterial")
                    {
                        sphereTransform.GetComponent<MeshRenderer>().material.SetColor("_Color", off);
                    }
                }
            }
        }


        //foreach (Transform transformer in selectedObjects)
        //{
        //    Debug.Log($"BANANANANA Sphere transform {transformer.position} exists.");
        //}
    }
    public List<Transform> getSelectedObjects() { return selectedObjects; }
    // Get next Pilot/Final Test Target
    public void nextTarget(bool changeTechnique = false)
    {
        //currentTarget = pilotTestController.getnewTarget(changeTechnique);
        currentTarget = finalTestController.getnewTarget(changeTechnique);
        if(!finalTestController.getTutorialOn() && testStarted) { currentTarget.GetComponent<MeshRenderer>().material.SetColor("_Color", target); }
    }
    // Rating has been done or tutorial
    private void isRatingDone(MeshRenderer rating)
    {
        string materialName = rating.material.name.Replace(" (Instance)", "");
        Debug.Log("BlaBlaBla2");
        Debug.Log($"MaterialName: {materialName}");

        if (rating.name == "start tutorial")
        {
            Debug.Log("BlaBlaBla3");
            finalTestController.tutorialHandler();
        }
        else if (rating.name == "start")
        {
            Debug.Log("BlaBlaBla4");
            //pilotTestController.endTutorial();
            rating.material.SetColor("_Color", off);
            finalTestController.exitShortBreak();
            tries = 0;
        }
        else if (materialName == "ratingSphere")
        {
            rating.material.SetColor("_Color", off);
            //pilotTestController.finishedRating(Int32.Parse(rating.name));
            finalTestController.finishedRating(Int32.Parse(rating.name));
        }
    }
    // Handles the selected targets confirmation
    private void targetSelectionHandler(MeshRenderer lastSelected, bool coneCast = false)
    {
        if (!coneCast)
        {
            lastSelected.material.SetColor("_Color", confirmed);
            lastSelectedTarget = lastSelected;
        }
        if (finalTestController.getTutorialOn()) {
            isRatingDone(lastSelected);
            currentTarget = finalTestController.getnewTarget();
            counter++;
            return; 
        }

        tries++;
        if (tries <= 3)
        {
            Material material = currentTarget.GetComponent<MeshRenderer>().material;
            //int num = pilotTestController.getCurrentTarget();
            int num = finalTestController.getCurrentTarget();

            if (lastSelected.transform == currentTarget || tries == 3)
            {
                if (num != 10)
                {
                    resetObjectColorsAux(material);
                    //Avoid rating and start spheres to be registered
                    //if (selectedObjects.Contains(lastSelected.transform)) { finalTestController.writeSelectedObject(lastSelected,tries); }
                    float distance = (float)Math.Round(Vector3.Distance(lastSelected.transform.position, currentTarget.position), 2);
                    finalTestController.writeSelectedObject2(lastSelected, tries, distance);
                    nextTarget();
                }
                else if (num == 10) // switch confirmation technique
                {
                    resetObjectColorsAux(material);
                    //if (selectedObjects.Contains(lastSelected.transform)) { finalTestController.writeSelectedObject(lastSelected, tries); }
                    float distance = (float)Math.Round(Vector3.Distance(lastSelected.transform.position, currentTarget.position), 2);
                    finalTestController.writeSelectedObject2(lastSelected, tries, distance);
                    nextTarget(true);
                }
                tries = 0;
                Debug.Log("Selected Target");
            }
        }
        isRatingDone(lastSelected);
        if (!selectedObjects.Contains(lastSelectedObject.transform)) { tries = 0; } // make sure the number of tries doesn't reach
                                                                                    // 3 by accident while not selecting targets
    }
    // Algoritm used to determine the calculated Point using Vergence
    private void calculateDistance(Vector3 P1, Vector3 P3, Vector3 R2, Vector3 R4, out float gazeDepth, out Vector3 Pm, out Vector3 eyeMidPoint)
    {
        Vector3 P13 = P1 - P3;

        float r2dotr2 = Vector3.Dot(R2, R2);
        float r4dotr4 = Vector3.Dot(R4, R4);
        float r2dotr4 = Vector3.Dot(R2, R4);

        //check denominator
        float denom = (Mathf.Pow(r2dotr4, 2) - (r2dotr2 * r4dotr4));

        if (r2dotr4 < Mathf.Epsilon || Mathf.Abs(denom) < Mathf.Epsilon)
        {
            gazeDepth = -1.0f;
            Pm = new Vector3(-1, -1, -1);
            eyeMidPoint = new Vector3(-1, -1, -1);
            return;
        }

        //calculate Vector Vector intersection
        //if (t1 || t2 < 0) then the intersection point is behind the gaze rays origins
        float t2 = ((Vector3.Dot(P13, R2) * r4dotr4) - (Vector3.Dot(P13, R4) * r2dotr4)) / denom;
        float t1 = (Vector3.Dot(P13, R2) + t2 * r2dotr2) / r2dotr4;

        Vector3 Pa = P1 + t1 * R2;
        Vector3 Pb = P3 + t2 * R4;
        Pm = (Pa + Pb) / 2;

        eyeMidPoint = (P1 + P3) / 2;
        gazeDepth = Vector3.Distance(eyeMidPoint, Pm);
    }
    // Calculate the mean of the last n positions
    private float meanCalculatedDistance(LinkedList<float> data)
    {
        float total = 0f;

        // Handle the case where the list is empty
        if (data.Count == 0)
        {
            return 0f;
        }
        
        foreach (float distance in data)
        {
            total += distance;
        }

        return total / data.Count;
    }
    // Calculate the mean of the last n positions for vector3 
    private Vector3 meanCalculatedDistance(LinkedList<Vector3> data)
    {
        Vector3 total = Vector3.zero;

        // Handle the case where the list is empty
        if (data.Count == 0)
        {
            return total;
        }

        foreach (Vector3 vector in data)
        {
            total += vector;
        }

        return total / data.Count;
    }
    // Check which objects should revert to default color
    private void resetObjectColors(HashSet<Material> currentHitMaterials)
    {
        //REVERT OBJECTS COLOR
        List<Material> materialsToRevert = new List<Material>();

        // Check which objects are not being hit anymore
        foreach (Material material in changedMaterials)
        {
            if (!currentHitMaterials.Contains(material))
            {
                if(material != circularPlane.GetComponent<MeshRenderer>().material)
                {
                    materialsToRevert.Add(material);
                }
            }
        }

        // Revert color of materials that are no longer being hit or confirmed
        foreach (Material material in materialsToRevert)
        {
            resetObjectColorsAux(material);
        }

        foreach (Transform selectedObject in selectedObjects)
        {
            MeshRenderer meshRenderer = selectedObject.GetComponent<MeshRenderer>();
            Material material = meshRenderer.material;
            string materialName = material.name.Replace(" (Instance)", "");
            if (meshRenderer.material.color == confirmed && meshRenderer != lastSelectedTarget)
            {
                if (materialName == "MySphereMaterial")
                {
                    material.SetColor("_Color", off);
                }
                else if (materialName == "MySphereMaterial 1")
                {
                    material.SetColor("_Color", set1);
                }
                else if (materialName == "MySphereMaterial 2")
                {
                    material.SetColor("_Color", set2);
                }
                else if (materialName == "MySphereMaterial 3")
                {
                    material.SetColor("_Color", set3);
                }
                else if (materialName == "MySphereMaterial 4")
                {
                    material.SetColor("_Color", set4);
                }
                else if (materialName == "MySphereMaterial 5")
                {
                    material.SetColor("_Color", set5);
                }
                changedMaterials.Remove(meshRenderer.material);
            }
        }
    }
    private void resetObjectColorsAux(Material material)
    {
        string materialName = material.name.Replace(" (Instance)", "");
        if (material.color != confirmed)
        {
            if (material.color == onClone)
            {
                material.SetColor("_Color", offClone);
            }
            else // revert color depending on atom 
            {
                if (materialName == "MySphereMaterial")
                {
                    material.SetColor("_Color", off);
                }
                else if (materialName == "MySphereMaterial 1")
                {
                    material.SetColor("_Color", set1);
                }
                else if (materialName == "MySphereMaterial 2")
                {
                    material.SetColor("_Color", set2);
                }
                else if (materialName == "MySphereMaterial 3")
                {
                    material.SetColor("_Color", set3);
                }
                else if (materialName == "MySphereMaterial 4")
                {
                    material.SetColor("_Color", set4);
                }
                else if (materialName == "MySphereMaterial 5")
                {
                    material.SetColor("_Color", set5);
                }

            }
            changedMaterials.Remove(material);
        }
    }
    // Check which objects should revert to not having an outline
    private void resetObjectColors2(HashSet<Transform> currentHitTransform)
    {
        //REVERT OBJECTS COLOR
        List<Transform> materialsToRevert = new();

        // Check which objects are not being hit anymore
        foreach (Transform transform in changedTransforms)
        {
            if (!currentHitTransform.Contains(transform))
            {
                materialsToRevert.Add(transform);
            }
        }

        // Revert color of materials that are no longer being hit or confirmed
        foreach (Transform transform in materialsToRevert)
        {
            ToggleOutline(transform, false);
            changedTransforms.Remove(transform);
        }
    }
    // Create outline for objects
    private void CreateOutline(Transform target)
    {
        GameObject original = target.gameObject;

        // Create an outline GameObject
        GameObject outline = new GameObject("Outline_" + original.name);
        outline.transform.SetParent(original.transform, false);
        outline.transform.localPosition = Vector3.zero;
        outline.transform.localRotation = Quaternion.identity;
        outline.transform.localScale = Vector3.one * outlineScaleFactor;

        // Copy the mesh
        MeshFilter originalMeshFilter = original.GetComponent<MeshFilter>();
        MeshRenderer originalRenderer = original.GetComponent<MeshRenderer>();

        if (originalMeshFilter == null || originalRenderer == null)
        {
            Debug.LogWarning("Missing MeshFilter or MeshRenderer on: " + original.name);
            return;
        }

        MeshFilter mf = outline.AddComponent<MeshFilter>();
        mf.sharedMesh = originalMeshFilter.sharedMesh;

        MeshRenderer mr = outline.AddComponent<MeshRenderer>();
        mr.material = outlineMaterial;

        // Optional: Disable shadow casting/receiving
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }
    // Create outline for objects and toggle them
    private void ToggleOutline(Transform original, bool enable = true)
    {
        if (original.childCount == 0)
        {
            GameObject outlineGO = new GameObject();
            outlineGO.transform.SetParent(original);

            Debug.Log($"Number of children inside original ({original.gameObject.name}) is {original.childCount}");

            var outlineMF = outlineGO.AddComponent<MeshFilter>();
            var outlineMR = outlineGO.AddComponent<MeshRenderer>();

            var originalMF = original.GetComponent<MeshFilter>();
            var originalMR = original.GetComponent<MeshRenderer>();

            Mesh outlineMesh = new Mesh();

            outlineMesh.SetVertices(originalMF.sharedMesh.vertices);
            int[] triangles = originalMF.sharedMesh.triangles;
            Array.Reverse(triangles);
            outlineMesh.SetTriangles(triangles, 0);
            Vector3[] normals = originalMF.sharedMesh.normals;
            for (int i = 0; i < normals.Length; i++)
                normals[i] *= -1.0f;
            outlineMesh.SetNormals(normals);

            outlineMF.sharedMesh = outlineMesh;
            outlineMR.material = outlineMaterial2;

            outlineGO.transform.localPosition = Vector3.zero;
            outlineGO.transform.localScale = Vector3.one * outlineScaleFactor;
        }

        Transform outline = original.GetChild(0);
        outline.gameObject.SetActive(enable);
        if (coneCastLocked) { outline.GetComponent<MeshRenderer>().material = outlineMaterialClones; }
        else { outline.GetComponent<MeshRenderer>().material = outlineMaterial2; }

    }



    // These 3 next function work together to determine the error for detection sizes
    public float getSphereSize(float distance)
        {
            float error = getErrorCalculatedDistance(distance, false);
            return distance * Mathf.Tan(foveaAngleRadians + error);
        }
    // Determine the error for depth or sphere radius
    private float getErrorCalculatedDistance(float distance, bool isDepth)
    {
        //Calculate Depth error
        if (isDepth)
        {
            if (depthErrors.ContainsKey(distance))
            {
                return depthErrors[distance];
            }
            
            return interpolateError(distance, depthErrors);    
        }
        
        // If the exact error exists there's no need for interpolation
        if (horizontalErrors.ContainsKey(distance) && verticalErrors.ContainsKey(distance))

        {
            return Mathf.Max(horizontalErrors[distance], verticalErrors[distance]) * Mathf.Deg2Rad;
        }

        // Otherwise, interpolate both angles errors
        float horizontalError = interpolateError(distance, horizontalErrors);
        float verticalError = interpolateError(distance, verticalErrors);

        return Mathf.Max(horizontalError, verticalError) * Mathf.Deg2Rad;
        
    }
    // Determine the error of the detection sphere's size depending on it's depth
    private float interpolateError(float distance, Dictionary<float, float> errorDict)
    {
        // Handle cases where distance is greater than the max key (for now this is how I handle longer distances)
        float maxKey = errorDict.Keys.Max();
        if (distance >= maxKey)
        {
            return errorDict[maxKey];
        }
        // Handle cases where distance is lesser than the min key (for now this is how I handle  smaller distances)
        float minKey = errorDict.Keys.Min();
        if (distance <= minKey)
        {
            return errorDict[minKey];
        }

        // Find nearest distances for interpolation
        float lowerKey = 0, upperKey = 0;
        foreach (var key in errorDict.Keys)
        {
            if (key <= distance) lowerKey = key;
            if (key >= distance) { upperKey = key; break; }
        }

        float lowerError = errorDict[lowerKey];
        float upperError = errorDict[upperKey];

        // Apply linear interpolation (free rule of 3)
        float deltaDistance = upperKey - lowerKey;
        float deltaError = upperError - lowerError;

        // Avoid division by zero when the errors are exactly the same
        if (deltaError == 0) return lowerError; 

        float t = (distance - lowerKey) / deltaDistance;
        return lowerError + t * deltaError;
    }
    // Clamp sphere size
    private float RemapClamped(float input, float inMin, float inMax, float outMin, float outMax)
    {
        float t = Mathf.InverseLerp(inMin, inMax, input);
        float tClamped = Mathf.Clamp01(t);
        return Mathf.Lerp(outMin, outMax, tClamped);
    }



    // Determine which of the detected objects is closer to the vergence point in depth
    private MeshRenderer closestTargetToVergencePoint(RaycastHit[] hits, Vector3 referencePoint, float referenceDistance)
    {
        MeshRenderer closestTarget = null;
        float closestHit = maxDistance;
        foreach (RaycastHit hit in hits)
        {
            GameObject hitGameObject = hit.collider.gameObject;
            MeshRenderer meshRenderer = hitGameObject.GetComponent<MeshRenderer>();

            if (meshRenderer != null)
            {
                float currentTargetDistance = Vector3.Distance(referencePoint, hitGameObject.transform.position);
                float distanceToReference = Mathf.Abs(referenceDistance - currentTargetDistance);

                if (distanceToReference < closestHit)
                {
                    closestTarget = meshRenderer;
                    closestHit = distanceToReference;
                }
            }

            Debug.Log("METHOD 3 - Hit object: " + hitGameObject);                          
        }
        return closestTarget;
    }
    // Determine which of the detected objects is closer to the vergence point in the 3D Space
    private MeshRenderer closestTargetToVergencePoint3D(Collider[] hits, Vector3 vergencePoint)
    {
        MeshRenderer closestTarget = null;
        float closestHit = maxDistance;
        foreach (Collider hit in hits)
        {
            GameObject hitGameObject = hit.gameObject;
            MeshRenderer meshRenderer = hitGameObject.GetComponent<MeshRenderer>();

            if (meshRenderer != null)
            {
                float distanceToVergencePoint = Vector3.Distance(vergencePoint, hitGameObject.transform.position);

                if (distanceToVergencePoint < closestHit)
                {
                    closestTarget = meshRenderer;
                    closestHit = distanceToVergencePoint;
                }
            }

            Debug.Log("METHOD 1 - Hit object: " + hitGameObject);
        }
        return closestTarget;
    }
    // Detect which objects are inside the conecast
    private List<GameObject> detectObjects(Vector3 playerPosition, Vector3 gazeDirection, RaycastHit[] hits)
    {
        List<GameObject> selectedSpheres = new();

        foreach (RaycastHit hit in hits)
        {
            Vector3 toObject = hit.transform.position - playerPosition;
            Vector3 directionToObject = toObject.normalized;

            float angle = Vector3.Angle(gazeDirection, directionToObject);
            if (angle <= coneAngle)
            {
                selectedSpheres.Add(hit.transform.gameObject);
            }
        }

        Debug.Log($"Selected {selectedSpheres.Count} spheres in view range.");
        return selectedSpheres;
    }
    // Places detected ConeCast objects on top of the DiscDisplay
    private void placeCloneObjects(Vector3 coneOrigin, Vector3 gazeDirection)
    {
        if (usedCameraCoordinates) { coneOrigin = lockedHeadPosition2; usedCameraCoordinates = false; }

        resetClones();
        int cloneLayer = LayerMask.NameToLayer("CloneLayer");
        int cloneLayerMask = 1 << cloneLayer;

        //circularPlane axes and origin
        Vector3 planeCenter = circularPlane.position;
        Vector3 planeNormal = circularPlane.forward;
        Vector3 planeRight = circularPlane.right;
        Vector3 planeUp = circularPlane.up;
        float planeRadius = circularPlane.lossyScale.x * 0.5f;


        float minDepth = float.MaxValue;
        float maxDepth = float.MinValue;
        // find the range of the selected objects
        foreach (GameObject coneHit in coneHits)
        {
            float dist = Vector3.Distance(coneOrigin, coneHit.transform.position);
            if (dist < minDepth) minDepth = dist;
            if (dist > maxDepth) maxDepth = dist;
        }
        float depthRange = Mathf.Max(0.01f, maxDepth - minDepth); //make sure to avoid division by 0
        bool applyDepthScaling = depthRange > 0.1f;

        // order coneHits from closest to furthes away from center
        coneHits.Sort((a, b) =>
            angleToCenter(a, coneOrigin, gazeDirection)
                .CompareTo(angleToCenter(b, coneOrigin, gazeDirection))
        );

        // PLACE CLONES
        foreach (GameObject coneHit in coneHits)
        {
            //Highlight the to be cloned objects
            Material originalHit = coneHit.GetComponent<MeshRenderer>().material;
            if (originalHit.color != confirmed && originalHit.color != target)
            {
                originalHit.SetColor("_Color", offClone);
            }

            // Determine where the clone needs to be positioned when projected to the circularPlane
            Vector3 toHit = (coneHit.transform.position - coneOrigin);
            float distanceToUser = toHit.magnitude;
            toHit = toHit.normalized;

            float horizontalAngle = Vector3.SignedAngle(gazeDirection, toHit, Vector3.up);
            Vector3 gazeHoriz = Quaternion.AngleAxis(horizontalAngle, Vector3.up) * gazeDirection;
            float verticalAngle = Vector3.SignedAngle(gazeHoriz, toHit,Vector3.Cross(Vector3.up, gazeDirection));

            //float horizontalAngle = Vector3.SignedAngle(gazeDirection, toHit, Vector3.up);
            horizontalAngle = Mathf.Clamp(horizontalAngle, -coneAngle, coneAngle);
            //float verticalAngle = Vector3.SignedAngle(gazeDirection, toHit, Vector3.Cross(Vector3.up, gazeDirection));
            verticalAngle = Mathf.Clamp(verticalAngle, -coneAngle, coneAngle);

            float xOffset = (horizontalAngle / coneAngle) * planeRadius;
            float yOffset = (verticalAngle / coneAngle) * planeRadius;
            Vector3 clonePos = planeCenter - (planeRight * xOffset) - (planeUp * yOffset);


            // Clones size depending on distance
            float minScale = 0.35f;
            float maxScale = 0.7f;
            float t = applyDepthScaling ? Mathf.Clamp01((distanceToUser - minDepth) / depthRange) : 0.5f;
            if (depthRange < 0.3) { minScale = 0.5f;}
            float cloneScaling = Mathf.Lerp(minScale , maxScale, 1f - t);
            //float cloneScaling = 0.6f;
            if (coneHit.GetComponent<MeshRenderer>().material.name.Replace(" (Instance)", "") == "MySphereMaterial 4") { cloneScaling = 0.7f; }

            // Create and place the clone
            GameObject clone = Instantiate(coneHit, clonePos, Quaternion.identity);
            clone.transform.localScale = coneHit.transform.localScale * cloneScaling;
            clone.layer = cloneLayer;
            var refOriginal = clone.AddComponent<CloneReference>();
            refOriginal.originalObject = coneHit;

            // setup for intersection search
            float cloneRadius = clone.GetComponent<MeshRenderer>().bounds.extents.magnitude;
            Vector3 pushDir = (clonePos - planeCenter).normalized;
            int maxAttempts = 20;
            float step = cloneRadius * 0.2f;
            int attempt = 0;

            // push clone if needed until there's no more overlaping with other clones
            while (attempt++ < maxAttempts)
            {
                Collider[] overlaps = Physics.OverlapSphere(clone.transform.position, cloneRadius, cloneLayerMask);

                bool hasOtherOverlap = overlaps.Any(c => c.gameObject != clone);
                if (!hasOtherOverlap) break;

                Vector3 newPos = clone.transform.position + pushDir * step;
                clone.transform.position = newPos;
            }
            clone.transform.LookAt(Camera.main.transform.position, planeUp);

            //pushes clone to the front so they can be fully seen in the circularPlane
            Renderer rend = clone.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                float halfDepth = rend.bounds.extents.magnitude;
                float circularPlaneCloneDepth = 1 - Mathf.InverseLerp(minDepth, maxDepth, distanceToUser);
                clone.transform.position += (planeNormal * halfDepth * 0.5f) + (planeNormal * circularPlaneCloneDepth) / 25f;
            }
            miniClones.Add(clone);
        }
    }
    // Calculate angle to center of the coneCast
    private float angleToCenter(GameObject hit, Vector3 coneOrigin, Vector3 gazeDirection)
    {
        Vector3 toHit = hit.transform.position - coneOrigin;
        return Vector3.Angle(gazeDirection, toHit);
    }
    // Deletes previous used clones
    private void resetClones()
    {
        foreach (var clone in miniClones) { Destroy(clone); }
        miniClones.Clear();
    }
    // verifies if user's gaze intersects circular plane
    private bool isLookingAtCirculartPlane(Vector3 origin, Vector3 direction)
    {
        int circularPlaneLayer = LayerMask.GetMask("CircularPlane");
        Ray ray = new Ray(origin, direction);
        RaycastHit hit;
        if (Physics.Raycast(ray,out hit, maxDistance, circularPlaneLayer))
        {
            if (hit.transform == circularPlane)
            {
                something = "YES SIR";
                return true;
            }
        }
        something = "NO SIR";
        return false;
    }
    // deal with all global variable when exiting coneCastLock
    private void exitCircularPlane()
    {
        circularPlane.GetComponent<MeshCollider>().enabled = false;
        circularPlane.GetComponent<MeshRenderer>().enabled = false;
        debugCone.GetComponent<MeshRenderer>().enabled = true;
        coneCastLocked = false;
        if (ogCloneLink != null) Destroy(ogCloneLink.gameObject);
        ogCloneLink = null;
        blinkConeCount += 1;

        changeTransitionState(TransitionState.Idle);
        changeWinkingState(WinkingEye.None);
        noTargetTime = 0;
        blinkCountTest += 1;
        debugSound.Play();
        resetClones();

        //avoid still having the clones color present for a few instants after exiting circularPlane
        foreach (GameObject hit in coneHits)
        {
            Material material = hit.GetComponent<MeshRenderer>().material;
            string materialName = material.name.Replace(" (Instance)", "");
            if (lastSelectedTarget.gameObject != hit && material.color != target)
            {
                if (materialName == "MySphereMaterial")
                {
                    material.SetColor("_Color", off);
                }
                else if (materialName == "MySphereMaterial 1")
                {
                    material.SetColor("_Color", set1);
                }
                else if (materialName == "MySphereMaterial 2")
                {
                    material.SetColor("_Color", set2);
                }
                else if (materialName == "MySphereMaterial 3")
                {
                    material.SetColor("_Color", set3);
                }
                else if (materialName == "MySphereMaterial 4")
                {
                    material.SetColor("_Color", set4);
                }
                else if (materialName == "MySphereMaterial 5")
                {
                    material.SetColor("_Color", set5);
                }
                ToggleOutline(hit.transform, false);
                changedMaterials.Remove(material);
                changedTransforms.Remove(hit.transform);
            }
        }
    }



    // These functions change the state of the used ENUMS
    private void changeState (EyeState newState)
    {
        currentEyeState = newState;

        Debug.Log("New eye Status: " + currentEyeState);
    }
    private void changeTransitionState(TransitionState newState)
    {
        currentTransitionState = newState;

        Debug.Log("New eye Status: " + currentEyeState);
    }
    private void changeWinkingState(WinkingEye newState)
    {
        winkingState = newState;

        Debug.Log("New wiking Status: " + winkingState);
    }
    public void changeSelection(bool right)
    {
        if (right)
        {
            selectionMethod = selectionMethod.Next();
        } 
        else
        {
            selectionMethod = selectionMethod.Previous();
        }

        if (selectionMethod != SelectionMethod.ConeCast)
        {
            circularPlane.GetComponent<MeshCollider>().enabled = false;
            circularPlane.GetComponent<MeshRenderer>().enabled = false;
            resetClones();
        }

        Debug.Log("New Selection Method in use: " + selectionMethod);
    }
    public void changeSelection2(SelectionMethod method)
    {
        selectionMethod = method;
        if (selectionMethod != SelectionMethod.ConeCast)
        {
            circularPlane.GetComponent<MeshCollider>().enabled = false;
            circularPlane.GetComponent<MeshRenderer>().enabled = false;
            resetClones();
        }
    }
    public void changeConfirmation(bool up)
    {
        if (up)
        {
            confirmationMethod = confirmationMethod.Next();
        }
        else
        {
            confirmationMethod = confirmationMethod.Previous();
        }
        Debug.Log("New Confirmation Method in use: " + confirmationMethod);
    }
    public void changeConfirmation2(ConfirmationMethod method) 
    {
        confirmationMethod = method; 
    }
    public void changeCircularPlaneConfirmation()
    {
        if (useWink) useWink = false;
        else useWink = true;
    }


    // Creates the mesh for the detection Cone
    private Mesh GenerateConeMesh(float height, float radius, int segments)
    {
        Mesh mesh = new Mesh();

        List<Vector3> vertices = new();
        List<int> triangles = new();

        // Cone tip
        vertices.Add(Vector3.zero);

        // Base circle
        for (int i = 0; i <= segments; i++)
        {
            float angle = 2 * Mathf.PI * i / segments;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            vertices.Add(new Vector3(x, y, height));
        }

        // Triangles
        for (int i = 1; i <= segments; i++)
        {
            triangles.Add(0);
            triangles.Add(i);
            triangles.Add(i + 1);
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        return mesh;
    }
    // Update Circular Plane location according to user position
    private void updateCircularPlane(Vector3 playerPosition, Vector3 gazeDirection)
    {
        if (circularPlane.GetComponent<MeshRenderer>().enabled != false)
        {
            //if (Vector3.Dot(gazeDirection, Camera.main.transform.forward) < 0)
            //if (Vector3.Dot(lockedDirection, lockedDirection2) > 0.64f)
            if (Vector3.Distance(lockedHeadPosition, lockedHeadPosition2) > 0.3f)
            {
                playerPosition = lockedHeadPosition2;
                usedCameraCoordinates = true;
                //gazeDirection = lockedDirection2;
            }
            
            float angleOffset = 10f; // 10 degrees  
            float distance = 0.35f; // 0.4 meters

            //Vector3 flatGaze = Vector3.ProjectOnPlane(gazeDirection, Vector3.up).normalized;
            //Vector3 rotatedDirection = Quaternion.AngleAxis(angleOffset, Vector3.up) * flatGaze;
            Vector3 rotatedDirection = Quaternion.AngleAxis(angleOffset, Vector3.up) * gazeDirection;
            Vector3 targetPosition = Camera.main.transform.position + rotatedDirection * distance; //Use this if it starts placing it in weird spots
                                                                                   //playerPosition -> Camera.main.transform.position
            circularPlane.position = targetPosition;
            circularPlane.rotation = Quaternion.LookRotation(-rotatedDirection, Vector3.up);

            //// Draw green gazeDirection ray
            //LineRenderer gazeRay = Instantiate(debugGazeRay);
            //gazeRay.positionCount = 2;
            //gazeRay.startColor = Color.green;
            //gazeRay.endColor = Color.green;
            //gazeRay.SetPosition(0, playerPosition);
            //gazeRay.SetPosition(1, playerPosition + gazeDirection.normalized * 0.4f);
            //debugRays.Add(gazeRay);
            //// Draw red rotatedDirection ray
            //LineRenderer rotatedRay = Instantiate(debugGazeRay);
            //rotatedRay.positionCount = 2;
            //rotatedRay.startColor = Color.red;
            //rotatedRay.endColor = Color.red;
            //rotatedRay.SetPosition(0, playerPosition);
            //rotatedRay.SetPosition(1, playerPosition + rotatedDirection.normalized * 0.4f);
            //debugRays.Add(rotatedRay);
        }
    }
    // Wink Behaviour Update
    private void updateWink(float left, float right)
    {
        // Check if eyes are closing or opening
        Debug.Log("Right Eye Openness" + right + "\nRight Eye Openness:" + left);
        if (right <= 0.7 || left <= 0.7)
        {
            // determine if eye is starting to close
            if (currentEyeState == EyeState.Open && currentTransitionState == TransitionState.Idle && keepTracking == true)
            {
                changeTransitionState(TransitionState.StartingToClose);
                keepTracking = false;
            }

            // determine if the eyes are closed
            if (right <= 0.35 || left <= 0.35)
            {
                if (currentEyeState == EyeState.Open && currentTransitionState == TransitionState.StartingToClose)
                {
                    changeTransitionState(TransitionState.Closing);
                }
                changeState(EyeState.Closed);

                // Which eye is closed
                if (right <= 0.35 && left <= 0.35)
                {
                    changeWinkingState(WinkingEye.Both);
                }
                else if (right <= 0.35 && winkingState != WinkingEye.Both)
                {
                    changeWinkingState(WinkingEye.Right);
                }
                else if (left <= 0.35 && winkingState != WinkingEye.Both)
                {
                    changeWinkingState(WinkingEye.Left);
                }

            }
            else if (right > 0.35 && left > 0.35)
            {
                if (currentEyeState == EyeState.Closed && currentTransitionState == TransitionState.Closing)
                {
                    changeTransitionState(TransitionState.Opening);
                    if ((winkingState == WinkingEye.Right || winkingState == WinkingEye.Left) && winkingState != WinkingEye.Both)
                    {
                        changeWinkingState(WinkingEye.Winked);
                    }
                }
                changeState(EyeState.Open);
            }
        }
        else if (right > 0.7 && left > 0.7)
        {
            keepTracking = true;
            changeWinkingState(WinkingEye.None);
            changeTransitionState(TransitionState.Idle);
        }
    }
    // Double Blink Behaviour Update
    private void updateDoubleBlink(float left, float right)
    {
        // Check if eyes are closing or opening
        Debug.Log("Right Eye Openness" + right + "\nRight Eye Openness:" + left);
        if (right <= 0.65 && left <= 0.65)
        {
            if (currentEyeState == EyeState.Open && currentTransitionState == TransitionState.Idle && keepTracking == true)
            {
                changeTransitionState(TransitionState.StartingToClose);
                keepTracking = false;
            }

            if (right <= 0.35 && left <= 0.35)
            {
                if (currentEyeState == EyeState.Open && currentTransitionState == TransitionState.StartingToClose)
                {
                    changeTransitionState(TransitionState.Closing);
                }
                changeState(EyeState.Closed);
            }
            else if (right > 0.35 && left > 0.35)
            {
                if (currentEyeState == EyeState.Closed && currentTransitionState == TransitionState.Closing)
                {
                    changeTransitionState(TransitionState.Opening);
                }
                changeState(EyeState.Open);
            }
        }
        else if (right > 0.65 && left > 0.65)
        {
            keepTracking = true;
        }
    }
    private void clearDebugRays()
    {
        foreach (LineRenderer ray in debugRays)
        {
            Destroy(ray.gameObject);
        }
        debugRays.Clear();
    }
}
