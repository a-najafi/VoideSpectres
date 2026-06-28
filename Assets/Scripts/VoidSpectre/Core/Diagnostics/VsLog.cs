using System;

namespace VoidSpectre.Core.Diagnostics
{
    public static class VsLog
    {
        public static Action<string> Info = _ => { };
        public static Action<string> Warning = _ => { };
        public static Action<Exception> Exception = _ => { };
    }
}
