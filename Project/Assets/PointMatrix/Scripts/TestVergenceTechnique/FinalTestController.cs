using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;
using static UnityEngine.UIElements.UxmlAttributeDescription;

public class FinalTestController : MonoBehaviour
{
    public Transform rateMenu; //not sure ratings will be necessary this time around
    public Transform startingScreen; //introduces tutorial section where 6 selections will be available
    public Transform endingScreen;
    public Transform shortBreakScreen; //this one would be after each rating except last one
    public Transform cachedObjects;
    public Transform Block_A;
    public Transform Block_B;
    public Transform Block_C;
    public Transform Block_D;
    public int playerID;

    private VarjoVergengeHandlerPrototypeOutlines vergenceOutlineHandler;
    private VarjoVergengeHandlerPrototypeOutlines.ConfirmationMethod confirmationMethod = 
        VarjoVergengeHandlerPrototypeOutlines.ConfirmationMethod.Winking;

    private List<Transform> selectedObjects = new();
    private List<Transform> Targets = new();
    private List<float> results = new();
    private List<float> results2 = new();
    private List<int> targetsID = new() { 183, 379, 392, 621, 648, 1053, 1114, 1139 ,1150, 1155 }; // These objects IDs will be the ones used in
                                                                                                   // the experiment and they need to be ordered
    private List<List<int>> targetsPath = new()
    {
        new List<int>() { 6, 8, 9, 4, 2, 5, 7, 3, 1, 0 },
        new List<int>() { 7, 2, 4, 0, 1, 3, 9, 8, 6, 5 },
        new List<int>() { 8, 4, 1, 5, 6, 7, 9, 0, 2, 3 },
        new List<int>() { 0, 9, 2, 1, 6, 3, 4, 5, 8, 7 }
    };
    private List<VarjoVergengeHandlerPrototypeOutlines.SelectionMethod> tutorialPath = new() 
    {VarjoVergengeHandlerPrototypeOutlines.SelectionMethod.OverlapSphere, VarjoVergengeHandlerPrototypeOutlines.SelectionMethod.SphereCast, 
        VarjoVergengeHandlerPrototypeOutlines.SelectionMethod.RayCast, VarjoVergengeHandlerPrototypeOutlines.SelectionMethod.ConeCast};
    private List<List<VarjoVergengeHandlerPrototypeOutlines.SelectionMethod>> selectionMethodPaths = new()
    {
        new() { VarjoVergengeHandlerPrototypeOutlines.SelectionMethod.SphereCast, VarjoVergengeHandlerPrototypeOutlines.SelectionMethod.OverlapSphere,
            VarjoVergengeHandlerPrototypeOutlines.SelectionMethod.ConeCast, VarjoVergengeHandlerPrototypeOutlines.SelectionMethod.RayCast },

        new() { VarjoVergengeHandlerPrototypeOutlines.SelectionMethod.OverlapSphere, VarjoVergengeHandlerPrototypeOutlines.SelectionMethod.RayCast,
            VarjoVergengeHandlerPrototypeOutlines.SelectionMethod.SphereCast, VarjoVergengeHandlerPrototypeOutlines.SelectionMethod.ConeCast },

        new() { VarjoVergengeHandlerPrototypeOutlines.SelectionMethod.RayCast, VarjoVergengeHandlerPrototypeOutlines.SelectionMethod.ConeCast,
            VarjoVergengeHandlerPrototypeOutlines.SelectionMethod.OverlapSphere, VarjoVergengeHandlerPrototypeOutlines.SelectionMethod.SphereCast },

        new() { VarjoVergengeHandlerPrototypeOutlines.SelectionMethod.ConeCast, VarjoVergengeHandlerPrototypeOutlines.SelectionMethod.SphereCast,
            VarjoVergengeHandlerPrototypeOutlines.SelectionMethod.RayCast, VarjoVergengeHandlerPrototypeOutlines.SelectionMethod.OverlapSphere }
    };

    private int blockCounter = 0;
    private int waitForIt = 0;
    private int tutorialTries = 0;
    private int tutorialSelection = 0;
    private int currentSelection = 0;
    private int selectionPath;
    private int currentTarget = 0;
    private float targetTime = 0f;
    private bool tutorialON = false;
    private bool targetsSelectionON = false;
    private bool testIsOver = false;
    private string csvResults;
    private string filePath;


    IEnumerator WaitAndDoSomething()
    {
        yield return new WaitForSeconds(5); // Wait for 5 seconds
        Debug.Log("Waited for 5 seconds!");
        waitForIt++;
    }

