﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SaintsField.Condition;

namespace SaintsField.Playa
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
    public class LayoutReadOnlyAttribute: Attribute, IPlayaAttribute, ISaintsLayoutToggle
    {
        public IReadOnlyList<ConditionInfo> ConditionInfos { get; }
        public EMode EditorMode { get; }

        public LayoutReadOnlyAttribute(EMode editorMode, params object[] by)
        {
            EditorMode = editorMode;
            ConditionInfos = Parser.Parse(by).ToArray();
        }

        public LayoutReadOnlyAttribute(params object[] by): this(EMode.Edit | EMode.Play, by)
        {
        }

        public override string ToString()
        {
            return $"<LayoutReadOnlyAttribute eMode={EditorMode} conditions={string.Join(", ", ConditionInfos)}>";
        }
    }
}
