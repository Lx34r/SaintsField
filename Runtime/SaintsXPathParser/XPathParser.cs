#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SaintsField.SaintsXPathParser.XPathAttribute;
using UnityEngine;

// ReSharper disable ReplaceSubstringWithRangeIndexer


namespace SaintsField.SaintsXPathParser
{
    public static class XPathParser
    {
        public static IEnumerable<XPathStep> Parse(string xPath)
        {
            foreach ((int stepSep, string stepContent) in SplitXPath(xPath))
            {
                (string axisNameRaw, string attrRaw, string nodeTestRaw, string predicatesRaw) = SplitStep(stepContent);
                AxisName axisName = ParseAxisName(axisNameRaw);
                XPathAttrBase attr = null;
                if(!string.IsNullOrEmpty(attrRaw))
                {
                    (XPathAttrBase xPathAttrBase, string leftContent) = XPathAttrBase.Parser(attrRaw);
                    Debug.Assert(leftContent == "", attrRaw);
                    attr = xPathAttrBase;
                }
                NodeTest nodeTest = ParseNodeTest(nodeTestRaw);
                IReadOnlyList<XPathPredicate> predicates = string.IsNullOrEmpty(predicatesRaw)
                    ? Array.Empty<XPathPredicate>()
                    : XPathBracketParser
                        .ParseFilter(predicatesRaw)
                        .Select(each => new XPathPredicate
                        {
                            Attr = each.attrBase,
                            FilterComparer = each.filterComparerBase,
                        })
                        .ToArray();

                yield return new XPathStep
                {
                    SepCount = stepSep,
                    AxisName = axisName,
                    NodeTest = nodeTest,
                    Attr = attr,
                    Predicates = predicates,
                };
            }
        }

        private static NodeTest ParseNodeTest(string nodeTestRaw)
        {
            // ReSharper disable once ConvertSwitchStatementToSwitchExpression
            switch (nodeTestRaw)
            {
                case null:
                case "" :
                    return NodeTest.None;
                case "::ancestor":
                    return NodeTest.Ancestor;
                case "::ancestor-inside-prefab":
                    return NodeTest.AncestorInsidePrefab;
                case "::ancestor-or-self":
                    return NodeTest.AncestorOrSelf;
                case "::ancestor-or-self-inside-prefab":
                    return NodeTest.AncestorOrSelfInsidePrefab;
                case "::parent":
                    return NodeTest.Parent;
                case "::parent-or-self":
                    return NodeTest.ParentOrSelf;
                case "::parent-or-self-inside-prefab":
                    return NodeTest.ParentOrSelfInsidePrefab;
                case "::scene-root":
                    return NodeTest.SceneRoot;
                case "::prefab-root":
                    return NodeTest.PrefabRoot;
                case "::Resources":
                    return NodeTest.Resources;
                case "::Asset":
                    return NodeTest.Asset;
                default:
                    throw new ArgumentOutOfRangeException(nameof(nodeTestRaw), nodeTestRaw, null);
            }
        }

        private static IEnumerable<(int stepSep, string stepContent)> SplitXPath(string xPath)
        {
            StringBuilder stepBuilder = new StringBuilder();

            StringBuilder quoteBuilder = null;
            char quoteType = '\0';

            Queue<char> chars = new Queue<char>(xPath);

            int sepCount = 0;
            bool hasContent = false;

            while (chars.Count > 0)
            {
                char curChar = chars.Dequeue();
                if (curChar == '/')
                {
                    if (hasContent)  // `content/`
                    {
                        yield return (sepCount, stepBuilder.ToString());
                        sepCount = 1;
                        hasContent = false;
                        stepBuilder.Clear();
                        quoteBuilder = null;
                        quoteType = '\0';
                    }
                    else  // continued `/`
                    {
                        sepCount += 1;
                    }
                }
                else
                {
                    hasContent = true;
                    bool isSingleQuote = curChar == '\'';
                    bool isDoubleQuote = curChar == '"';
                    bool inSingleQuote = quoteType == '\'';
                    bool inDoubleQuote = quoteType == '"';

                    bool matchedQuote = (isSingleQuote && inSingleQuote) || (isDoubleQuote && inDoubleQuote);
                    if (isSingleQuote || isDoubleQuote)
                    {
                        if (quoteBuilder == null)  // new quote
                        {
                            quoteType = curChar;
                            quoteBuilder = new StringBuilder();
                            quoteBuilder.Append(curChar);
                        }
                        else  // still in quote
                        {
                            if (matchedQuote)  // same quote, now close it
                            {
                                quoteBuilder.Append(curChar);
                                stepBuilder.Append(quoteBuilder.ToString());
                                quoteBuilder = null;
                            }
                            else  // keep quoting
                            {
                                quoteBuilder.Append(curChar);
                            }
                        }
                    }
                    else  // not in any quote
                    {
                        stepBuilder.Append(curChar);
                    }
                }
            }

            if (hasContent)
            {
                if (quoteBuilder != null)
                {
                    stepBuilder.Append(quoteBuilder.ToString());
                }
                yield return (sepCount, stepBuilder.ToString());
            }
        }

