﻿using System;
using UnityEngine;

namespace SaintsField
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class AssetPreviewAttribute: PropertyAttribute, ISaintsAttribute
    {
        public SaintsAttributeType AttributeType => SaintsAttributeType.Other;
        public string GroupBy { get; }

        public readonly bool Above;
        public readonly int MaxWidth;
        public readonly int MaxHeight;
        public readonly EAlign Align;

        public AssetPreviewAttribute(int maxWidth=-1, int maxHeight=-1, EAlign align=EAlign.End, bool above=false, string groupBy="")
        {
            GroupBy = groupBy;

            Above = above;
            MaxWidth = maxWidth;
            MaxHeight = maxHeight;
            Align = align;
        }
    }
}
