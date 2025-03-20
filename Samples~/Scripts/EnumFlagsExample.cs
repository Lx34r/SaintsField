﻿using System;
using UnityEngine;

namespace SaintsField.Samples.Scripts
{
    public class EnumFlagsExample: MonoBehaviour
    {
        [Serializable, Flags]
        public enum BitMask
        {
            None = 0,  // this will be replaced for all/none button
            [RichLabel("M<color=red>1</color>")]
            Mask1 = 1,
            [RichLabel("M<color=green>2</color>")]
            Mask2 = 1 << 1,
            [RichLabel("M<color=blue>3</color>")]
            Mask3 = 1 << 2,
            [RichLabel("M4")]
            Mask4 = 1 << 3,
            Mask5 = 1 << 4,
            MaskLongLongLongLong = 1 << 5,
            MaskLongLongLongLong2 = 1 << 6,
            Mask7 = 1 << 7,
            Mask8 = 1 << 8,
        }

        // [RichLabel("<icon=star.png /><label />")]
        [EnumFlags]
        public BitMask myMask;

        [Space(60)]

        [EnumFlags, RichLabel(null), OnValueChanged(nameof(ValueChanged))] public BitMask myMask2;
        private void ValueChanged() => Debug.Log(myMask2);

        [Serializable]
        public struct MyStruct
        {
            [EnumFlags, BelowRichLabel(nameof(myMask), true)] public BitMask myMask;
        }

        public MyStruct myStruct;

        [ReadOnly]
        [EnumFlags]
        public BitMask myMaskDisabled;

        [ReadOnly]
        [EnumFlags]
        [RichLabel("<icon=star.png /><label />")]
        public BitMask myMaskDisabledLabel;

        [Serializable]
        public enum EnumNormal
        {
            First,
            Second,
            [RichLabel("<color=lime><label /></color>")]
            Third,
        }

        [EnumFlags] public EnumNormal myEnumNormal;

    }
}
