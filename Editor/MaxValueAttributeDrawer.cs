﻿using System;
using System.Linq;
using System.Reflection;
using SaintsField.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace SaintsField.Editor
{
    [CustomPropertyDrawer(typeof(MaxValueAttribute))]
    public class MaxValueAttributeDrawer : SaintsPropertyDrawer
    {
        private string _error = "";

        protected override bool DrawPostField(Rect position, SerializedProperty property, GUIContent label,
            ISaintsAttribute saintsAttribute, bool valueChanged)
        {
            if (!valueChanged)
            {
                return true;
            }

            if (property.propertyType == SerializedPropertyType.Float)
            {
                float curValue = property.floatValue;
                MaxValueAttribute maxValueAttribute = (MaxValueAttribute)saintsAttribute;
                float valueLimit;
                if (maxValueAttribute.ValueCallback == null)
                {
                    valueLimit = maxValueAttribute.Value;
                }
                else
                {
                    (float getValueLimit, string getError) = Util.GetCallbackFloat(property, maxValueAttribute.ValueCallback);
                    valueLimit = getValueLimit;
                    _error = getError;
                }

                if (_error != "")
                {
                    return true;
                }

                if (valueLimit < curValue)
                {
                    property.floatValue = valueLimit;
                }
            }
            else if (property.propertyType == SerializedPropertyType.Integer)
            {
                int curValue = property.intValue;
                MaxValueAttribute maxValueAttribute = (MaxValueAttribute)saintsAttribute;
                float valueLimit;
                if (maxValueAttribute.ValueCallback == null)
                {
                    valueLimit = maxValueAttribute.Value;
                }
                else
                {
                    (float getValueLimit, string getError) = Util.GetCallbackFloat(property, maxValueAttribute.ValueCallback);
                    valueLimit = getValueLimit;
                    _error = getError;
                }

                if (_error != "")
                {
                    return true;
                }

                if (valueLimit < curValue)
                {
                    property.intValue = (int)valueLimit;
                }
            }
            return true;
        }

        protected override bool WillDrawBelow(Rect position, SerializedProperty property, GUIContent label, ISaintsAttribute saintsAttribute) => _error != "";

        protected override float GetBelowExtraHeight(SerializedProperty property, GUIContent label, float width, ISaintsAttribute saintsAttribute) => _error == "" ? 0 : HelpBox.GetHeight(_error, width, MessageType.Error);

        protected override Rect DrawBelow(Rect position, SerializedProperty property, GUIContent label, ISaintsAttribute saintsAttribute) => _error == "" ? position : HelpBox.Draw(position, _error, MessageType.Error);
    }
}