    void Start()
    {
        //results.Add(playerID);
        vergenceOutlineHandler = GetComponent<VarjoVergengeHandlerPrototypeOutlines>();
        filePath = Path.Combine(Application.persistentDataPath, "TargetsFinalTest2.csv");
        if (!File.Exists(filePath)) 
        {
            //csvResults = "PlayerID|T1_SO1|T1_1_tries|T1_time_1|T1_SO2|T1_2_tries|T1_time_2|T1_SO3|T1_3_tries|T1_time_3|T1_SO4|T1_4_tries|T1_time_4|T1_SO5|" +
            //    "T1_5_tries|T1_time_5|T1_SO6|T1_6_tries|T1_time_6|T1_SO7|T1_7_tries|T1_time_7|T1_SO8|T1_8_tries|T1_time_8|T1_SO9|T1_9_tries|T1_time_9|T1_SO10|" +
            //    "T1_10_tries|T1_time_10|Tech1 Rating|T2_SO1|T2_1_tries|T2_time_1|T2_SO2|T2_2_tries|T2_time_2|T2_SO3|T2_3_tries|T2_time_3|T2_SO4|T2_4_tries|" +
            //    "T2_time_4|T2_SO5|T2_5_tries|T2_time_5|T2_SO6|T2_6_tries|T2_time_6|T2_SO7|T2_7_tries|T2_time_7|T2_SO8|T2_8_tries|T2_time_8|T2_SO9|T2_9_tries|" +
            //    "T2_time_9|T2_SO10|T2_10_tries|T2_time_10|Tech2 Rating|T3_SO1|T3_1_tries|T3_time_1|T3_SO2|T3_2_tries|T3_time_2|T3_SO3|T3_3_tries|T3_time_3|T3_SO4|" +
            //    "T3_4_tries|T3_time_4|T3_SO5|T3_5_tries|T3_time_5|T3_SO6|T3_6_tries|T3_time_6|T3_SO7|T3_7_tries|T3_time_7|T3_SO8|T3_8_tries|T3_time_8|T3_SO9|" +
            //    "T3_9_tries|T3_time_9|T3_SO10|T3_10_tries|T3_time_10|Tech3 Rating|T4_SO1|T4_1_tries|T4_time_1|T4_SO2|T4_2_tries|T4_time_2|T4_SO3|T4_3_tries|" +
            //    "T4_time_3|T4_SO4|T4_4_tries|T4_time_4|T4_SO5|T4_5_tries|T4_time_5|T4_SO6|T4_6_tries|T4_time_6|T4_SO7|T4_7_tries|T4_time_7|T4_SO8|T4_8_tries|" +
            //    "T4_time_8|T4_SO9|T4_9_tries|T4_time_9|T4_SO10|T4_10_tries|T4_time_10|Tech4 Rating\n";
            csvResults = "PlayerID|Trial_Number|Trial|Target|Selected_Object|Tries|Distance|Time|Rating\n";
            csvResults = csvResults.Replace("|", ",");
            System.IO.File.WriteAllText(filePath, csvResults);
            csvResults = "";
        } //CREATE CSV FILE AND COLUMNS
        selectionPath = playerID;
        while (selectionPath > 4)
        {
            selectionPath -= 4;
        }
        StartCoroutine(WaitAndDoSomething());
    }


    // Update is called once per frame
    void Update()
    {
        if (waitForIt == 1f)
        {
            selectedObjects = vergenceOutlineHandler.getSelectedObjects();
            waitForIt++;
        }
        else if (waitForIt == 2f)
        {
            foreach (Transform Object in selectedObjects)
            {
                string objectName = Object.gameObject.name;
                Match match = Regex.Match(objectName, @"^dna_molecules\.(\d+)$");
                if (match.Success)
                {
                    int number = int.Parse(match.Groups[1].Value);
                    if (targetsID.Contains(number))
                    {
                        Targets.Add(Object);
                    }
                }
            }
            vergenceOutlineHandler.changeConfirmation2(confirmationMethod);
            //vergenceOutlineHandler.changeSelection2(selectionMethodPaths[selectionPath - 1][currentSelection]);
            vergenceOutlineHandler.nextTarget(); //Prepare 1st target although dna molecules are not visible yet
            activateDeactivateScenery(startingScreen);
            waitForIt++;
        }
        else if (waitForIt == 3)
        {
            targetTime += Time.deltaTime;
        }
    }

