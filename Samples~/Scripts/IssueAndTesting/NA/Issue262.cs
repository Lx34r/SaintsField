using UnityEngine;

namespace SaintsField.Samples.Scripts.IssueAndTesting.NA
{
    public class Issue262 : MonoBehaviour
    {
        [
            SerializeField,
            Header("Center message"),
#if SAINTSFIELD_SAMPLE_NAUGHYTATTRIBUTES
            NaughtyAttributes.Required,
#else
            InfoBox("NaughtyAttributes not installed", above: true),
#endif
        ]
        // ReSharper disable once NotAccessedField.Local
#pragma warning disable 0262
        private Canvas naCenterMessageCanvas = default;
#pragma warning restore 0262

        // nah, SaintsField always render the error message below the field so...
        [
            SerializeField,
            Header("Center message"),
            Required,
        ]
        // ReSharper disable once NotAccessedField.Local
        private Canvas centerMessageCanvas = default;
    }
}
