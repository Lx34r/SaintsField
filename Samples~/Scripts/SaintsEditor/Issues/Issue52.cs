using SaintsField.Playa;

namespace SaintsField.Samples.Scripts.SaintsEditor.Issues
{
    public class Issue52 : SaintsMonoBehaviour
    {
        public string start;

        [LayoutGroup("Group", ELayout.FoldoutBox, marginTop: 10, marginBottom: 10)]
        public string group1;
        public string group2;
        public string group3;
        [LayoutEnd("Group")]

        public string middle;

        [Layout("Layout", ELayout.TitleBox, marginTop: 10, marginBottom: 10)] public string layout1;
        [Layout("Layout")] public string layout2;
        [Layout("Layout")] public string layout3;

        public string end;
    }
}
