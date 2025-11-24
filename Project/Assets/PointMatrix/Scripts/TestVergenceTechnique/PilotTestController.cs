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

public class PilotTestController : MonoBehaviour
{
    public Transform rateMenu;
    public Transform endingScreen;
    public Transform infoDoubleBlinking;
    public Transform infoWinking;
    public Transform infoDwell;
    public Transform cachedObjects;
    public int playerID;

    private VarjoVergengeHandlerPrototypeOutlines vergenceOutlineHandler;

    private List<Transform> selectedObjects = new();
    private List<Transform> Targets = new();
    private List<float> results = new();
    private List<int> targetsID = new() { 183, 379, 392, 621, 648, 1053, 1114, 1139 ,1150, 1155 }; // These objects IDs will be the ones used in the experiment
    private List<List<int>> targetsPath = new()
    {
        new List<int>() { 6, 8, 9, 4, 2, 5, 7, 3, 1, 0 },                             
        new List<int>() { 7, 2, 4, 0, 1, 3, 9, 8, 6, 5 },
        new List<int>() { 8, 4, 1, 5, 6, 7, 9, 0, 2, 3 }
    };
    private List<List<VarjoVergengeHandlerPrototypeOutlines.ConfirmationMethod>> confirmartionMethodPaths = new()
    {
        new() { VarjoVergengeHandlerPrototypeOutlines.ConfirmationMethod.Winking, VarjoVergengeHandlerPrototypeOutlines.ConfirmationMethod.DoubleBlinking, VarjoVergengeHandlerPrototypeOutlines.ConfirmationMethod.Dwell },
        new() { VarjoVergengeHandlerPrototypeOutlines.ConfirmationMethod.DoubleBlinking, VarjoVergengeHandlerPrototypeOutlines.ConfirmationMethod.Dwell, VarjoVergengeHandlerPrototypeOutlines.ConfirmationMethod.Winking },
        new() { VarjoVergengeHandlerPrototypeOutlines.ConfirmationMethod.Dwell, VarjoVergengeHandlerPrototypeOutlines.ConfirmationMethod.Winking, VarjoVergengeHandlerPrototypeOutlines.ConfirmationMethod.DoubleBlinking }
    };

    private int waitForIt = 0;
    private int currentPath = 0;
    private int currentTarget = -1;
    private int confirmationPath;
    private float targetTime = 0f;
    private bool tutorialON = false;
    private bool skipFirstTime = true;
    private bool pilotIsOver = false;
    private string csvResults;
    private string filePath;


    // Start is called before the first frame update
    IEnumerator WaitAndDoSomething()
    {
        yield return new WaitForSeconds(5); // Wait for 5 seconds
        Debug.Log("Waited for 5 seconds!");
        waitForIt++;
    }

