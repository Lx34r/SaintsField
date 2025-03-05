using System;
using SaintsField.Playa;
using SaintsField.Samples.Scripts.SaintsEditor;
using UnityEditor;
using UnityEngine;

namespace SaintsField.Samples.Scripts
{
    public class TableExampleSo : SaintsMonoBehaviour
    {
        [Table]
        // [GetScriptableObject]
        public Scriptable[] scriptableArray;
        public Scriptable scriptable;
        public Scriptable[] scriptables;

        [Button]
        private void Button()
        {
            Debug.Log(EditorGUIUtility.systemCopyBuffer);
        }

        [Serializable]
        public struct MyStruct
        {
            public Vector2 vector2;
            public Vector2Int vector2Int;
            public Vector3 vector3;
            public Vector3Int vector3Int;
            public Color color;

            public GameObject gameObject;
            public Scriptable so;
            public Sprite sprite;
            public Animator animator;
            public AnimationCurve curve;
        }

        public MyStruct myStruct;

        // [GetScriptableObject, Expandable]
        // public Scriptable[] rawScriptableArray;
    }
}
