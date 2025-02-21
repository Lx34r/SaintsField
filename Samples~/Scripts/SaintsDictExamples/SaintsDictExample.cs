using SaintsField.Playa;
using SaintsField.Samples.Scripts.SaintsEditor;
using UnityEngine;

namespace SaintsField.Samples.Scripts.SaintsDictExamples
{
    public class SaintsDictExample : SaintsMonoBehaviour
    {
        public SaintsDictionary<string, GameObject> genDict;

        [Button]
        private void DebugKey(string key)
        {
            if(!genDict.TryGetValue(key, out GameObject go))
            {
                Debug.LogWarning($"Key {key} not found in dictionary");
                return;
            }
            Debug.Log(go);
        }
    }
}