        private static (string axisName, string attr, string nodeTest, string predicates) SplitStep(string step)
        {
            int nodeTestSepIndex = step.IndexOf("::", StringComparison.Ordinal);
            int attrSepIndex = step.IndexOf('@');
            int bracketSepIndex = step.IndexOf('[');
            if (bracketSepIndex != -1 && bracketSepIndex < attrSepIndex)
            {
                attrSepIndex = -1;
            }

            if (nodeTestSepIndex == -1 && attrSepIndex == -1)  // name[filter]
            {
                (string axisName, string predicates) = SplitPredicates(step);
                return (axisName, "", "", predicates);
            }

            if (nodeTestSepIndex != -1 && attrSepIndex != -1)
            {
                if (nodeTestSepIndex < attrSepIndex)
                {
                    attrSepIndex = -1;
                }
                else
                {
                    nodeTestSepIndex = -1;
                }
            }

            if (nodeTestSepIndex != -1)  // name[filter]::nodeTest
            {
                string axisNameAndPredicates = step.Substring(0, nodeTestSepIndex);
                string nodeTest = step.Substring(nodeTestSepIndex);
                (string axisName, string predicates) = SplitPredicates(axisNameAndPredicates);
                return (axisName, "", nodeTest, predicates);
            }
            else  // name[filter]@attr
            {
                string axisNameAndPredicates = step.Substring(0, attrSepIndex);
                string attr = step.Substring(attrSepIndex);
                (string axisName, string predicates) = SplitPredicates(axisNameAndPredicates);
                return (axisName, attr, "", predicates);
            }
        }

        private static (string preText, string predicates) SplitPredicates(string text)
        {
            int squareQuoteStart = text.IndexOf("[", StringComparison.Ordinal);
            if (squareQuoteStart == -1)
            {
                return (text, "");
            }

            string preText = text.Substring(0, squareQuoteStart);
            string predicatesText = text.Substring(squareQuoteStart);

            return (preText, predicatesText);
        }

        private static AxisName ParseAxisName(string axisNameRaw)
        {
            switch (axisNameRaw)
            {
                case "":
                    return new AxisName
                    {
                        NameEmpty = true,
                    };
                case "*":
                    return new AxisName
                    {
                        NameAny = true,
                    };
            }

            List<string> rawFragments = axisNameRaw.Split('*').ToList();
            if (rawFragments.Count == 1)
            {
                return new AxisName
                {
                    ExactMatch = axisNameRaw,
                };
            }

            bool startsWithFragment = !axisNameRaw.StartsWith("*");

            string startsWithStr = null;
            if (startsWithFragment)
            {
                startsWithStr = rawFragments[0];
                rawFragments.RemoveAt(0);
            }

            if(rawFragments.Count == 0)
            {
                return new AxisName
                {
                    StartsWith = startsWithStr,
                };
            }

            int lastIndex = rawFragments.Count - 1;
            string endsLeftValue = rawFragments[lastIndex];
            rawFragments.RemoveAt(lastIndex);
            bool endsWithFragment = !endsLeftValue.EndsWith("*");

            return new AxisName
            {
                StartsWith = startsWithStr,
                EndsWith = endsWithFragment? endsLeftValue: null,
                Contains = rawFragments,
            };
        }
    }
}

// ReSharper enable ReplaceSubstringWithRangeIndexer
#endif
