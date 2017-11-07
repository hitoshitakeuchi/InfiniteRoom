using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections;

public class CustomEditorScript
{
    [MenuItem("Custom/Select/All Materials")]
    public static void SelectMats()
    {
        var materials = Selection.activeGameObject.GetComponentsInChildren<Renderer>().Select(b => b.sharedMaterial).ToArray();
        Selection.objects = materials;
    }
}
