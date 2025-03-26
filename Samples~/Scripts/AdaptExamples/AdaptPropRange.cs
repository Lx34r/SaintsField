using Unity.Mathematics;
using UnityEngine;

namespace SaintsField.Samples.Scripts.AdaptExamples
{
    public class AdaptPropRange : MonoBehaviour
    {
        public float3 f3;

        [Adapt(EUnit.Percent)] public float percentF;
        [Adapt(EUnit.Percent)] public int percentI;
        [PropRange(0f, 1f), Adapt(EUnit.Percent), OverlayRichLabel("<color=gray>%", end: true), BelowRichLabel("$" + nameof(percentRange)), BelowButton("$" + nameof(ExternalPumpValue), "Pump")] public float percentRange;
        [
            PropRange(0f, 1f, step: 0.05f),
            Adapt(EUnit.Percent),
            OverlayRichLabel("<color=gray>%", end: true),
            BelowRichLabel("$" + nameof(DisplayActualValue)),
            // BelowButton("$" + nameof(ExternalPumpValueStep), "Pump"),
            // BelowButton("$" + nameof(SetValue))
        ] public float stepRange;

        private string DisplayActualValue(float av) => $"<color=gray>Actual Value: {av}";

        private void ExternalPumpValue(float curValue)
        {
            // Debug.Log(curValue);
            percentRange = Mathf.FloorToInt((curValue + 0.1f) / 0.1f) % 10 * 0.1f;
        }

        private void ExternalPumpValueStep(float curValue)
        {
            // Debug.Log(curValue);
            stepRange = Mathf.FloorToInt((curValue + 0.1f) / 0.1f) % 10 * 0.1f + 0.001f;
        }

        [Space]
        public float v;

        private void SetValue()
        {
            stepRange = v;
        }

    }
}
