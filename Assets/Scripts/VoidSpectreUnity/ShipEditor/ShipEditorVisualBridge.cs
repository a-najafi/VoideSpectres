using System;

namespace VoidSpectreUnity.ShipEditor
{
    public static class ShipEditorVisualBridge
    {
        public static Action<ShipEditorPartMB> RefreshVisual;
        public static Action CleanupAllEditorVisuals;
    }
}
