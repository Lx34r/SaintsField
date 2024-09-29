using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SaintsField.Editor.Utils;
using UnityEditor;

namespace SaintsField.Editor.Drawers.DisabledDrawers
{
    [CustomPropertyDrawer(typeof(EnableIfAttribute))]
    public class EnableIfAttributeDrawer: ReadOnlyAttributeDrawer
    {
        protected override (string error, bool disabled) IsDisabled(SerializedProperty property, FieldInfo info, object target)
        {
            List<bool> allResults = new List<bool>();
            
            ReadOnlyAttribute[] targetAttributes = SerializedUtils.GetAttributesAndDirectParent<ReadOnlyAttribute>(property).attributes;
            foreach (var targetAttribute in targetAttributes.Where(_ => !_.IsReadOnly)) // EnableIfAttribute
            {
                (IReadOnlyList<string> errors, IReadOnlyList<bool> boolResults) = Util.ConditionChecker(targetAttribute.ConditionInfos, property, info, target);

                if (errors.Count > 0)
                {
                    return (string.Join("\n\n", errors), true); // don't disable
                }
                
                bool editorModeOk = Util.ConditionEditModeChecker(targetAttribute.EditorMode);
                // And Mode
                bool boolResultsOk = boolResults.All(each => each);
                allResults.Add(editorModeOk && boolResultsOk);
            }
            
            // Or Mode
            bool truly = allResults.Any(each => each);

#if SAINTSFIELD_DEBUG && SAINTSFIELD_DEBUG_READ_ONLY
            Debug.Log($"{property.name} final={truly}/ars={string.Join(",", allResults)}");
#endif
            return ("", !truly); // reverse
        }
    }
}
