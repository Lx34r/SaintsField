using System;
using System.Collections.Generic;
using System.Linq;
using SaintsField.Playa;
using SaintsField.Samples.Scripts.SaintsEditor;
using UnityEngine;

namespace SaintsField.Samples.Scripts.IssueAndTesting.Issue
{
    public class Issue97 : SaintsMonoBehaviour
    {
        [Serializable]
        public class MyClass
        {
            public string unique;
        }

        [OnValueChanged(nameof(ValueChanged))]
        [OnArraySizeChanged(nameof(SizeChanged))]
        public MyClass[] myClasses;

        public void ValueChanged(MyClass myClass, int index)
        {
            Debug.Log($"OnValueChanged: {myClass.unique} at {index}");
        }

        public void SizeChanged(IReadOnlyList<MyClass> myClassNewValues)
        {
            Debug.Log($"OnArraySizeChanged: {string.Join("; ", myClassNewValues.Select(each => each?.unique))}");
        }

#if UNITY_2021_3_OR_NEWER
        public interface IMyInterface
        {
        }

        [Serializable]
        public struct MyStructIn : IMyInterface
        {
            public int myInt;
            public override string ToString() => $"MyStructIn: {myInt}";
        }

        [Serializable]
        public class MyClassIn : IMyInterface
        {
            public string myString = "Default String";
            public override string ToString() => $"MyClassIn: {myString}";
        }
        [SerializeReference, ReferencePicker, OnArraySizeChanged(nameof(InterfaceSizeChanged)), OnValueChanged(nameof(InterfacesValueChanged))]
        public IMyInterface[] myInterfaces;

        public void InterfaceSizeChanged(IReadOnlyList<IMyInterface> newValues)
        {
            Debug.Log($"InterfacesValueChanged to {newValues.Count}: {string.Join(", ", newValues)}");
        }
        public void InterfacesValueChanged(IMyInterface newValue, int index)
        {
            Debug.Log($"Interfaces[{index}] changed to {newValue}");
        }
#endif
    }
}