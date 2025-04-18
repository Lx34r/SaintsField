﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SaintsField.Editor.Linq;
using SaintsField.Editor.Playa;
using SaintsField.Editor.Playa.Renderer;
using SaintsField.Editor.Playa.Renderer.BaseRenderer;
using SaintsField.Editor.Playa.Renderer.SpecialRenderer.ListDrawerSettings;
using SaintsField.Editor.Playa.Renderer.SpecialRenderer.Table;
using SaintsField.Editor.Playa.RendererGroup;
using SaintsField.Editor.Utils;
using SaintsField.Playa;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_2021_3_OR_NEWER && !SAINTSFIELD_UI_TOOLKIT_DISABLE
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#endif
#if DOTWEEN && !SAINTSFIELD_DOTWEEN_DISABLED
using DG.DOTweenEditor;
#endif


namespace SaintsField.Editor
{
    public partial class SaintsEditor: UnityEditor.Editor, IDOTweenPlayRecorder, IMakeRenderer
    {
        private static bool _saintsEditorIMGUI = true;

        // private MonoScript _monoScript;
        // private List<SaintsFieldWithInfo> _fieldWithInfos = new List<SaintsFieldWithInfo>();

        [NonSerialized]
        public bool EditorShowMonoScript = true;

#if DOTWEEN && !SAINTSFIELD_DOTWEEN_DISABLED
        private static readonly HashSet<IDOTweenPlayRecorder> AliveInstances = new HashSet<IDOTweenPlayRecorder>();
        public static void RemoveInstance(IDOTweenPlayRecorder doTweenPlayRecorder)
        {
            AliveInstances.Remove(doTweenPlayRecorder);
            if (AliveInstances.Count == 0)
            {
                DOTweenEditorPreview.Stop();
            }
        }
        public static void AddInstance(IDOTweenPlayRecorder doTweenPlayRecorder)
        {
            AliveInstances.Add(doTweenPlayRecorder);
        }
#endif

        // private Dictionary<string, ISaintsRendererGroup> _layoutKeyToGroup;
        private IReadOnlyList<ISaintsRenderer> _renderers;

