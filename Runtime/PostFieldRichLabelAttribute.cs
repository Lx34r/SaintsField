﻿using System;
using UnityEngine;

namespace SaintsField
{
    [AttributeUsage(AttributeTargets.Field)]
    public class PostFieldRichLabelAttribute: RichLabelAttribute
    {
        public override SaintsAttributeType AttributeType => SaintsAttributeType.Other;
        public string GroupBy { get; }

        public readonly float Padding;

        public PostFieldRichLabelAttribute(string richTextXml, bool isCallback=false, float padding=5f, string groupBy=""): base(richTextXml, isCallback)
        {
            Padding = padding;
            GroupBy = groupBy;
        }
    }
}
