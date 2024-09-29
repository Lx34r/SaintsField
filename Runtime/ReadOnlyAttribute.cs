using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using SaintsField.Condition;
using UnityEngine;

namespace SaintsField
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class ReadOnlyAttribute: PropertyAttribute, ISaintsAttribute, IReadOnlyAttribute
    {
        public SaintsAttributeType AttributeType => SaintsAttributeType.Other;
        public string GroupBy => "";

        public IReadOnlyList<ConditionInfo> ConditionInfos { get; }
        public EMode EditorMode { get; }
        public virtual bool IsReadOnly => true;

        public ReadOnlyAttribute(params object[] by): this(EMode.Edit | EMode.Play, by)
        {
        }

        public ReadOnlyAttribute(EMode editorMode, params object[] by)
        {
            EditorMode = editorMode;
            ConditionInfos = Parser.Parse(by).ToArray();
        }
    }
}
