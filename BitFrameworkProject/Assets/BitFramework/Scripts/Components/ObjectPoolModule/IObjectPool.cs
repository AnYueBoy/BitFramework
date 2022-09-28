using UnityEngine;

namespace BitFramework.Component.ObjectPoolModule
{
    public interface IObjectPool
    {
        GameObject RequestInstance(GameObject prefab);

        void ReturnInstance(GameObject target);
    }
}