    void Start()
    {
        results.Add(playerID);
        vergenceOutlineHandler = GetComponent<VarjoVergengeHandlerPrototypeOutlines>();
        filePath = Path.Combine(Application.persistentDataPath, "Targets.csv");
        if (!File.Exists(filePath)) 
        {
            csvResults = "PlayerID|T1_SO1|T1_time_1|T1_SO2|T1_time_2|T1_SO3|T1_time_3|T1_SO4|T1_time_4|T1_SO5|T1_time_5|T1_SO6|" +
            "T1_time_6|T1_SO7|T1_time_7|T1_SO8|T1_time_8|T1_SO9|T1_time_9|T1_SO10|T1_time_10|Tech1 Rating|T2_SO1|T2_time_1|T2_SO2|" +
            "T2_time_2|T2_SO3|T2_time_3|T2_SO4|T2_time_4|T2_SO5|T2_time_5|T2_SO6|T2_time_6|T2_SO7|T2_time_7|T2_SO8|T2_time_8|T2_SO9|" +
            "T2_time_9|T2_SO10|T2_time_10|Tech2 Rating|T3_SO1|T3_time_1|T3_SO2|T3_time_2|T3_SO3|T3_time_3|T3_SO4|T3_time_4|T3_SO5|" +
            "T3_time_5|T3_SO6|T3_time_6|T3_SO7|T3_time_7|T3_SO8|T3_time_8|T3_SO9|T3_time_9|T3_SO10|T3_time_10|Tech3 Rating\n";
            csvResults = csvResults.Replace("|", ",");
            System.IO.File.WriteAllText(filePath, csvResults);
            csvResults = "";
        }
        confirmationPath = playerID;
        while (confirmationPath > 3)
        {
            confirmationPath -= 3;
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
            vergenceOutlineHandler.changeConfirmation2(confirmartionMethodPaths[confirmationPath - 1][currentPath]);
            vergenceOutlineHandler.nextTarget();
            activateDeactivateTutorial(confirmartionMethodPaths[confirmationPath - 1][currentPath]);
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
        //if (currentTarget == 9) { skipFirstTime = true; }
        if (!resetPositions) 
        { 
            if (!tutorialON) { currentTarget++; } // iterate position
        }
        else // finished the current positions for the technique
        {
            cachedObjects.gameObject.SetActive(false);
            rateMenu.gameObject.SetActive(true);
            currentTarget = -1;
            if (currentPath < 2)
            {
                currentPath++;
            }
            else 
            {
                pilotIsOver = true;
                currentPath = 0;
            } 
        }
        if (!skipFirstTime || tutorialON) // Avoid readings when the 1st target is being setup and when tutorial is on
        {
            float rounded = (float)Math.Round(targetTime, 2);
            results.Add(rounded);
        }
        targetTime = 0f;
        skipFirstTime = false;

        return Targets[targetsPath[currentPath][currentTarget]];
    }
    public int getCurrentTarget()
    {
        return currentTarget + 1;
    }
    // Go to next technique
    public void finishedRating(int rating)
    {
        VarjoVergengeHandlerPrototypeOutlines.ConfirmationMethod newMethod = confirmartionMethodPaths[confirmationPath - 1][currentPath];
        rateMenu.gameObject.SetActive(false);
        if (!pilotIsOver) { activateDeactivateTutorial(newMethod); }
        results.RemoveAt(results.Count - 1);
        results.Add(rating);
        vergenceOutlineHandler.changeConfirmation2(newMethod);
        

        // SAVE RESULTS
        if (pilotIsOver) 
        {
            cachedObjects.gameObject.SetActive(false);
            endingScreen.gameObject.SetActive(true);
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
    }
    // activate tutorial before starting trial
    private void activateDeactivateTutorial(VarjoVergengeHandlerPrototypeOutlines.ConfirmationMethod method) 
    {
        tutorialON = !tutorialON;
        if (method == VarjoVergengeHandlerPrototypeOutlines.ConfirmationMethod.DoubleBlinking)
        {
            infoDoubleBlinking.gameObject.SetActive(!infoDoubleBlinking.gameObject.activeSelf);
        }
        else if (method == VarjoVergengeHandlerPrototypeOutlines.ConfirmationMethod.Winking)
        {
            infoWinking.gameObject.SetActive(!infoWinking.gameObject.activeSelf);
        }
        else if (method == VarjoVergengeHandlerPrototypeOutlines.ConfirmationMethod.Dwell)
        {
            infoDwell.gameObject.SetActive(!infoDwell.gameObject.activeSelf);
        }
    }
    //exit the menu for tutorial
    public void endTutorial()
    {
        activateDeactivateTutorial(confirmartionMethodPaths[confirmationPath - 1][currentPath]);
        cachedObjects.gameObject.SetActive(true);
        int num = results.Count;
        results.RemoveAt(results.Count - 1);
    }
    // Register Selected object
    public void writeSelectedObject(MeshRenderer meshRenderer)
    {
        string num = meshRenderer.name.Substring(meshRenderer.name.LastIndexOf(".") + 1);
        results.Add(float.Parse(num));
    }
    public void blabla(MeshRenderer meshRenderer)
    {
        csvResults += meshRenderer.name;
    }
}


// C:\Users\VIMMI\AppData\LocalLow\DefaultCompany\CreatePoints_Varjo