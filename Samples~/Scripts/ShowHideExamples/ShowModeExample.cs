using UnityEngine;

namespace SaintsField.Samples.Scripts.ShowHideExamples
{
    public class ShowModeExample : MonoBehaviour
    {
        public bool boolValue;

        [ShowIf(EMode.Edit)] public string showEdit;
        [ShowIf(EMode.Play)] public string showPlay;

        [ShowIf(EMode.Edit, nameof(boolValue))] public string showEditAndBool;
        [ShowIf(EMode.Edit), ShowIf(nameof(boolValue))] public string showEditOrBool;
        
        [ShowIf(EMode.Play, nameof(boolValue))] public string showPlayAndBool;
        [ShowIf(EMode.Play), ShowIf(nameof(boolValue))] public string showPlayOrBool;
    }
}
