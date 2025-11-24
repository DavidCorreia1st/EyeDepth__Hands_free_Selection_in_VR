using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Shapes;
using Varjo.XR;
using static Varjo.XR.VarjoEyeTracking;
using UnityEngine.XR.Interaction.Toolkit.AffordanceSystem.Receiver.Primitives;
using VInspector;
//using VInspector.Libs;



public class GimballGazeTracker : MonoBehaviour
{
    public Sphere sphere = null;

    private float timer = 0.0f;
    private string filePath1;
    private string filePath2;
    private bool pathUsed = false;
    private int user = 0;
    private int position;
    private Vector3 point;
    private bool changePosition = false;
    private bool isGreen = false;
    private RaycastHit hit;
    private Color sphereColor = Color.red;
    private int positionAmount = 125;

    //private List<List<string>> gazeDataResults = new List<List<string>>();
    private List<string> gazeFrameData = new List<string>();
    private List<float> timePerPoint = new List<float>();
    private string csvGazeDataResults;

    private GimbalController gimbalController;

    [Button]
    private void resetTrial()
    {
        timePerPoint.Clear();
        gimbalController.resetTrial();
    }

    // Start is called before the first frame update
    void Start()
    {
        // Check if eye tracking is supported
        if (!IsGazeAllowed())
        {
            Debug.LogError("Varjo eye tracking is not supported on this device.");
            return;
        }


        gimbalController = GetComponent<GimbalController>();
        user = gimbalController.getUser();
        Debug.Log("Path: " + Application.persistentDataPath);
        filePath1 = Path.Combine(Application.persistentDataPath, "TimePerPoint" + user.ToString() + ".csv");
        filePath2 = Path.Combine(Application.persistentDataPath, "VarjoGazeData" + user.ToString() + ".csv");
        int i = 1;
        while (!pathUsed)
        {
            if (File.Exists(filePath2))
            {
                filePath1 = Path.Combine(Application.persistentDataPath, "TimePerPoint" + (user + i).ToString() + ".csv");
                filePath2 = Path.Combine(Application.persistentDataPath, "VarjoGazeData" + (user + i).ToString() + ".csv");
                i += 1;
                continue;
            }
            pathUsed = true;
        }

        csvGazeDataResults = "position|gazeFrameNumber|gazeCaptureTime|status|gazeOriginX|gazeOriginY|gazeOriginZ|gazeforwardX|" +
            "gazeforwardY|gazeforwardZ|isGreen|trueHorizontalAngle|trueVerticalAngle|calculatedHorizontalAngle|calculatedVerticalAngle|" +
            "calculatedPointX|calculatedPointY|calculatedPointZ|TrueDistance|calculatedDistance|focusDistance|focusStability|" +
            "leftHorizontalAngle|leftVerticalAngle|leftStatus|leftOriginX|leftOriginY|leftOriginZ|leftForwardX|leftForwardY|leftForwardZ|" +
            "rightHorizontalAngle|rightVerticalAngle|rightStatus|rightOriginX|rightOriginY|rightOriginZ|rightForwardX|rightForwardY|rightForwardZ|" +
            "bothHorizontalAngle|bothVerticalAngle|eyeFrameNumber|eyeCaptureTime|interPupillaryDistanceInMM|leftPupilIrisDiameterRatio|" +
            "rightPupilIrisDiameterRatio|leftPupilDiameterInMM|rightPupilDiameterInMM|leftIrisDiameterInMM|rightIrisDiameterInMM|" +
            "leftEyeOpenness|rightEyeOpenness\n";
        csvGazeDataResults = csvGazeDataResults.Replace("|", ",");
        System.IO.File.WriteAllText(filePath2, csvGazeDataResults);
    }

