#if UNITY_2021_3_OR_NEWER && !SAINTSFIELD_UI_TOOLKIT_DISABLE

using System;
using System.Collections.Generic;
using System.Linq;
using SaintsField.Editor.Linq;
using UnityEditor;
using Object = UnityEngine.Object;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SaintsField.Editor.AutoRunner.AutoRunnerResultsRenderer
{
    public partial class ResultsRenderer
    {
        private VisualElement _root;
        private IReadOnlyList<AutoRunnerResult> _results = new List<AutoRunnerResult>();

        protected override (VisualElement target, bool needUpdate) CreateTargetUIToolkit()
        {
            _root = new VisualElement();
            // Debug.Log(AutoRunner);
            // AutoRunnerResult[] results = AutoRunner.results;
            return (_root, true);
        }

        protected override PreCheckResult OnUpdateUIToolKit()
        {
            PreCheckResult preCheckResult = base.OnUpdateUIToolKit();

            if (_autoRunner.results.SequenceEqual(_results))
            {
                return preCheckResult;
            }

            _results = _autoRunner.results.ToArray();
            _root.Clear();

            (AutoRunnerResult value, int index)[] canFixWithIndex = _autoRunner.results
                .WithIndex()
                .Where(each => each.value.FixerResult.CanFix)
                .Reverse()
                .ToArray();
            if (canFixWithIndex.Length > 0)
            {
                _root.Add(new Button(() =>
                {
                    List<int> toRemoveIndex = new List<int>();
                    foreach ((AutoRunnerResult autoRunnerResult, int index) in canFixWithIndex)
                    {
                        bool errorFixed = false;
                        try
                        {
                            autoRunnerResult.FixerResult.Callback();
                            errorFixed = true;
                        }
                        catch (Exception e)
                        {
                            autoRunnerResult.FixerResult.ExecError = e.Message;
                        }

                        if (errorFixed)
                        {
                            toRemoveIndex.Add(index);
                        }
                    }

                    // ReSharper disable once InvertIf
                    if(toRemoveIndex.Count > 0)
                    {
                        foreach (int index in toRemoveIndex)
                        {
                            _autoRunner.results.RemoveAt(index);
                        }

                        OnUpdateUIToolKit();
                    }
                })
                {
                    text = "Fix All",
                });
            }

            foreach ((MainTarget mainTarget, IEnumerable<IGrouping<Object, AutoRunnerResultInfo>> subGroup) in FormatResults(_autoRunner.results))
            {
                // Debug.Log($"#AutoRunner# draw {mainTarget}");
                Foldout group = new Foldout
                {
                    // text = mainTarget as string ?? mainTarget.ToString(),
                };
                if (!mainTarget.MainTargetIsAssetPath)
                {
                    group.text = mainTarget.MainTargetString;
                }
                else
                {
                    Object obj = AssetDatabase.LoadAssetAtPath<Object>(mainTarget.MainTargetString);
                    if (obj == null)
                    {
                        Debug.Log($"#AutoRunner# target is null: {mainTarget.MainTargetString}");
                        continue;
                    }
                    group.Add(new ObjectField
                    {
                        value = obj,
                    });
                    group.text = obj.name;
                }

                VisualElement subGroupElement = new VisualElement
                {
                    // style =
                    // {
                    //     paddingLeft = 4,
                    //     // backgroundColor = EColor.Aqua.GetColor(),
                    // },
                };
                foreach (IGrouping<Object, AutoRunnerResultInfo> grouping in subGroup)
                {
                    if (grouping.Key == null)
                    {
                        Debug.Log($"#AutoRunner# skip null group for {mainTarget}");
                        continue;
                    }

                    Foldout subGroupElementGroup = new Foldout
                    {
                        text = grouping.Key.name,
                    };
                    subGroupElementGroup.Add(new ObjectField
                    {
                        value = grouping.Key,
                    });
                    // Debug.Log($"#AutoRunner# draw {grouping.Key} for {mainTarget}");
                    foreach (AutoRunnerResultInfo autoRunnerResultInfo in grouping)
                    {
                        VisualElement subGroupElementGroupElement = new VisualElement();

                        TextField serializedPropertyLabel = new TextField("Field/Property")
                        {
                            value = autoRunnerResultInfo.AutoRunnerResult.propertyPath,
                        };
                        // serializedPropertyLabel.SetEnabled(false);
                        subGroupElementGroupElement.Add(serializedPropertyLabel);

                        if (autoRunnerResultInfo.AutoRunnerResult.FixerResult.ExecError != "")
                        {
                            subGroupElementGroupElement.Add(new HelpBox(autoRunnerResultInfo.AutoRunnerResult.FixerResult.ExecError, HelpBoxMessageType.Warning));
                        }

                        if (autoRunnerResultInfo.AutoRunnerResult.FixerResult.Error != "")
                        {
                            subGroupElementGroupElement.Add(new HelpBox(autoRunnerResultInfo.AutoRunnerResult.FixerResult.Error, HelpBoxMessageType.Error));
                        }

                        if (autoRunnerResultInfo.AutoRunnerResult.FixerResult.CanFix)
                        {
                            subGroupElementGroupElement.Add(new Button(() =>
                            {
                                try
                                {
                                    autoRunnerResultInfo.AutoRunnerResult.FixerResult.Callback();
                                }
                                catch (Exception e)
                                {
                                    autoRunnerResultInfo.AutoRunnerResult.FixerResult.ExecError = e.Message;
                                    OnUpdateUIToolKit();
                                    return;
                                }

                                _autoRunner.results.RemoveAt(autoRunnerResultInfo.Index);
                                OnUpdateUIToolKit();
                            })
                            {
                                text = "Fix",
                            });
                        }

                        subGroupElementGroup.Add(subGroupElementGroupElement);
                    }

                    subGroupElement.Add(subGroupElementGroup);
                }

                group.Add(subGroupElement);
                _root.Add(group);
            }

            return preCheckResult;
        }
    }
}
#endif