        public static MonoScript GetMonoScript(UnityEngine.Object target)
        {
            try
            {
                return MonoScript.FromMonoBehaviour((MonoBehaviour) target);
            }
            catch (Exception)
            {
                try
                {
                    return MonoScript.FromScriptableObject((ScriptableObject)target);
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public static IReadOnlyList<ISaintsRenderer> Setup(ICollection<string> skipSerializedFields, SerializedObject serializedObject, IMakeRenderer makeRenderer,
            object target)
        {
            string[] serializableFields = GetSerializedProperties(serializedObject).ToArray();
            // Debug.Log($"serializableFields={string.Join(",", serializableFields)}");
            Dictionary<string, SerializedProperty> serializedPropertyDict = serializableFields
                .Where(each => !skipSerializedFields.Contains(each))
                .ToDictionary(each => each, serializedObject.FindProperty);
            // Debug.Log($"serializedPropertyDict.Count={serializedPropertyDict.Count}");
            return HelperGetRenderers(serializedPropertyDict, serializedObject, makeRenderer, target);
        }

        public static IEnumerable<SaintsFieldWithInfo> HelperGetSaintsFieldWithInfo(
            IReadOnlyDictionary<string, SerializedProperty> serializedPropertyDict,
            object target)
        {
            List<SaintsFieldWithInfo> fieldWithInfos = new List<SaintsFieldWithInfo>();
            List<Type> types = ReflectUtils.GetSelfAndBaseTypes(target);
            types.Reverse();

            // Dictionary<string, SerializedProperty> pendingSerializedProperties = new Dictionary<string, SerializedProperty>(serializedPropertyDict);
            Dictionary<string, SerializedProperty> pendingSerializedProperties = serializedPropertyDict.ToDictionary(each => each.Key, each => each.Value);
            pendingSerializedProperties.Remove("m_Script");

            // base type -> this type
            // a later field should override current in different depth
            // but, if the field is not in the same depth, it should be added (method override)
            // Yep, C# is a crap
            for (int inherentDepth = 0; inherentDepth < types.Count; inherentDepth++)
            {
                Type systemType = types[inherentDepth];

                // as we can not get the correct order, we'll make it order as: field(serialized+nonSerialized), property, method
                List<SaintsFieldWithInfo> thisDepthInfos = new List<SaintsFieldWithInfo>();
                List<string> memberDepthIds = new List<string>();

                foreach (MemberInfo memberInfo in systemType
                             .GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic |
                                         BindingFlags.Public | BindingFlags.DeclaredOnly)
                             .OrderBy(memberInfo => memberInfo.MetadataToken))  // this is still not the correct order, but... a bit better
                {
                    // Debug.Log(memberInfo.Name);
                    IReadOnlyList<IPlayaAttribute> playaAttributes = ReflectCache.GetCustomAttributes<IPlayaAttribute>(memberInfo);

                    ISaintsLayoutBase[] layoutBases = GetLayoutBases(playaAttributes.OfType<ISaintsLayoutBase>()).ToArray();

                    switch (memberInfo)
                    {
                        case FieldInfo fieldInfo:
                        {
                            #region SerializedField

                            if (serializedPropertyDict.ContainsKey(fieldInfo.Name))
                            {
                                // Debug.Log($"Name            : {fieldInfo.Name}");
                                // Debug.Log($"Declaring Type  : {fieldInfo.DeclaringType}");
                                // Debug.Log($"IsPublic        : {fieldInfo.IsPublic}");
                                // Debug.Log($"MemberType      : {fieldInfo.MemberType}");
                                // Debug.Log($"FieldType       : {fieldInfo.FieldType}");
                                // Debug.Log($"IsFamily        : {fieldInfo.IsFamily}");

                                OrderedAttribute orderProp =
                                    playaAttributes.OfType<OrderedAttribute>().FirstOrDefault();
                                int order = orderProp?.Order ?? int.MinValue;

                                // Debug.Log($"{fieldInfo.Name}/{string.Join(",", pendingSerializedProperties.Keys)}");
                                thisDepthInfos.Add(new SaintsFieldWithInfo
                                {
                                    PlayaAttributes = playaAttributes,
                                    LayoutBases = layoutBases,
                                    Target = target,

                                    RenderType = SaintsRenderType.SerializedField,
                                    SerializedProperty = serializedPropertyDict[fieldInfo.Name],
                                    MemberId = fieldInfo.Name,
                                    FieldInfo = fieldInfo,
                                    InherentDepth = inherentDepth,
                                    Order = order,
                                    // serializable = true,
                                });
                                memberDepthIds.Add(fieldInfo.Name);
                                // Debug.Log($"remove key {fieldInfo.Name}");
                                pendingSerializedProperties.Remove(fieldInfo.Name);
                            }
                            #endregion

                            #region nonSerFieldInfo

                            else if (playaAttributes.Count > 0)
                            {
                                OrderedAttribute orderProp = playaAttributes.OfType<OrderedAttribute>().FirstOrDefault();
                                int order = orderProp?.Order ?? int.MinValue;
                                thisDepthInfos.Add(new SaintsFieldWithInfo
                                {
                                    PlayaAttributes = playaAttributes,
                                    LayoutBases = layoutBases,
                                    Target = target,

                                    RenderType = SaintsRenderType.NonSerializedField,
                                    // memberType = nonSerFieldInfo.MemberType,
                                    MemberId = fieldInfo.Name,
                                    FieldInfo = fieldInfo,
                                    InherentDepth = inherentDepth,
                                    Order = order,
                                    // serializable = false,
                                });
                                memberDepthIds.Add(fieldInfo.Name);
                            }
                            #endregion
                        }
                            break;
                        case PropertyInfo propertyInfo:
                        {
                            #region NativeProperty
                            if(playaAttributes.Count > 0)
                            {
                                OrderedAttribute orderProp =
                                    playaAttributes.OfType<OrderedAttribute>().FirstOrDefault();
                                int order = orderProp?.Order ?? int.MinValue;
                                thisDepthInfos.Add(new SaintsFieldWithInfo
                                {
                                    PlayaAttributes = playaAttributes,
                                    LayoutBases = layoutBases,
                                    Target = target,

                                    RenderType = SaintsRenderType.NativeProperty,
                                    MemberId = propertyInfo.Name,
                                    PropertyInfo = propertyInfo,
                                    InherentDepth = inherentDepth,
                                    Order = order,
                                });
                                memberDepthIds.Add(propertyInfo.Name);
                            }
                            #endregion
                        }
                            break;
                        case MethodInfo methodInfo:
                        {
                            #region Method
                            // method attributes will be collected no matter what, because DOTweenPlayGroup depending on it even
                            // it has no attribute at all

                            // Attribute[] allMethodAttributes = methodInfo.GetCustomAttributes<Attribute>().ToArray();

                            OrderedAttribute orderProp =
                                playaAttributes.FirstOrDefault(each => each is OrderedAttribute) as OrderedAttribute;
                            int order = orderProp?.Order ?? int.MinValue;

                            // wrong: inspector does not care about inherited/new method. It just needs to use the last one
                            // right: we support method override now
                            // fieldWithInfos.RemoveAll(each => each.InherentDepth < inherentDepth && each.RenderType == SaintsRenderType.Method && each.MethodInfo.Name == methodInfo.Name);
                            // methodInfos.RemoveAll(each => each.InherentDepth < inherentDepth && each.RenderType == SaintsRenderType.Method && each.MethodInfo.Name == methodInfo.Name);

#if SAINTSFIELD_DEBUG && SAINTSFIELD_DEBUG_SAINTS_EDITOR_METHOD
                            Debug.Log($"[{systemType}] method: {methodInfo.Name}");
#endif

                            string buttonExtraId = string.Join(":", methodInfo.GetParameters()
                                    .Select(each => each.ParameterType)
                                    .Append(methodInfo.ReturnType)
                                    .Select(each => each.FullName));

                            string buttonId = $"{methodInfo.Name}.{buttonExtraId}";

                            thisDepthInfos.Add(new SaintsFieldWithInfo
                            {
                                PlayaAttributes = playaAttributes,
                                LayoutBases = layoutBases,
                                Target = target,

                                // memberType = MemberTypes.Method,
                                RenderType = SaintsRenderType.Method,
                                MemberId = buttonId,
                                MethodInfo = methodInfo,
                                InherentDepth = inherentDepth,
                                Order = order,
                            });
                            memberDepthIds.Add(buttonId);
                            #endregion
                        }
                            break;
                    }
                }

                // now handle overrides
                fieldWithInfos.RemoveAll(each => memberDepthIds.Contains(each.MemberId));

                fieldWithInfos.AddRange(thisDepthInfos);
                // fieldWithInfos.AddRange(fieldInfos);
                // fieldWithInfos.AddRange(propertyInfos);
                // fieldWithInfos.AddRange(methodInfos);
            }

            if (pendingSerializedProperties.Count > 0)
            {
                // we got unused serialized properties because Unity directly inject them rather than using a
                // normal workflow
                foreach (KeyValuePair<string, SerializedProperty> pendingSer in pendingSerializedProperties.Reverse())
                {
                    fieldWithInfos.Insert(0, new SaintsFieldWithInfo
                    {
                        PlayaAttributes = Array.Empty<IPlayaAttribute>(),
                        LayoutBases = Array.Empty<ISaintsLayoutBase>(),
                        Target = target,

                        RenderType = SaintsRenderType.InjectedSerializedField,
                        SerializedProperty = pendingSer.Value,
                        FieldInfo = null,
                        InherentDepth = types.Count - 1,
                        Order = int.MinValue,
                        // serializable = true,
                    });
                }
            }

            return fieldWithInfos
                .WithIndex()
                .OrderBy(each => each.value.InherentDepth)
                .ThenBy(each => each.value.Order)
                .ThenBy(each => each.index)
                .Select(each => each.value);
        }

        private static IEnumerable<ISaintsLayoutBase> GetLayoutBases(IEnumerable<ISaintsLayoutBase> layoutBases)
        {
            foreach (ISaintsLayoutBase saintsLayoutBase in layoutBases)
            {
                switch (saintsLayoutBase)
                {
                    case LayoutTerminateHereAttribute layoutTerminateHereAttribute:
                        yield return new LayoutAttribute(".", layoutTerminateHereAttribute.Layout, false, layoutTerminateHereAttribute.MarginTop, layoutTerminateHereAttribute.MarginBottom);
                        yield return new LayoutEndAttribute(null, layoutTerminateHereAttribute.MarginTop, layoutTerminateHereAttribute.MarginBottom);
                        break;
                    case LayoutCloseHereAttribute layoutCloseHereAttribute:
                        yield return new LayoutAttribute(".", layoutCloseHereAttribute.Layout, false, layoutCloseHereAttribute.MarginTop, layoutCloseHereAttribute.MarginBottom);
                        yield return new LayoutEndAttribute(".", layoutCloseHereAttribute.MarginTop, layoutCloseHereAttribute.MarginBottom);
                        break;
                    default:
                        yield return saintsLayoutBase;
                        break;
                }
            }
        }

        public static IReadOnlyList<ISaintsRenderer> HelperGetRenderers(
            IReadOnlyDictionary<string, SerializedProperty> serializedPropertyDict, SerializedObject serializedObject,
            IMakeRenderer makeRenderer,
            object target)
        {
            IReadOnlyList<SaintsFieldWithInfo> fieldWithInfosSorted = HelperGetSaintsFieldWithInfo(serializedPropertyDict, target).ToArray();
            IReadOnlyList<RendererGroupInfo> chainedGroups = ChainSaintsFieldWithInfo(fieldWithInfosSorted);
            return HelperFlattenRendererGroupInfoIntoRenderers(chainedGroups, serializedObject, makeRenderer, target).Select(each => each.saintsRenderer).ToArray();
        }

        private static IEnumerable<(string absGroupBy, ISaintsRenderer saintsRenderer)> HelperFlattenRendererGroupInfoIntoRenderers(IReadOnlyList<RendererGroupInfo> chainedGroups, SerializedObject serializedObject, IMakeRenderer makeRenderer, object target)
        {
            foreach (RendererGroupInfo rendererGroupInfo in chainedGroups)
            {
                bool isEndNode = rendererGroupInfo.AbsGroupBy == "" && rendererGroupInfo.Children.Count == 0;

                if (isEndNode)
                {
                    AbsRenderer result = makeRenderer.MakeRenderer(serializedObject, rendererGroupInfo.FieldWithInfo);
                    if(result != null)
                    {
#if SAINTSFIELD_DEBUG && SAINTSFIELD_DEBUG_SAINTS_EDITOR_LAYOUT
                        if(rendererGroupInfo.FieldWithInfo.MethodInfo == null)
                        {
                            Debug.Log($"Flatten EndNode return {result}");
                        }
#endif
                        yield return ("", result);
                    }
                }
                else
                {
                    (string absGroupBy, ISaintsRenderer saintsRenderer)[] children = HelperFlattenRendererGroupInfoIntoRenderers(rendererGroupInfo.Children, serializedObject, makeRenderer, target).ToArray();
                    if (children.Length > 0)
                    {

                        string curGroupAbs = rendererGroupInfo.AbsGroupBy;

                        ISaintsRendererGroup group =
#if DOTWEEN && !SAINTSFIELD_DOTWEEN_DISABLED
                            rendererGroupInfo.Config.IsDoTween
                                // ReSharper disable once RedundantCast
                                ? (ISaintsRendererGroup)new DOTweenPlayGroup(target)
                                : new SaintsRendererGroup(curGroupAbs, rendererGroupInfo.Config, target)
#else
                            new SaintsRendererGroup(curGroupAbs, rendererGroupInfo.Config, target)
#endif
                        ;

                        foreach ((string eachChildGroupBy, ISaintsRenderer eachChild) in children)
                        {
#if SAINTSFIELD_DEBUG && SAINTSFIELD_DEBUG_SAINTS_EDITOR_LAYOUT
                            Debug.Log($"Flatten {group} add renderer {eachChild}");
#endif

                            group.Add(eachChildGroupBy, eachChild);
                        }

#if SAINTSFIELD_DEBUG && SAINTSFIELD_DEBUG_SAINTS_EDITOR_LAYOUT
                        Debug.Log($"Flatten {group} return with {children.Length} children");
#endif

                        yield return (rendererGroupInfo.AbsGroupBy, group);
                    }
#if SAINTSFIELD_DEBUG && SAINTSFIELD_DEBUG_SAINTS_EDITOR_LAYOUT
                    else
                    {
                        Debug.Log($"Flatten {rendererGroupInfo.AbsGroupBy} empty children, skip");
                    }
#endif
                }
            }
        }

        private class RendererGroupInfo {
            public string AbsGroupBy;  // ""=normal fields, other=grouped fields
            public List<RendererGroupInfo> Children;
            public SaintsRendererGroup.Config Config;
            public SaintsFieldWithInfo FieldWithInfo;
        }

        private static IReadOnlyList<RendererGroupInfo> ChainSaintsFieldWithInfo(IReadOnlyList<SaintsFieldWithInfo> fieldWithInfosSorted)
        {
            List<RendererGroupInfo> rendererGroupInfos = new List<RendererGroupInfo>();
            Dictionary<string, RendererGroupInfo> rootToRendererGroupInfo =
                new Dictionary<string, RendererGroupInfo>();

            RendererGroupInfo keepGroupingInfo = null;
            int inherent = -1;
            foreach (SaintsFieldWithInfo saintsFieldWithInfo in fieldWithInfosSorted)
            {
                bool isNewInherent = saintsFieldWithInfo.InherentDepth != inherent;
                inherent = saintsFieldWithInfo.InherentDepth;

                IReadOnlyList<ISaintsLayoutBase> layoutBases = saintsFieldWithInfo.LayoutBases;
                IReadOnlyList<ISaintsLayout> layouts = layoutBases.OfType<ISaintsLayout>().ToArray();
                RendererGroupInfo lastGroupInfo = null;

                if (isNewInherent)
                {
                    keepGroupingInfo = null;
                    lastGroupInfo = null;
                }

                if (layouts.Count > 0)
                {
                    string preAbsGroupBy = null;
                    List<ISaintsLayoutToggle> layoutToggles = new List<ISaintsLayoutToggle>();

                    foreach (ISaintsLayoutBase layoutBase in layoutBases)
                    {
#if SAINTSFIELD_DEBUG && SAINTSFIELD_DEBUG_SAINTS_EDITOR_LAYOUT
                        Debug.Log($"Layout processing {layoutBase}");
#endif

                        switch (layoutBase)
                        {
                            case ISaintsLayoutToggle layoutToggle:
                                layoutToggles.Add(layoutToggle);
                                break;
                            case LayoutEndAttribute layoutEndAttribute:
                            {
                                // does not work with toggles, just clear it
                                if(layoutToggles.Count > 0)
                                {
                                    Debug.LogWarning($"layout toggles does not work with LayoutEnd. Please adjust the order of the attributes. ({string.Join(", ", layoutToggles)})");
                                    layoutToggles.Clear();
                                }

                                string endGroupBy = layoutEndAttribute.LayoutBy;
                                if (endGroupBy == null)
                                {
                                    keepGroupingInfo = null;
#if SAINTSFIELD_DEBUG && SAINTSFIELD_DEBUG_SAINTS_EDITOR_LAYOUT
                                    Debug.Log($"Layout close null");
#endif
                                }
                                else if (keepGroupingInfo == null)
                                {
                                    // do nothing. End a layout when it's not in a layout is meaningless

#if SAINTSFIELD_DEBUG && SAINTSFIELD_DEBUG_SAINTS_EDITOR_LAYOUT
                                    Debug.Log($"Layout close with no scoop inside");
#endif
                                }
                                else
                                {
                                    if (endGroupBy.StartsWith("."))
                                    {
                                        string closeGroup = JoinGroupBy(keepGroupingInfo.AbsGroupBy, endGroupBy);
                                        if(closeGroup.Contains('/'))
                                        {
                                            List<string> splitCloseGroup = closeGroup.Split('/').ToList();
                                            splitCloseGroup.RemoveAt(splitCloseGroup.Count - 1);
                                            string openGroupTo = string.Join("/", splitCloseGroup);
                                            if (!rootToRendererGroupInfo.TryGetValue(openGroupTo,
                                                    out RendererGroupInfo info))
                                            {
                                                rootToRendererGroupInfo[openGroupTo] = info = new RendererGroupInfo
                                                {
                                                    AbsGroupBy = openGroupTo,
                                                    Children = new List<RendererGroupInfo>(),
                                                    Config = new SaintsRendererGroup.Config(),
                                                };
                                            }

                                            keepGroupingInfo = info.Config.KeepGrouping ? info : null;
#if SAINTSFIELD_DEBUG && SAINTSFIELD_DEBUG_SAINTS_EDITOR_LAYOUT
                                            Debug.Log($"Layout close, {closeGroup}->{openGroupTo}: {keepGroupingInfo?.AbsGroupBy}");
#endif
                                        }
                                        else
                                        {
#if SAINTSFIELD_DEBUG && SAINTSFIELD_DEBUG_SAINTS_EDITOR_LAYOUT
                                            Debug.Log($"Layout close, {closeGroup}: null");
#endif
                                            keepGroupingInfo = null;
                                        }
                                    }
                                    else
                                    {
                                        string parentGroupBy;
                                        if (endGroupBy.Contains('/'))
                                        {
                                            List<string> endGroupBySplit = endGroupBy.Split('/').ToList();
                                            endGroupBySplit.RemoveAt(endGroupBySplit.Count - 1);
                                            parentGroupBy = string.Join("/", endGroupBySplit);
                                        }
                                        else
                                        {
                                            parentGroupBy = "";
                                        }
                                        if (parentGroupBy != "" && rootToRendererGroupInfo.TryGetValue(parentGroupBy,
                                                out RendererGroupInfo info))
                                        {
                                            keepGroupingInfo = info.Config.KeepGrouping
                                                ? info
                                                : null;
#if SAINTSFIELD_DEBUG && SAINTSFIELD_DEBUG_SAINTS_EDITOR_LAYOUT
                                            Debug.Log($"Layout close, {endGroupBy}->{parentGroupBy}: {keepGroupingInfo?.AbsGroupBy}");
#endif
                                        }
                                        else
                                        {
#if SAINTSFIELD_DEBUG && SAINTSFIELD_DEBUG_SAINTS_EDITOR_LAYOUT
                                            Debug.Log($"Layout close, {endGroupBy}: null");
#endif
                                            keepGroupingInfo = null;
                                        }
                                    }
                                }
                            }
                                break;

//                             case LayoutCloseHereAttribute layoutCloseHereAttribute:
//                             {
//                                 string groupBy = layoutCloseHereAttribute.LayoutBy;
//                                 string preGroupBy = keepGroupingInfo?.AbsGroupBy ?? preAbsGroupBy;
//                                 if(preGroupBy != null)
//                                 {
//                                     groupBy = JoinGroupBy(preGroupBy, groupBy);
//                                 }
//                                 preAbsGroupBy = groupBy;
//                                 // Debug.Log($"{saintsGroup}: {groupBy}({saintsGroup.LayoutBy})");
//
//                                 (bool newRoot, RendererGroupInfo targetGroup) = GetOrCreateGroupInfo(rootToRendererGroupInfo, groupBy);
//                                 if (newRoot)
//                                 {
//                                     rendererGroupInfos.Add(targetGroup);
//                                 }
//                                 lastGroupInfo = targetGroup;
//
//                                 SaintsRendererGroup.Config newConfig = new SaintsRendererGroup.Config
//                                 {
//                                     ELayout = layoutCloseHereAttribute.Layout,
//                                     IsDoTween = layoutCloseHereAttribute is DOTweenPlayAttribute,
//                                     MarginTop = layoutCloseHereAttribute.MarginTop,
//                                     MarginBottom = layoutCloseHereAttribute.MarginBottom,
//                                 };
//                                 SaintsRendererGroup.Config oldConfig = targetGroup.Config;
//                                 targetGroup.Config = new SaintsRendererGroup.Config
//                                 {
//                                     ELayout = newConfig.ELayout == 0? oldConfig.ELayout: newConfig.ELayout,
//                                     IsDoTween = oldConfig.IsDoTween || newConfig.IsDoTween,
//                                     MarginTop = newConfig.MarginTop >= 0? newConfig.MarginTop: oldConfig.MarginTop,
//                                     MarginBottom = newConfig.MarginBottom >= 0? newConfig.MarginBottom: oldConfig.MarginBottom,
//                                     KeepGrouping = layoutCloseHereAttribute.KeepGrouping,
//                                     Toggles = (oldConfig?.Toggles ?? Array.Empty<ISaintsLayoutToggle>()).Concat(layoutToggles).ToArray(),
//                                 };
//                                 layoutToggles.Clear();
//
//                                 if (targetGroup.Config.KeepGrouping)
//                                 {
//                                     keepGroupingInfo = targetGroup;
//                                 }
//                                 else if (keepGroupingInfo != null &&
//                                          targetGroup.AbsGroupBy != keepGroupingInfo.AbsGroupBy)
//                                 {
//                                     keepGroupingInfo = null;
//                                 }
//
//                                 if (layoutCloseHereAttribute.Ternimate)
//                                 {
//                                     keepGroupingInfo = null;
//                                 }
//                                 else if (keepGroupingInfo != null)
//                                 {
//                                     (bool _, RendererGroupInfo targetGroupUpper) = GetOrCreateGroupInfo(rootToRendererGroupInfo, JoinGroupBy(groupBy, ".."));
//                                     keepGroupingInfo = targetGroupUpper;
//                                 }
//
// #if SAINTSFIELD_DEBUG && SAINTSFIELD_DEBUG_SAINTS_EDITOR_LAYOUT
//                                 Debug.Log($"Layout item {groupBy}, newRoot={newRoot}, eLayout={targetGroup.Config.ELayout}, keepGroupingInfo={keepGroupingInfo?.AbsGroupBy}");
// #endif
//                             }
//                                 break;

                            case ISaintsLayout saintsGroup:
                            {
                                string groupBy = saintsGroup.LayoutBy;
                                if (groupBy.StartsWith("."))
                                {
                                    string preGroupBy = keepGroupingInfo?.AbsGroupBy ?? preAbsGroupBy;
                                    if(preGroupBy != null)
                                    {
                                        groupBy = JoinGroupBy(preGroupBy, groupBy);
                                    }
                                }
                                preAbsGroupBy = groupBy;
                                // Debug.Log($"{saintsGroup}: {groupBy}({saintsGroup.LayoutBy})");

                                (bool newRoot, RendererGroupInfo targetGroup) = GetOrCreateGroupInfo(rootToRendererGroupInfo, groupBy);
                                if (newRoot)
                                {
                                    rendererGroupInfos.Add(targetGroup);
                                }
                                lastGroupInfo = targetGroup;

                                SaintsRendererGroup.Config newConfig = new SaintsRendererGroup.Config
                                {
                                    ELayout = saintsGroup.Layout,
                                    IsDoTween = saintsGroup is DOTweenPlayAttribute,
                                    MarginTop = saintsGroup.MarginTop,
                                    MarginBottom = saintsGroup.MarginBottom,
                                };
                                SaintsRendererGroup.Config oldConfig = targetGroup.Config;
                                targetGroup.Config = new SaintsRendererGroup.Config
                                {
                                    ELayout = newConfig.ELayout == 0? oldConfig.ELayout: newConfig.ELayout,
                                    IsDoTween = oldConfig.IsDoTween || newConfig.IsDoTween,
                                    MarginTop = newConfig.MarginTop >= 0? newConfig.MarginTop: oldConfig.MarginTop,
                                    MarginBottom = newConfig.MarginBottom >= 0? newConfig.MarginBottom: oldConfig.MarginBottom,
                                    KeepGrouping = saintsGroup.KeepGrouping,
                                    Toggles = (oldConfig?.Toggles ?? Array.Empty<ISaintsLayoutToggle>()).Concat(layoutToggles).ToArray(),
                                };
                                layoutToggles.Clear();

                                if (targetGroup.Config.KeepGrouping)
                                {
                                    keepGroupingInfo = targetGroup;
                                }
                                else if (keepGroupingInfo != null &&
                                         targetGroup.AbsGroupBy != keepGroupingInfo.AbsGroupBy)
                                {
                                    keepGroupingInfo = null;
                                }

#if SAINTSFIELD_DEBUG && SAINTSFIELD_DEBUG_SAINTS_EDITOR_LAYOUT
                                Debug.Log($"Layout item {groupBy}, newRoot={newRoot}, eLayout={targetGroup.Config.ELayout}, keepGroupingInfo={keepGroupingInfo?.AbsGroupBy}");
#endif
                            }
                                break;
                        }
                    }
                }

                RendererGroupInfo endNode = new RendererGroupInfo
                {
                    AbsGroupBy = "",
                    Children = new List<RendererGroupInfo>(),
                    Config = new SaintsRendererGroup.Config(),
                    FieldWithInfo = saintsFieldWithInfo,
                };

                if (lastGroupInfo == null && keepGroupingInfo != null)
                {
#if SAINTSFIELD_DEBUG && SAINTSFIELD_DEBUG_SAINTS_EDITOR_LAYOUT
                    Debug.Log($"Layout lastGroupInfo set to keepGrouping: {keepGroupingInfo.AbsGroupBy}");
#endif
                    lastGroupInfo = keepGroupingInfo;
                }

                if (lastGroupInfo == null)
                {
#if SAINTSFIELD_DEBUG && SAINTSFIELD_DEBUG_SAINTS_EDITOR_LAYOUT
                    Debug.Log($"Layout add direct: {saintsFieldWithInfo.FieldInfo?.Name ?? saintsFieldWithInfo.PropertyInfo?.Name ?? saintsFieldWithInfo.MethodInfo?.Name}");
#endif
                    rendererGroupInfos.Add(endNode);
                }
                else
                {
#if SAINTSFIELD_DEBUG && SAINTSFIELD_DEBUG_SAINTS_EDITOR_LAYOUT
                    Debug.Log($"Layout add field under {lastGroupInfo.AbsGroupBy}: {saintsFieldWithInfo.FieldInfo?.Name ?? saintsFieldWithInfo.PropertyInfo?.Name ?? saintsFieldWithInfo.MethodInfo?.Name}");
#endif
                    lastGroupInfo.Children.Add(endNode);
                    // if (!lastGroupInfo.AbsGroupBy.Contains('/') && !rendererGroupInfos.Contains(lastGroupInfo))
                    // {
                    //     rendererGroupInfos.Add(lastGroupInfo);
                    // }
                }
            }

            return rendererGroupInfos;
        }

        private static (bool newRoot, RendererGroupInfo rendererGroupInfo) GetOrCreateGroupInfo(Dictionary<string, RendererGroupInfo> rootToRendererGroupInfo, string path)
        {
            bool newRoot = false;
            if (!path.Contains('/'))
            {
                if(!rootToRendererGroupInfo.TryGetValue(path, out RendererGroupInfo info))
                {
                    newRoot = true;
                    rootToRendererGroupInfo[path] = info = new RendererGroupInfo
                    {
                        AbsGroupBy = path,
                        Children = new List<RendererGroupInfo>(),
                        Config = new SaintsRendererGroup.Config(),
                    };
#if SAINTSFIELD_DEBUG && SAINTSFIELD_DEBUG_SAINTS_EDITOR_LAYOUT
                    Debug.Log($"Layout add node root default for {path}");
#endif
                }

                return (newRoot, info);
            }

            string[] groupByParts = path.Split('/');
            string rootGroup = groupByParts[0];
            if (!rootToRendererGroupInfo.TryGetValue(rootGroup, out RendererGroupInfo accInfo))
            {
                newRoot = true;
                rootToRendererGroupInfo[rootGroup] = accInfo = new RendererGroupInfo
                {
                    AbsGroupBy = rootGroup,
                    Children = new List<RendererGroupInfo>(),
                    Config = new SaintsRendererGroup.Config(),
                };


#if SAINTSFIELD_DEBUG && SAINTSFIELD_DEBUG_SAINTS_EDITOR_LAYOUT
                Debug.Log($"Layout add node root default for {rootGroup}");
#endif
            }

            string pathAcc = rootGroup;

            foreach (string part in groupByParts.Skip(1))
            {
                pathAcc += $"/{part}";
                RendererGroupInfo found = accInfo.Children.FirstOrDefault(each => each.AbsGroupBy == pathAcc);
                if (found == null)
                {
                    found = new RendererGroupInfo
                    {
                        AbsGroupBy = pathAcc,
                        Children = new List<RendererGroupInfo>(),
                        Config = new SaintsRendererGroup.Config(),
                    };
                    accInfo.Children.Add(found);

#if SAINTSFIELD_DEBUG && SAINTSFIELD_DEBUG_SAINTS_EDITOR_LAYOUT
                    Debug.Log($"Layout add node child default {pathAcc} under {accInfo.AbsGroupBy}");
#endif
                }
                accInfo = found;
            }

            return (newRoot, accInfo);
        }

        private static string JoinGroupBy(string layoutGroupByAcc, string curGroupBy)
        {
            List<string> ori = layoutGroupByAcc.Split('/').ToList();

            foreach (string eachPart in curGroupBy.Split('/'))
            {
                switch (eachPart)
                {
                    case ".":
                        break;
                    case "..":
                        if (ori.Count > 0)
                        {
                            ori.RemoveAt(ori.Count - 1);
                        }

                        break;
                    default:
                        ori.Add(eachPart);
                        break;
                }
            }

            return ori.Count == 0? "": string.Join("/", ori);
        }

        private static IEnumerable<(string parentGroupBy, string subGroupBy)> ChunkGroupBy(string longestGroupGroupBy)
        {
            // e.g "a/b/c/d"
            // first yield: "a/b/c", "a/b/c/d"
            // then yield: "a/b", "a/b/c"
            // then yield: "a", "a/b"
            string[] groupChunk = longestGroupGroupBy.Split('/');

            for (int i = groupChunk.Length - 1; i > 0; i--)
            {
                yield return (string.Join("/", groupChunk, 0, i), string.Join("/", groupChunk, 0, i + 1));
            }
        }

        public static IEnumerable<string> GetSerializedProperties(SerializedObject serializedObject)
        {
            // outSerializedProperties.Clear();
            // ReSharper disable once ConvertToUsingDeclaration
            using (SerializedProperty iterator = serializedObject.GetIterator())
            {
                // ReSharper disable once InvertIf
                if (iterator.NextVisible(true))
                {
                    do
                    {
                        // outSerializedProperties.Add(serializedObject.FindProperty(iterator.name));
                        yield return iterator.name;
                    } while (iterator.NextVisible(false));
                }
            }
        }

        // private static void SetupRendererGroup(ISaintsRendererGroup saintsRendererGroup, LayoutInfo layoutInfo)
        // {
        //     ISaintsRendererGroup group = MakeRendererGroup(layoutInfo);
        //     saintsRendererGroup.Add(group);
        //     foreach (KeyValuePair<string, LayoutInfo> kv in layoutInfo.Children)
        //     {
        //         Debug.Log($"add sub group {kv.Key}({kv.Value.Config})");
        //         SetupRendererGroup(group, kv.Value);
        //     }
        // }

        // private static ISaintsRendererGroup MakeRendererGroup(LayoutInfo layoutInfo)
        // {
        //     if (layoutInfo.Config.HasFlagFast(ELayout.Vertical))
        //     {
        //         return new VerticalGroup(layoutInfo.Config);
        //     }
        //     return new HorizontalGroup(layoutInfo.Config);
        // }
        // private static ISaintsRendererGroup MakeRendererGroup(ELayout layoutInfo)
        // {
        //     if (layoutInfo.HasFlagFast(ELayout.Tab))
        //     {
        //         return new SaintsRendererGroup(layoutInfo);
        //     }
        //     if (layoutInfo.HasFlagFast(ELayout.Horizontal))
        //     {
        //         return new HorizontalGroup(layoutInfo);
        //     }
        //     return new VerticalGroup(layoutInfo);
        // }

        public static AbsRenderer HelperMakeRenderer(SerializedObject serializedObject, SaintsFieldWithInfo fieldWithInfo)
        {
            // Debug.Log($"field {fieldWithInfo.fieldInfo?.Name}/{fieldWithInfo.fieldInfo?.GetCustomAttribute<ExtShowHideConditionBase>()}");
            switch (fieldWithInfo.RenderType)
            {
                case SaintsRenderType.SerializedField:
                {
                    foreach (IPlayaAttribute playaAttribute in fieldWithInfo.PlayaAttributes)
                    {
                        switch (playaAttribute)
                        {
                            case TableAttribute _:
                                return new TableRenderer(serializedObject, fieldWithInfo);
                            case ListDrawerSettingsAttribute _:
                                return new ListDrawerSettingsRenderer(serializedObject, fieldWithInfo);
                        }
                    }

                    return new SerializedFieldRenderer(serializedObject, fieldWithInfo);
                }
                case SaintsRenderType.InjectedSerializedField:
                    return new SerializedFieldBareRenderer(serializedObject, fieldWithInfo);
                case SaintsRenderType.NonSerializedField:
                case SaintsRenderType.NativeProperty:
                    return new NativeFieldPropertyRenderer(serializedObject, fieldWithInfo);
                case SaintsRenderType.Method:
                    return new MethodRenderer(serializedObject, fieldWithInfo);
                default:
                    throw new ArgumentOutOfRangeException(nameof(fieldWithInfo.RenderType), fieldWithInfo.RenderType, null);
            }
        }

        public virtual AbsRenderer MakeRenderer(SerializedObject so, SaintsFieldWithInfo fieldWithInfo)
        {
            return HelperMakeRenderer(so, fieldWithInfo);
        }
    }
}
