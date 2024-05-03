﻿#if SAINTSFIELD_ADDRESSABLE && !SAINTSFIELD_ADDRESSABLE_DISABLE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SaintsField.Addressable;
using SaintsField.Editor.Core;
using SaintsField.Editor.Utils;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.UIElements;

namespace SaintsField.Editor.Drawers.Addressable
{
    [CustomPropertyDrawer(typeof(AddressableAddressAttribute))]
    public class AddressableAddressAttributeDrawer: SaintsPropertyDrawer
    {
        #region IMGUI

        private string _error = "";

        // private IReadOnlyList<string> _targetKeys;

        protected override float GetFieldHeight(SerializedProperty property, GUIContent label,
            ISaintsAttribute saintsAttribute,
            FieldInfo info,
            bool hasLabelWidth, object parent) =>
            EditorGUIUtility.singleLineHeight;

        protected override void DrawField(Rect position, SerializedProperty property, GUIContent label,
            ISaintsAttribute saintsAttribute, OnGUIPayload onGUIPayload, FieldInfo info, object parent)
        {
            AddressableAddressAttribute addressableAddressAttribute = (AddressableAddressAttribute)saintsAttribute;

            (string error, IReadOnlyList<string> keys) = SetupAssetGroup(addressableAddressAttribute);

            _error = error;
            // _targetKeys = keys;
            if (_error != "")
            {
                DefaultDrawer(position, property, label, info);
                return;
            }

            int index = Util.ListIndexOfAction(keys, each => each == property.stringValue);

            using(new EditorGUI.PropertyScope(position, label, property))
            {
                EditorGUI.BeginChangeCheck();
                int newIndex = EditorGUI.Popup(position, label, index, keys.Select(each => new GUIContent(each.Replace('/', '\u2215').Replace('&', '＆'))).ToArray());
                if (EditorGUI.EndChangeCheck())
                {
                    property.stringValue = keys[newIndex];
                }
            }
        }

        protected override bool WillDrawBelow(SerializedProperty property, ISaintsAttribute saintsAttribute,
            FieldInfo info,
            object parent)
        {
            AddressableAddressAttribute addressableAddressAttribute = (AddressableAddressAttribute)saintsAttribute;
            (string error, IReadOnlyList<string> _) = SetupAssetGroup(addressableAddressAttribute);
            _error = error;
            return _error != "";
        }

        protected override float GetBelowExtraHeight(SerializedProperty property, GUIContent label,
            float width,
            ISaintsAttribute saintsAttribute, FieldInfo info, object parent)
        {
            AddressableAddressAttribute addressableAddressAttribute = (AddressableAddressAttribute)saintsAttribute;
            (string error, IReadOnlyList<string> _) = SetupAssetGroup(addressableAddressAttribute);
            _error = error;

            return _error == "" ? 0 : ImGuiHelpBox.GetHeight(_error, width, MessageType.Error);
        }

        protected override Rect DrawBelow(Rect position, SerializedProperty property,
            GUIContent label, ISaintsAttribute saintsAttribute, FieldInfo info, object parent) => _error == "" ? position : ImGuiHelpBox.Draw(position, _error, MessageType.Error);

        private static (string error, IReadOnlyList<string> assetGroups) SetupAssetGroup(AddressableAddressAttribute addressableAddressAttribute)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            if(settings == null)
            {
                return ("Addressable has not settings created yet", Array.Empty<string>());
            }

            // AddressableAssetGroup[] targetGroups;
            IReadOnlyList<AddressableAssetGroup> assetGroups = string.IsNullOrEmpty(addressableAddressAttribute.Group)
                ? settings.groups.ToArray()
                : settings.groups.Where(each => each.name == addressableAddressAttribute.Group).ToArray();

            string[][] labelFilters = addressableAddressAttribute.LabelFilters;

            IEnumerable<AddressableAssetEntry> entries = assetGroups.SelectMany(each => each.entries);

