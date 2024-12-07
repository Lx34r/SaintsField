﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SaintsField.Condition;

namespace SaintsField.Playa
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
    public class PlayaDisableIfAttribute: Attribute, IPlayaAttribute
    {
        public readonly IReadOnlyList<ConditionInfo> ConditionInfos;
        public readonly EMode EditorMode;

        public PlayaDisableIfAttribute(EMode editorMode, params object[] by)
        {
            EditorMode = editorMode;
            ConditionInfos = Parser.Parse(by).ToArray();
        }

        public PlayaDisableIfAttribute(params object[] by): this(EMode.Edit | EMode.Play, by)
        {
        }

        public override string ToString()
        {
            return $"<PlayaDisableIf eMode={EditorMode} conditions={string.Join(", ", ConditionInfos)}>";
        }
    }
}