    // Gets the Targets depending on the current path and position
    public Transform getnewTarget(bool resetPositions = false)
    {
        //Tutorial started
        if (tutorialON)
        {
            tutorialTries++;
            if (tutorialTries < 4)
            {
                tutorialHandler();
            }
            else if (tutorialTries == 6)
            {
                endTutorial();
            }
        }
        //Trial started
        if (targetsSelectionON)
        {
            if (!resetPositions) { currentTarget++; }
            else // finished the current positions for the technique
            {
                activateDeactivateScenery(rateMenu);
                targetsSelectionON = false;
                currentTarget = 0;

                if (currentSelection < 3) { currentSelection++; }
                else { testIsOver = true; currentSelection = 0; }
            }

            float rounded = (float)Math.Round(targetTime, 2);
            results.Add(rounded);
            if (!resetPositions) { results.Add(0f); writeToCsv();  } // only last target will get the rating, the rest will have a 0 and write to CSV
        }
        targetTime = 0f;
        return Targets[ targetsPath[currentSelection][currentTarget] ];
    }
    public int getCurrentTarget()
    {
        return currentTarget + 1;
    }
    public bool getTutorialOn() 
    {
        return tutorialON;
    }
    // load the current section of the experiment
    private void activateDeactivateScenery(Transform screenToActivate)
    {
        //reset all scenery
        rateMenu.gameObject.SetActive(false);
        startingScreen.gameObject.SetActive(false);
        endingScreen.gameObject.SetActive(false);
        shortBreakScreen.gameObject.SetActive(false);
        cachedObjects.gameObject.SetActive(false);
        Debug.Log("BLABLABLA:" + $"{screenToActivate.name}");
        screenToActivate.gameObject.SetActive(true);
    }
    // activate tutorial before starting trial
    public void tutorialHandler() 
    {
        if (!tutorialON)
        {
            activateDeactivateScenery(cachedObjects);
            tutorialON = true;
            vergenceOutlineHandler.testStarted = true;
        }
        vergenceOutlineHandler.changeConfirmation2(confirmationMethod);
        vergenceOutlineHandler.changeSelection2(tutorialPath[tutorialSelection]);
        tutorialSelection++;
    }
    // exit the tutorial section
    private void endTutorial()
    {
        //setup 1st selection method
        VarjoVergengeHandlerPrototypeOutlines.SelectionMethod newMethod = selectionMethodPaths[selectionPath - 1][currentSelection];// Going back to normal confirmation after ConeCast is used
        vergenceOutlineHandler.changeConfirmation2(confirmationMethod);
        vergenceOutlineHandler.changeSelection2(newMethod);
        activateDeactivateScenery(shortBreakScreen);
        setBlock(true);
        tutorialON = false;
        vergenceOutlineHandler.nextTarget(); //Prepare 1st target although dna molecules are not visible yet
    }
    //exit the menu for shortBreak
    public void exitShortBreak()
    {
        activateDeactivateScenery(cachedObjects);
        targetsSelectionON = true;
        //results.RemoveAt(results.Count - 1); //PROBABLY WON'T BE NEEDED IF I FIX IT
    }
    // Rating has been finalized
    public void finishedRating(int rating)
    {
        VarjoVergengeHandlerPrototypeOutlines.SelectionMethod newMethod = selectionMethodPaths[selectionPath - 1][currentSelection];
        rateMenu.gameObject.SetActive(false);
        if (!testIsOver) { activateDeactivateScenery(shortBreakScreen); setBlock(); }
        //results.RemoveAt(results.Count - 1); //PROBABLY WON'T BE NEEDED IF I FIX IT
        results.Add(rating);
        writeToCsv();
        // Going back to normal confirmation in case ConeCast is used
        vergenceOutlineHandler.changeConfirmation2(confirmationMethod); 
        vergenceOutlineHandler.changeSelection2(newMethod);

        // SAVE RESULTS
        if (testIsOver) 
        {
            cachedObjects.gameObject.SetActive(false);
            endingScreen.gameObject.SetActive(true);
            //writeToCsv();
        }
    }
    // Register Selected object
    public void writeSelectedObject(MeshRenderer meshRenderer, int tries)
    {
        string num = meshRenderer.name.Substring(meshRenderer.name.LastIndexOf(".") + 1); //DNA MODECULE NUMBER
        results.Add(float.Parse(num));
        results.Add(tries);
    }
    public void writeSelectedObject2(MeshRenderer meshRenderer, int tries, float distance)
    {
        results.Clear();
        Transform target = Targets[targetsPath[currentSelection][currentTarget]];
        string num = target.name.Substring(meshRenderer.name.LastIndexOf(".") + 1); //DNA MODECULE NUMBER;
        csvResults = $"{playerID},{currentSelection+1},{selectionMethodPaths[selectionPath - 1][currentSelection]},{num},";
        num = meshRenderer.name.Substring(meshRenderer.name.LastIndexOf(".") + 1); //DNA MODECULE NUMBER
        results.Add(float.Parse(num));
        results.Add(tries);
        results.Add(distance);
    }
    public void writeToCsv()
    {
        int num = 1;
        int key = results.Count;
        foreach (float result in results)
        {
            if (num < key)
            {
                csvResults += $"{result.ToString()},";
            }
            else
            {
                csvResults += $"{result.ToString()}";
            }
            num++;
        }
        csvResults += "\n";
        System.IO.File.AppendAllText(filePath, csvResults);
    }

    private void setBlock(bool isFirstTime = false)
    {
        if (currentSelection == 0)
        {
            Block_A.gameObject.SetActive(true);
        }
        if (currentSelection == 1)
        {
            Block_A.gameObject.SetActive(false);
            Block_B.gameObject.SetActive(true);
        }
        if (currentSelection == 2)
        {
            Block_B.gameObject.SetActive(false);
            Block_C.gameObject.SetActive(true);
        }
        if (currentSelection == 3)
        {
            Block_C.gameObject.SetActive(false);
            Block_D.gameObject.SetActive(true);
        }
    }
}


// C:\Users\VIMMI\AppData\LocalLow\DefaultCompany\CreatePoints_Varjo