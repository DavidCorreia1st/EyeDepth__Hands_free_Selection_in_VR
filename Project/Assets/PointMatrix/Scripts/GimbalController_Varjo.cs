using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Shapes;
using Varjo.XR;
using static Varjo.XR.VarjoEyeTracking;
using UnityEngine.XR.Interaction.Toolkit.AffordanceSystem.Receiver.Primitives;
using VInspector;
using System;
using System.Linq;



public class GimbalController : MonoBehaviour
{

    public SphereCollider sphereCollider = null;

    [Range(-180, 180)] public float horizontalAngle;
    [Range(-90, 90)] public float VerticalAngle;
    [Range(0, 100)] public float distance;

    Dictionary<float, float> radiusConvertion = new Dictionary<float, float>
        {
            { 0.3f, 0.00975f },
            { 0.6f, 0.0195f },
            { 0.9f, 0.0292f },
            { 1.2f, 0.0390f },
            { 1.5f, 0.0488f }
        };

    /*sphere 30px - diameter sizes for hitbox
     * 0.0195f 
     * 0.039f 
     * 0.0584f 
     * 0.0779f 
     * 0.0975f 
     */

    /*sphere 20px - radius sizes for hitbox
     * 0.008f 
     * 0.0159f 
     * 0.0398f 
     * 0.0238f 
     * 0.0318f 
     */

    /*incorrect original radius sizes for hitbox
     * 0.05f  
     * 0.08f  
     * 0.13f  
     * 0.18f  
     * 0.22f  
     */

    [SerializeField] private Transform _rotateHorizontal;
    [SerializeField] private Transform _rotateVertical;
    [SerializeField] private Transform _distance;
    //[SerializeField] private bool runOnStart = false;

    float currentHorizontalAngle = 0;
    float currentVerticalAngle = 0;
    float currentDistance = 0;
    float radius = 0;
    float currentRadius = 0;

    private bool isAnimating = false;
    private string filePath;
    public int user = 1;
    private int currentPosition = 0;
    private bool startCode = false;
    private bool routinestarted = false;
    private bool isDone = false;
    private Vector3 currentPoint = Vector3.zero;

    private Dictionary<int, List<Vector3>> userData = new();
    private GimballGazeTracker gimballGazeTracker;

    #region Point Manager
    [Button]
    public void SetPoint(float horizontalAngle, float VerticalAngle, float distance, float currentRadius)
    {
        sphereCollider.radius = currentRadius;
        _rotateHorizontal.localEulerAngles = new Vector3(0, horizontalAngle, 0);
        _rotateVertical.localEulerAngles = new Vector3(VerticalAngle, 0, 0);
        this._distance.localPosition = new Vector3(0, 0, distance);
    }

    public List<Vector3> positions = new List<Vector3>();

    public void InitializePositions()
    {
        for (int dist = 1; dist < 5; dist++)
        {
            for (int hor = -10; hor < 11; hor += 5)
            {
                for (int ver = -10; ver < 11; ver += 5)
                {
                    positions.Add(new Vector3(hor, ver, dist));
                }
            }
        }
    }

    static Vector3 ParseVector(string s)
    {
        var parts = s.Split(' ').Select(float.Parse).ToArray();
        return new Vector3(parts[0], parts[1], parts[2]);
    }

