using UnityEngine;
using UnityEngine.UI;

namespace BeatmapScanner.UI
{
    /// <summary>
    /// A Graphic that emits no geometry and is therefore fully invisible, but still acts
    /// as a valid raycast target. Used to provide a hit-test surface for the drag handle
    /// without any visible rendering artifact.
    /// </summary>
    internal class NonDrawingGraphic : Graphic
    {
        public override void SetMaterialDirty() { }
        public override void SetVerticesDirty() { }

        protected override void OnPopulateMesh(VertexHelper vh) => vh.Clear();
    }
}
