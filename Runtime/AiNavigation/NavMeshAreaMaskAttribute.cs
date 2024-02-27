﻿using UnityEngine;

namespace SaintsField.AiNavigation
{
#if SAINTSFIELD_AI_NAVIGATION
    public class NavMeshAreaMaskAttribute: PropertyAttribute, ISaintsAttribute
    {
        public SaintsAttributeType AttributeType => SaintsAttributeType.Field;
        public string GroupBy { get; }

        public NavMeshAreaMaskAttribute(string groupBy = "")
        {
            GroupBy = groupBy;
        }
    }
#endif
}