    [Button]
    public void Getpoints(string path)
    {
        if (File.Exists(path))
        {
            var lines = File.ReadAllLines(path);

            // Process each line (skip header)
            foreach (var line in lines.Skip(1))
            {
                var parts = line.Split(',');
                int user = int.Parse(parts[0]);

                // Parse the positions
                //List<Vector3> positions = new List<Vector3>();
                positions = parts[1].Trim()
                                        .Split(new[] { ") (" }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(s => s.Trim('(', ')'))
                                        .Select(ParseVector)
                                        .Where(v => v != null)
                                        .ToList();

                //positions.Insert(0, Vector3.zero);
                userData[user] = positions;
            }
        }
    }

    [Button]
    private void SetRandomPosition()
    {
        if (positions.Count == 0)
        {

            InitializePositions();
        }

        var pos = positions[UnityEngine.Random.Range(0, positions.Count - 1)];
        horizontalAngle = pos.x;
        VerticalAngle = pos.y;
        distance = pos.z;
    }

    private void setNextPosition()
    {
        if (userData.TryGetValue(user, out List<Vector3> userVectors))
        {
            horizontalAngle = userVectors[currentPosition].x;
            VerticalAngle = userVectors[currentPosition].y;
            distance = userVectors[currentPosition].z;
            radius = radiusConvertion[distance] / sphereCollider.gameObject.transform.localScale.x; //Account for possible downscale of the gameObject
            currentPoint = userVectors[currentPosition];
            Debug.Log("distance: " + distance + " radius: " + radius);

            currentPosition++;
            
        }
    }

    #endregion

    public int getCurrentPosition()
    {
        return currentPosition;
    }

    public int getUser()
    {
        return user;
    }

    public float getCurrentDistance()
    {
        return distance;
    }

    public Vector3 getCurrentPoint()
    {
        return currentPoint;
    }

    public void resetTrial()
    {
        currentPosition = 0;
    }

    private IEnumerator CheckConditionEveryTwoSeconds()
    {
        while (!isDone)
        {
            yield return new WaitForSeconds(3f);

            Debug.Log("Condition met at: " + Time.time + "position" + currentPosition);

            if (currentPosition <= 125)
            {
                gimballGazeTracker = GetComponent<GimballGazeTracker>();
                gimballGazeTracker.changeState();
                if (currentPosition < 125)
                {
                    setNextPosition();
                }
            }
        }
    }

    IEnumerator WaitAndDoSomething()
    {
        yield return new WaitForSeconds(3); // Wait for 3 seconds

        startCode = true;
        // Code here will execute after waiting for 3 seconds
        Debug.Log("Waited for 3 seconds!");
    }

    private void Start()
    {
        StartCoroutine(WaitAndDoSomething());
    }

    private void Update()
    {
        Debug.Log("Point: " + "horizontal" + horizontalAngle + "VerticalAngle" + VerticalAngle + "distance" + distance);
        if (currentPosition == 126)
        {
            isDone = true;
            Debug.Log("It's done");
            return;
        }

        if (startCode && !routinestarted)
        {
            filePath = Path.Combine(Application.persistentDataPath, "positions2.csv");
            Getpoints(filePath);
            //userData[user] = new() { Vector3.forward };
            setNextPosition();
            StartCoroutine(CheckConditionEveryTwoSeconds());
            routinestarted = true;
            Debug.Log("Routine Has started");
        }

        if (routinestarted)
        {
            isAnimating = false;

            //moves sphere closer bit by bit for distance
            if (currentDistance != distance)
            {
                if (Mathf.Abs(distance - currentDistance) < 0.01f)
                {
                    currentDistance = distance;
                }
                else
                {
                    currentDistance += (distance - currentDistance) / 10;
                }
                isAnimating = true;
            }

            //adjusts sphere collider radius bit by bit during transition
            if (currentRadius != radius)
            {
                if (Mathf.Abs(radius - currentRadius) < 0.01f)
                {
                    currentRadius = radius;
                }
                else
                {
                    currentRadius += (radius - currentRadius) / 10;
                }
                isAnimating = true;
            }

            //moves sphere closer bit by bit for horizontalAngle
            if (currentHorizontalAngle != horizontalAngle)
            {
                if (Mathf.Abs(horizontalAngle - currentHorizontalAngle) < 0.01f)
                {
                    currentHorizontalAngle = horizontalAngle;
                }
                else
                {
                    currentHorizontalAngle += (horizontalAngle - currentHorizontalAngle) / 10;
                }
                isAnimating = true;
            }

            //moves sphere closer bit by bit for VerticalAngle
            if (currentVerticalAngle != VerticalAngle)
            {
                if (Mathf.Abs(VerticalAngle - currentVerticalAngle) < 0.01f)
                {
                    currentVerticalAngle = VerticalAngle;
                }
                else
                {
                    currentVerticalAngle += (VerticalAngle - currentVerticalAngle) / 10;
                }
                isAnimating = true;
            }

            //move object if it's not in the target position
            if (isAnimating)
            {
                SetPoint(currentHorizontalAngle, currentVerticalAngle, currentDistance, currentRadius);
            }
        }

    }

}