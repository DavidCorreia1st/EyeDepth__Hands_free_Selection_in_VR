using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using VInspector;

/*
public static class ListExtensions
{
    // Method to shuffle a list
    public static void Shuffle<T>(this IList<T> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}
*/

public class DoPoints5X5 : MonoBehaviour
{
    private string filePath1;
    //private string filePath2;

    List<Vector3> points = new List<Vector3>();
    Dictionary<float, List<Vector3>> SetsByDistance = new();
    List<HashSet<Vector3>> RandomSets = new List<HashSet<Vector3>>();

    //public int distanceTransition = 24;
    private void InitializeCSVFile(string path)
    {
        //CheckAndLogDirectory(path);
        Debug.Log($"File path1: {path}");
        if (!File.Exists(path))
        {
            using (StreamWriter sw = new StreamWriter(path))
            {
                sw.WriteLine("users,positions");
                Debug.Log($"File path: {path}");
            }
        }
        else {
            System.IO.File.WriteAllText(path, string.Empty);
            using (StreamWriter sw = new StreamWriter(path))
            {
                sw.WriteLine("users,positions");
                Debug.Log($"File path: {path}");
            }
        }
    }

    private void WriteToCSV(string path, int index)
    {
        try
        {
            using (StreamWriter sw = new StreamWriter(path, true))
            {
                if (index == 1000)
                {
                    sw.WriteLine();
                }
                else if (index > 1000)
                {
                    sw.Write($"{index - 1000},");
                }
                else
                {
                    sw.Write($"({points[index].x} {points[index].y} {points[index].z}) ");
                }
            }
        }
        catch (IOException e)
        {
            Debug.LogError($"Failed to write to CSV file: {e.Message}");
        }
    }

    [Button]
    public void PreparePoints()
    {
        points.Clear();

        filePath1 = Path.Combine(Application.persistentDataPath, "positions2.csv");
        //filePath2 = Path.Combine(Application.persistentDataPath, "positions.csv");
        Debug.Log($"File path: {filePath1}");
        InitializeCSVFile(filePath1);
        //InitializeCSVFile(filePath2);

        for (float dist = 0.3f; dist <= 1.5f; dist = dist + 0.3f)
        {
            for (float horz = -10; horz <= 10; horz += 5f)
            {
                for (float vert = -10; vert <= 10; vert += 5f)
                {
                    points.Add(new Vector3(horz, vert, dist));
                }
            }
        }
        Debug.Log(string.Join(",", points));
    }

    // Method to calculate the Greatest Common Divisor (GCD) using the Euclidean algorithm
    static int GCD(int a, int b)
    {
        while (b != 0)
        {
            int temp = b;
            b = a % b;
            a = temp;
        }
        return a;
    }

    // Method to calculate the Least Common Multiple (LCM) of two numbers
    static int LCM(int a, int b)
    {
        return (a / GCD(a, b)) * b;
    }

    // Method to calculate the LCM of a list of integers
    static int LCM(List<int> numbers)
    {
        if (numbers == null || numbers.Count == 0)
        {
            throw new ArgumentException("The list of numbers cannot be null or empty.");
        }

        int lcm = numbers[0];
        foreach (int num in numbers)
        {
            lcm = LCM(lcm, num);
        }
        return lcm;
    }

    [Button]
    public void BuildSets()
    {
        string csvIndices = "users|points\n";
        string csvPoints = "users,positions\n";

        int user = 1;
        int pointOne;
        int pointTwo;
        int indiceIsGood = 0;
        List<int> indices = new List<int>();
        for (int i = 0; i < 5; i++)
        {
            indices.Clear();
            foreach (var point in points)
            {
                indices.Add(points.IndexOf(point));
                Debug.Log($"indices : {string.Join(",", indices)}");
            }

            for (int j = 0; j < 6; j++)
            {
                while (indiceIsGood < 118)
                {
                    Debug.Log($"Indices for user: {indiceIsGood} ");
                    indiceIsGood = 0;
                    indices.Shuffle();
                    //verify if adjacent transitions are in different quartiles
                    for (int k = 0; k < indices.Count - 1; k++)
                    {
                        Debug.Log($"indices : {indices.Count}, k : {k}");
                        pointOne = indices[k];
                        pointTwo = indices[k + 1];
                        //check if the [x,y] is repeated consecutively
                        if ((points[pointOne].x == points[pointTwo].x) && points[pointOne].y == points[pointTwo].y)
                        {
                            continue;
                        }

                        //check if quartile is a repeated
                        else
                        {
                            if ((points[pointOne].x < 0 && points[pointTwo].x < 0))
                            {
                                //(-,-)both in 3rd quartile
                                if (points[pointOne].y < 0 && points[pointTwo].y < 0)
                                {
                                    continue;
                                }
                                //(-,+)both in 4th quartile
                                else if (points[pointOne].y > 0 && points[pointTwo].y > 0)
                                {
                                    continue;
                                }
                                else
                                {
                                    indiceIsGood++;
                                }
                            }
                            else if ((points[pointOne].x > 0 && points[pointTwo].x > 0))
                            {
                                //(+,-)both in 2nd quartile
                                if (points[pointOne].y < 0 && points[pointTwo].y < 0)
                                {
                                    continue;
                                }
                                //(+,+)both in 1st quartile
                                else if (points[pointOne].y > 0 && points[pointTwo].y > 0)
                                {
                                    continue;
                                }
                                else
                                {
                                    indiceIsGood++;
                                }
                            }
                            else
                            {
                                indiceIsGood++;
                            }
                        }
                    }
                }
                //indices.Shuffle();
                Debug.Log($"Indices for user {user}: {string.Join(",", indices)}");

                csvIndices += $"{user}| {string.Join(",", indices)}\n";
                csvPoints += $"{user},";
                WriteToCSV(filePath1, 1000 + user);
                foreach (var index in indices)
                {
                    WriteToCSV(filePath1, index);
                    csvPoints += $"({points[index].x} {points[index].y} {points[index].z}) ";
                }
                WriteToCSV(filePath1, 1000);

                csvPoints += "\n";
                user++;
                indiceIsGood = 0;
            }
        }

        csvIndices = csvIndices.Replace(",", " ");
        csvIndices = csvIndices.Replace("|", ",");
        System.IO.File.WriteAllText(Path.Combine(Application.persistentDataPath, "indices.csv"), csvIndices);
        System.IO.File.WriteAllText(Path.Combine(Application.persistentDataPath, "positions.csv"), csvPoints);
    }

    private int GetOverlap(HashSet<Vector3> A, HashSet<Vector3> B)
    {
        HashSet<Vector3> C = new HashSet<Vector3>(A);
        C.IntersectWith(B);
        return C.Count;
    }

    private void Start()
    {
        PreparePoints();
        BuildSets();
    }

}
//C:\Users\VIMMI\AppData\LocalLow\DefaultCompany\CreatePoints_Varjo