    void Update()
    {
        if (IsGazeAvailable())
        {
            int numberOfRecords = GetGazeList(out List<GazeData> gazeDatas, out List<EyeMeasurements> eyeMeasurements);
            var gazeData = GetGaze();
            gimbalController = GetComponent<GimbalController>();

            if (timePerPoint.Count != positionAmount)
            {
                for (int i = 0; i < numberOfRecords; i++)
                {
                    gazeFrameData.Clear();
                    csvGazeDataResults = "";
                    position = gimbalController.getCurrentPosition();
                    point = gimbalController.getCurrentPoint();
                    gazeFrameData.Add(position.ToString());

                    var measureGaze = gazeDatas[i];
                    var measureEye = eyeMeasurements[i];

                    #region GazeFrameDta
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
                    float trueDistance = gimbalController.getCurrentDistance();
                    float calculatedDistance;
                    Vector3 Pm;
                    calculateDistance(leftOrigin, rightOrigin, leftGaze, rightGaze, out calculatedDistance, out Pm);

                    Vector3 eyeMidPoint = (leftOrigin + rightOrigin) / 2;
                    Vector3 toTarget = (Pm - eyeMidPoint).normalized;
                    //// Horizontal angle around Y axis
                    //float horizontalAngle = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
                    //// Vertical angle around X axis
                    //float verticalAngle = -1f * Mathf.Asin(toTarget.y) * Mathf.Rad2Deg;
                    Vector3 horizontalVector = new Vector3(Pm.x, 0, Pm.z);
                    Vector3 verticalVector = new Vector3(0, Pm.y, Pm.z);
                    float horizontalAngle = Vector3.SignedAngle(Vector3.forward, horizontalVector, Vector3.up);
                    float verticalAngle = Vector3.SignedAngle(Vector3.forward, verticalVector, Vector3.right);

                    //Calculations and distance comparisons
                    gazeFrameData.Add(isGreen.ToString());

                    gazeFrameData.Add(point.x.ToString());
                    gazeFrameData.Add(point.y.ToString());
                    gazeFrameData.Add(horizontalAngle.ToString());
                    gazeFrameData.Add(verticalAngle.ToString());

                    gazeFrameData.Add(Pm.x.ToString());
                    gazeFrameData.Add(Pm.y.ToString());
                    gazeFrameData.Add(Pm.z.ToString());

                    gazeFrameData.Add(trueDistance.ToString());
                    gazeFrameData.Add(calculatedDistance.ToString());
                    gazeFrameData.Add(measureGaze.focusDistance.ToString());
                    gazeFrameData.Add(measureGaze.focusStability.ToString());

                    //left eye
                    Vector3 PmLeft = new Vector3(measureGaze.left.forward.x, measureGaze.left.forward.y, measureGaze.left.forward.z);
                    horizontalVector = new Vector3(PmLeft.x, 0, PmLeft.z);
                    verticalVector = new Vector3(0, PmLeft.y, PmLeft.z);
                    horizontalAngle = Vector3.SignedAngle(Vector3.forward, horizontalVector, Vector3.up);
                    verticalAngle = Vector3.SignedAngle(Vector3.forward, verticalVector, Vector3.right);

                    gazeFrameData.Add(horizontalAngle.ToString());
                    gazeFrameData.Add(verticalAngle.ToString());
                    gazeFrameData.Add(measureGaze.leftStatus.ToString());
                    gazeFrameData.Add(measureGaze.left.origin.x.ToString());
                    gazeFrameData.Add(measureGaze.left.origin.y.ToString());
                    gazeFrameData.Add(measureGaze.left.origin.z.ToString());
                    gazeFrameData.Add(measureGaze.left.forward.x.ToString());
                    gazeFrameData.Add(measureGaze.left.forward.y.ToString());
                    gazeFrameData.Add(measureGaze.left.forward.z.ToString());

                    //right eye
                    Vector3 PmRight = new Vector3(measureGaze.right.forward.x, measureGaze.right.forward.y, measureGaze.right.forward.z);
                    horizontalVector = new Vector3(PmRight.x, 0, PmRight.z);
                    verticalVector = new Vector3(0, PmRight.y, PmRight.z);
                    horizontalAngle = Vector3.SignedAngle(Vector3.forward, horizontalVector, Vector3.up);
                    verticalAngle = Vector3.SignedAngle(Vector3.forward, verticalVector, Vector3.right);

                    gazeFrameData.Add(horizontalAngle.ToString());
                    gazeFrameData.Add(verticalAngle.ToString());
                    gazeFrameData.Add(measureGaze.rightStatus.ToString());
                    gazeFrameData.Add(measureGaze.right.origin.x.ToString());
                    gazeFrameData.Add(measureGaze.right.origin.y.ToString());
                    gazeFrameData.Add(measureGaze.right.origin.z.ToString());
                    gazeFrameData.Add(measureGaze.right.forward.x.ToString());
                    gazeFrameData.Add(measureGaze.right.forward.y.ToString());
                    gazeFrameData.Add(measureGaze.right.forward.z.ToString());

                    //both eyes
                    Vector3 PmBoth = new Vector3(measureGaze.gaze.forward.x, measureGaze.gaze.forward.y, measureGaze.gaze.forward.z);
                    horizontalVector = new Vector3(PmBoth.x, 0, PmBoth.z);
                    verticalVector = new Vector3(0, PmBoth.y, PmBoth.z);
                    horizontalAngle = Vector3.SignedAngle(Vector3.forward, horizontalVector, Vector3.up);
                    verticalAngle = Vector3.SignedAngle(Vector3.forward, verticalVector, Vector3.right);
                    
                    gazeFrameData.Add(horizontalAngle.ToString());
                    gazeFrameData.Add(verticalAngle.ToString());

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

                    int key = gazeFrameData.Count;
                    int num = 1;
                    foreach (string dta in gazeFrameData)
                    {
                        if (num < key)
                        {
                            csvGazeDataResults += $"{dta}|";
                        }
                        else
                        {
                            csvGazeDataResults += $"{dta}";
                        }
                    }
                    csvGazeDataResults.Remove(csvGazeDataResults.Length - 1);
                    csvGazeDataResults += "\n";
                    csvGazeDataResults = csvGazeDataResults.Replace("|", ",");
                    System.IO.File.AppendAllText(filePath2, csvGazeDataResults);
                    //gazeDataResults.Add(gazeFrameData);
                }
            }

            Vector3 gazeOrigin = gazeData.gaze.origin + transform.position;
            Vector3 gazeDirection = gazeData.gaze.forward;

            Ray ray = new Ray(gazeOrigin, (transform.rotation * gazeDirection));

            //Target is Hit
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (sphere != null)
                {
                    sphere.Color = Color.green;
                    isGreen = true;
                }

                timer += Time.deltaTime;

                if (changePosition)
                {
                    timePerPoint.Add(timer);
                    timer = 0.0f;
                    changePosition = false;
                }
            }
            //target is not Hit
            else
            {
                if (position == positionAmount / 4)
                {
                    sphereColor = Color.magenta;
                }
                else if (position == positionAmount / 2)
                {
                    sphereColor = Color.blue;
                }
                else if (position == positionAmount - positionAmount / 4)
                {
                    sphereColor = Color.cyan;
                }

                if (sphere != null)
                {
                    sphere.Color = sphereColor;
                    isGreen = false;
                }
            }

            if (changePosition)
            {
                timePerPoint.Add(timer);
                timer = 0.0f;
                changePosition = false;
            }

            if (timePerPoint.Count == 125)
            {
                writeToCSV();
            }

            Debug.Log("timePerPoint:" + timePerPoint.Count.ToString());
        }
    }

    public void changeState()
    {
        changePosition = true;
    }

    private void calculateDistance(Vector3 P1, Vector3 P3, Vector3 R2, Vector3 R4, out float gazeDepth, out Vector3 Pm)
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
            return;
        }

        //calculate Vector Vector intersection
        //if (t1 || t2 < 0) then the intersection point is behind the gaze rays origins
        float t2 = ((Vector3.Dot(P13, R2) * r4dotr4) - (Vector3.Dot(P13, R4) * r2dotr4)) / denom;
        float t1 = (Vector3.Dot(P13, R2) + t2 * r2dotr2) / r2dotr4;

        Vector3 Pa = P1 + t1 * R2;
        Vector3 Pb = P3 + t2 * R4;

        Pm = (Pa + Pb) / 2;

        Vector3 eyeMidPoint = (P1 + P3) / 2;
        gazeDepth = Vector3.Distance(eyeMidPoint, Pm);
    }

    private void writeToCSV()
    {
        string csvResults = "point|time\n";

        int point = 1;
        foreach (float time in timePerPoint)
        {
            csvResults += $"{point}|{string.Join(",", time)}\n";
            point++;
        }

        csvResults = csvResults.Replace("|", ",");
        System.IO.File.WriteAllText(filePath1, csvResults);

        //C:\Users\VIMMI\AppData\LocalLow\DefaultCompany\CreatePoints_Varjo
    }
}