            if (labelFilters != null && labelFilters.Length > 0)
            {
                entries = entries.Where(eachName =>
                {
                    HashSet<string> labels = eachName.labels;
                    return labelFilters.Any(eachOr => eachOr.All(eachAnd => labels.Contains(eachAnd)));
                    // Debug.Log($"{eachName.address} {match}: {string.Join(",", labels)}");
                });
            }

            IReadOnlyList<string> keys = entries
                .Select(each => each.address)
                .ToList();

            return ("", keys);
        }
        #endregion

        #region UIToolkit

        private static string NameDropdownField(SerializedProperty property) => $"{property.propertyPath}__AddressableAddress_DropdownField";
        private static string NameHelpBox(SerializedProperty property) => $"{property.propertyPath}__AddressableAddress_HelpBox";

        protected override VisualElement CreateFieldUIToolKit(SerializedProperty property,
            ISaintsAttribute saintsAttribute, VisualElement container1, FieldInfo info, object parent)
        {
            UIToolkitUtils.DropdownButtonField dropdownButtonField = UIToolkitUtils.MakeDropdownButtonUIToolkit(property.displayName);
            dropdownButtonField.name = NameDropdownField(property);
            dropdownButtonField.userData = Array.Empty<string>();
            // dropdownButtonField.buttonLabelElement.text = property.stringValue == null ? "-" : property.stringValue;
            // ReSharper disable once MergeConditionalExpression
            dropdownButtonField.buttonLabelElement.text = property.stringValue == null ? "-" : property.stringValue;

            dropdownButtonField.AddToClassList(ClassAllowDisable);
            return dropdownButtonField;
        }

        protected override VisualElement CreateBelowUIToolkit(SerializedProperty property,
            ISaintsAttribute saintsAttribute, int index, VisualElement container, FieldInfo info, object parent)
        {
            HelpBox helpBoxElement = new HelpBox("", HelpBoxMessageType.Error)
            {
                style =
                {
                    display = DisplayStyle.None,
                },
                name = NameHelpBox(property),
            };
            helpBoxElement.AddToClassList(ClassAllowDisable);
            return helpBoxElement;
        }

        protected override void OnAwakeUIToolkit(SerializedProperty property, ISaintsAttribute saintsAttribute,
            int index,
            VisualElement container, Action<object> onValueChangedCallback, FieldInfo info, object parent)
        {
            HelpBox helpBoxElement = container.Q<HelpBox>(NameHelpBox(property));
            UIToolkitUtils.DropdownButtonField dropdownField = container.Q<UIToolkitUtils.DropdownButtonField>(NameDropdownField(property));

            // container.RegisterCallback<GeometryChangedEvent>(_ => dropdownField.AlignLabelForce());

            AddressableAddressAttribute addressableAddressAttribute = (AddressableAddressAttribute)saintsAttribute;
            dropdownField.buttonElement.clicked += () => ShowDropdown(property, addressableAddressAttribute, dropdownField, helpBoxElement, info, parent, onValueChangedCallback);
            // dropdownField.RegisterValueChangedCallback(v =>
            // {
            //     if (v.newValue == null)  // Apparently, modify the field's label is also a "ValueChanged" event. Good job unity.
            //     {
            //         return;
            //     }
            //
            //     IReadOnlyList<string> curMetaInfo = (IReadOnlyList<string>) ((DropdownField) v.target).userData;
            //     // string selectedKey = curMetaInfo[dropdownField.index];
            //     int selectedIndex = Util.ListIndexOfAction(curMetaInfo, each => UnityFuckedUpDropdownStringEscape(each) == v.newValue);
            //     if (selectedIndex == -1)
            //     {
            //         Debug.LogError($"failed to find {v.newValue} in {string.Join(",", curMetaInfo)}");
            //         if (curMetaInfo.Count > 0)
            //         {
            //             selectedIndex = 0;
            //         }
            //         else
            //         {
            //             return;
            //         }
            //     }
            //
            //     string newValue = curMetaInfo[selectedIndex];
            //     // Debug.Log($"select {newValue}");
            //     property.stringValue = newValue;
            //     property.serializedObject.ApplyModifiedProperties();
            //     onValueChangedCallback.Invoke(newValue);
            // });
        }

