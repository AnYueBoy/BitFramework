using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ExportPackage
{
    [MenuItem("BitFramework/ExportFramework %e")]
    private static void ExportFramework()
    {
        // 时间戳
        string timeStamp = DateTime.Now.ToString ("yyyyMMdd");
        string projectPath = Directory.GetParent (Application.dataPath).FullName;
        string packagePath = Directory.GetParent (projectPath).FullName;

        string filePathName = packagePath + @"\BitFramework" + ".unitypackage";

        Debug.LogWarning ("path: " + filePathName);

        string assetPathName = "Assets/BitFramework";

        // you can use this api let file name to copy board
        // GUIUtility.systemCopyBuffer = fileTime;

        AssetDatabase.ExportPackage (assetPathName, filePathName, ExportPackageOptions.Recurse);

        // open package floder
        Application.OpenURL ("file:///" + packagePath);
    }
}