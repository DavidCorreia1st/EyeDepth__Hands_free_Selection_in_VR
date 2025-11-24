using System.Collections.Generic;
using System.Data;
using System.IO;
using UnityEngine;
using Varjo.XR;
using static Varjo.XR.VarjoEyeTracking;

public class VarjoEyeTrackingTest : MonoBehaviour
{
    public LineRenderer lr = null;
    private string filePath;
    private string cvsResults;
    private List<string> gazeFrameData = new List<string>();

    void Start()
    {
        filePath = Path.Combine(Application.persistentDataPath, "GazeDta" + ".csv");
        cvsResults = "gazeFrameNumber|gazeCaptureTime|status|gazeOriginX|gazeOriginY|gazeOriginZ|gazeforwardX|" +
            "gazeforwardY|gazeforwardZ|focusDistance|focusStability|leftStatus|leftOriginX|leftOriginY|leftOriginZ|leftForwardX|" +
            "leftForwardY|leftForwardZ|rightStatus|rightOriginX|rightOriginY|rightOriginZ|rightForwardX|rightForwardY|rightForwardZ|\n";
        System.IO.File.WriteAllText(filePath, cvsResults);
        // Check if eye tracking is supported
        if (!VarjoEyeTracking.IsGazeAllowed())
        {
            Debug.LogError("Varjo eye tracking is not supported on this device.");
            return;
        }
    }

    void Update()
    {
        // Check if gaze data is available
        if (VarjoEyeTracking.IsGazeAvailable())
        {
            // Retrieve the latest gaze data
            var gazeData = VarjoEyeTracking.GetGaze();

            // Extract gaze origin and direction for the combined gaze ray
            Vector3 gazeOrigin = gazeData.gaze.origin;
            Vector3 gazeDirection = gazeData.gaze.forward;

            // Log the gaze origin and direction
            Debug.Log($"Gaze Origin: {gazeOrigin}, Gaze Direction: {gazeDirection}");

            // Example: Draw a ray in the scene view to visualize gaze
            Debug.DrawRay(transform.position, transform.rotation * gazeDirection * 10.0f, Color.green);

            Vector3[] points = new Vector3[2];
            points[0] = transform.position;
            points[1] = transform.position + transform.rotation * gazeDirection * 10.0f;
            lr.SetPositions(points);

            int numberOfRecords = GetGazeList(out List<GazeData> gazeDatas, out List<EyeMeasurements> eyeMeasurements);

            for (int i = 0; i < numberOfRecords; i++)
            {
                gazeFrameData = new List<string>();
                var measureGaze = gazeDatas[i];

                gazeFrameData.Add(measureGaze.frameNumber.ToString());
                gazeFrameData.Add(measureGaze.captureTime.ToString());

                gazeFrameData.Add(measureGaze.status.ToString());
                gazeFrameData.Add(measureGaze.gaze.origin.x.ToString());
                gazeFrameData.Add(measureGaze.gaze.origin.y.ToString());
                gazeFrameData.Add(measureGaze.gaze.origin.z.ToString());
                gazeFrameData.Add(measureGaze.gaze.forward.x.ToString());
                gazeFrameData.Add(measureGaze.gaze.forward.y.ToString());
                gazeFrameData.Add(measureGaze.gaze.forward.z.ToString());

                gazeFrameData.Add(measureGaze.focusDistance.ToString());
                gazeFrameData.Add(measureGaze.focusStability.ToString());

                gazeFrameData.Add(measureGaze.leftStatus.ToString());
                gazeFrameData.Add(measureGaze.left.origin.x.ToString());
                gazeFrameData.Add(measureGaze.left.origin.y.ToString());
                gazeFrameData.Add(measureGaze.left.origin.z.ToString());
                gazeFrameData.Add(measureGaze.left.forward.x.ToString());
                gazeFrameData.Add(measureGaze.left.forward.y.ToString());
                gazeFrameData.Add(measureGaze.left.forward.z.ToString());

                gazeFrameData.Add(measureGaze.rightStatus.ToString());
                gazeFrameData.Add(measureGaze.right.origin.x.ToString());
                gazeFrameData.Add(measureGaze.right.origin.y.ToString());
                gazeFrameData.Add(measureGaze.right.origin.z.ToString());
                gazeFrameData.Add(measureGaze.right.forward.x.ToString());
                gazeFrameData.Add(measureGaze.right.forward.y.ToString());
                gazeFrameData.Add(measureGaze.right.forward.z.ToString());


                cvsResults = "";
                foreach (string dta in gazeFrameData)
                {
                    cvsResults += $"{dta}|";
                }

                cvsResults.Remove(cvsResults.Length - 1);
                cvsResults += "\n";
                System.IO.File.AppendAllText(filePath, cvsResults);
            }
        }
        else
        {
            Debug.Log("Gaze data is not valid.");
        }
    }
}