        private void ShowDropdown(SerializedProperty property, AddressableAddressAttribute addressableAddressAttribute, UIToolkitUtils.DropdownButtonField dropdownField, HelpBox helpBox, FieldInfo info, object parent, Action<object> onValueChangedCallback)
        {
            (string error, IReadOnlyList<string> keys) = SetupAssetGroup(addressableAddressAttribute);

            UpdateHelpBox(helpBox, error);

            GenericDropdownMenu genericDropdownMenu = new GenericDropdownMenu();
            foreach (string key in keys)
            {
                string thisKey = key;
                genericDropdownMenu.AddItem(key, property.stringValue == thisKey, () =>
                {
                    // dropdownField.buttonLabelElement.text = thisKey;
                    dropdownField.buttonLabelElement.text = thisKey;
                    property.stringValue = thisKey;
                    property.serializedObject.ApplyModifiedProperties();
                    onValueChangedCallback.Invoke(thisKey);
                });
            }

            if (keys.Count > 0)
            {
                genericDropdownMenu.AddSeparator("");
            }

            genericDropdownMenu.AddItem("Edit Addressable Group...", false, () =>
            {
                EditorApplication.ExecuteMenuItem("Window/Asset Management/Addressables/Groups");
            });

            genericDropdownMenu.DropDown(dropdownField.buttonElement.worldBound, dropdownField, true);
        }

        private static void UpdateHelpBox(HelpBox helpBox, string error)
        {
            if (helpBox.text == error)
            {
                return;
            }

            helpBox.style.display = error == "" ? DisplayStyle.None : DisplayStyle.Flex;
            helpBox.text = error;
        }

        // protected override void OnUpdateUIToolkit(SerializedProperty property, ISaintsAttribute saintsAttribute,
        //     int index,
        //     VisualElement container, Action<object> onValueChangedCallback, FieldInfo info, object parent)
        // {
        //     (string error, IReadOnlyList<string> keys) = SetupAssetGroup((AddressableAddressAttribute) saintsAttribute);
        //     UIToolkitUtils.DropdownButtonField dropdownField = container.Q<UIToolkitUtils.DropdownButtonField>(NameDropdownField(property));
        //
        //     IReadOnlyList<string> curKeys = (IReadOnlyList<string>) dropdownField.userData;
        //
        //     if(!curKeys.SequenceEqual(keys))
        //     {
        //         dropdownField.userData = keys;
        //         dropdownField.choices = keys.Select(UnityFuckedUpDropdownStringEscape).ToList();
        //         // int curSelect = Util.ListIndexOfAction(keys, each => each == property.stringValue);
        //         // dropdownField.index = curSelect;
        //         dropdownField.SetValueWithoutNotify(UnityFuckedUpDropdownStringEscape(property.stringValue));
        //     }
        //
        //     // Debug.Log($"AnimatorStateAttributeDrawer: {newAnimatorStates}");
        //     HelpBox helpBoxElement = container.Q<HelpBox>(NameHelpBox(property));
        //     // ReSharper disable once InvertIf
        //     if (error != helpBoxElement.text)
        //     {
        //         helpBoxElement.style.display = error == "" ? DisplayStyle.None : DisplayStyle.Flex;
        //         helpBoxElement.text = error;
        //     }
        // }

        // protected override void ChangeFieldLabelToUIToolkit(SerializedProperty property,
        //     ISaintsAttribute saintsAttribute, int index, VisualElement container, string labelOrNull,
        //     IReadOnlyList<RichTextDrawer.RichTextChunk> richTextChunks, bool tried, RichTextDrawer richTextDrawer)
        // {
        //     DropdownField dropdownField = container.Q<DropdownField>(NameDropdownField(property));
        //     dropdownField.label = labelOrNull;
        // }

        private static string UnityFuckedUpDropdownStringEscape(string value) =>
            // value.Replace('/', '\u2215').Replace('&', '＆');
            value;
        // private static string UnityFuckedUpDropdownStringReverse(string value) =>
        //     value.Replace('/', '\u2215').Replace('&', '＆');

        #endregion
    }
}
#endif
