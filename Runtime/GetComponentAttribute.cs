﻿using System;
using System.Diagnostics;
using SaintsField.Playa;
using UnityEngine;

namespace SaintsField
{
    [Conditional("UNITY_EDITOR")]
    public class GetComponentAttribute: PropertyAttribute, ISaintsAttribute, IPlayaAttribute, IPlayaArraySizeAttribute
    {
        public SaintsAttributeType AttributeType => SaintsAttributeType.Other;
        public string GroupBy { get; }

        // ReSharper disable once InconsistentNaming
        public readonly Type CompType;

        public GetComponentAttribute(Type compType = null, string groupBy = "")
        {
            CompType = compType;
            GroupBy = groupBy;
        }
    }
}
