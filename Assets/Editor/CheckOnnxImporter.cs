using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.AssetImporters;

public static class CheckOnnxType
{
    [MenuItem("Tools/Check ONNX Type")]
    public static void Check()
    {
        Debug.Log("========== ONNX TYPE CHECK ==========");

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            if (assembly.GetName().Name.Contains("InferenceEngine.ONNX"))
            {
                Debug.Log($"FOUND ASSEMBLY: {assembly.FullName}");

                try
                {
                    var types = assembly.GetTypes();

                    foreach (var type in types)
                    {
                        Debug.Log($"TYPE: {type.FullName}");

                        var attrs = type.GetCustomAttributes(
                            typeof(ScriptedImporterAttribute),
                            false
                        );

                        foreach (var attr in attrs)
                        {
                            var importerAttr = (ScriptedImporterAttribute)attr;
                            Debug.Log(
                                $"IMPORTER ATTRIBUTE FOUND: {type.FullName} -> {importerAttr}"
                            );
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"ERROR READING ASSEMBLY: {e}");
                }
            }
        }

        var importers = AssetDatabase.GetAvailableImporters(
            "Assets/Labels/Models/model.onnx"
        );

        Debug.Log($"Importer count: {importers?.Length ?? 0}");

        if (importers != null)
        {
            foreach (var importer in importers)
            {
                Debug.Log($"Importer: {importer.GetType().FullName}");
            }
        }

        Debug.Log("========== END ==========");
    }
}