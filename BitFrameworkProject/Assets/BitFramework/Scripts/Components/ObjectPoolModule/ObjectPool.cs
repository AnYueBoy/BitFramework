/*
 * @Author: l hy 
 * @Date: 2020-03-09 13:21:19 
 * @Description: 对象池 
 * @Last Modified by: l hy
 * @Last Modified time: 2021-02-23 21:48:17
 */

using System.Collections.Generic;
using UnityEngine;

namespace BitFramework.Component.ObjectPoolModule
{
    public class ObjectPool : IObjectPool
    {
        private Dictionary<GameObject, List<GameObject>> pool = new Dictionary<GameObject, List<GameObject>>();

        // 存放预制与实例的关系
        private Dictionary<GameObject, GameObject> relationShip = new Dictionary<GameObject, GameObject>();

        public GameObject RequestInstance(GameObject prefab)
        {
            if (pool.ContainsKey(prefab))
            {
                GameObject instance = null;
                List<GameObject> subPool = pool[prefab];
                for (int i = 0; i < subPool.Count; i++)
                {
                    GameObject temp = subPool[i];
                    if (!temp.activeSelf)
                    {
                        instance = temp;
                        break;
                    }
                }

                if (instance == null)
                {
                    GameObject gameObject = Object.Instantiate<GameObject>(prefab);
                    gameObject.name = prefab.name;
                    relationShip.Add(gameObject, prefab);
                    subPool.Add(gameObject);
                    instance = gameObject;
                }

                instance.SetActive(true);
                return instance;
            }
            else
            {
                pool.Add(prefab, new List<GameObject>());
                return RequestInstance(prefab);
            }
        }

        public void ReturnInstance(GameObject target)
        {
            if (!target.activeSelf)
            {
                return;
            }

            if (!relationShip.ContainsKey(target))
            {
                Debug.LogError("target" + target + "is not exist correspond prefab");
                return;
            }

            GameObject targetPrefab = relationShip[target];
            if (!pool.ContainsKey(targetPrefab))
            {
                Debug.LogError("targetPrefab" + targetPrefab + "is not exist correspond pool");
                return;
            }

            List<GameObject> subPool = pool[targetPrefab];
            for (int i = 0; i < subPool.Count; i++)
            {
                if (target == subPool[i])
                {
                    subPool[i].SetActive(false);
                    break;
                }
            }
        }
    }
}