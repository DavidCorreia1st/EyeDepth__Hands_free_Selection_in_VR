using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EnumExtension
{
    public static T Next<T>(this T src) where T : struct, System.Enum
    {
        T[] values = (T[])System.Enum.GetValues(src.GetType());
        int index = System.Array.IndexOf(values, src) + 1;
        return (index == values.Length) ? values[0] : values[index];
    }
    public static T Previous<T>(this T src) where T : struct, System.Enum
    {
        T[] values = (T[])System.Enum.GetValues(src.GetType());
        int index = System.Array.IndexOf(values, src) - 1;
        return (index < 0) ? values[values.Length - 1] : values[index];
    }
}
