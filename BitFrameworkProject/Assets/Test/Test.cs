using System.Collections;
using System.Collections.Generic;
using BitFramework.Component.AssetsModule;
using BitFramework.Component.ObjectPoolModule;
using BitFramework.Core;
using UnityEngine;

public class Test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GameObject cubePrefab = App.Make<IAssetsManager>().GetAssetByUrlSync<GameObject>("Cube");
        GameObject cubeNode = App.Make<IObjectPool>().RequestInstance(cubePrefab);
        cubeNode.transform.SetParent(transform);
        cubeNode.transform.localPosition = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
    }
}