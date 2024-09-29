using System.Collections.Generic;
using SaintsField.Condition;

namespace SaintsField
{
    public interface IReadOnlyAttribute
    {
        IReadOnlyList<ConditionInfo> ConditionInfos { get; }
        EMode EditorMode { get; }
        bool IsReadOnly { get; }
    }
}