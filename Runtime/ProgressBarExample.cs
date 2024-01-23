﻿using UnityEngine;

namespace SaintsField
{
    public class ProgressBarExample: MonoBehaviour
    {
        [ProgressBar(10)] public int myHp;
        [ProgressBar(0, 100f, step: 0.05f, color: EColor.Blue)] public float myMp;

        [Space]
        public int minValue;
        public int maxValue;

        [ProgressBar(nameof(minValue)
                , nameof(maxValue)
                , step: 0.05f
                , backgroundColorCallback: nameof(BackgroundColor)
                , colorCallback: nameof(FillColor)
                , titleCallback: nameof(Title)
            ),
        ]
        [RichLabel(null)]
        public float fValue;

        private EColor BackgroundColor() => fValue <= 0? EColor.Brown: EColor.CharcoalGray;

        private Color FillColor() => Color.Lerp(Color.yellow, EColor.Green.GetColor(), Mathf.Pow(Mathf.InverseLerp(minValue, maxValue, fValue), 2));

        private string Title(float curValue, float min, float max, string label) => curValue < 0 ? $"[{label}] Game Over: {curValue}" : $"[{label}] {curValue / max:P}";
    }
}