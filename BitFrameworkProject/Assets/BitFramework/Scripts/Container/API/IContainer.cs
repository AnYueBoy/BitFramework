using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BitFramework.Container
{
    public interface IContainer
    {
        IBindData Bind(string service, Type concrete, bool isStatic);
    }
}