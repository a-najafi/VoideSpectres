using VoidSpectre.Core.Diagnostics;
using UnityEngine;

namespace VoidSpectreUnity.Diagnostics
{
    public static class VoidSpectreLogBridge
    {
        public static void Install()
        {
            VsLog.Info = msg => Debug.Log(msg);
            VsLog.Warning = msg => Debug.LogWarning(msg);
            VsLog.Exception = ex => Debug.LogException(ex);
        }
    }
}
