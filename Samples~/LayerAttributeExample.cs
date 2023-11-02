﻿using UnityEngine;

namespace SaintsField.Samples
{
    public class LayerAttributeExample: MonoBehaviour
    {
        [SerializeField, Layer] private string _layerString;
        [SerializeField, Layer] private int _layerInt;
    }
}