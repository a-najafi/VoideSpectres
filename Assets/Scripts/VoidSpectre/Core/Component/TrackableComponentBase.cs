using System;
using System.Collections.Generic;
using VoidSpectre.Core.Interfaces;

namespace VoidSpectre.Core.Component
{
    [Serializable]
    public abstract class TrackableComponentBase : ITrackableComponent
    {
        public uint LocalVersion { get; private set; }
        public void BumpVersion() => LocalVersion++;

        protected bool SetField<T>(ref T field, T value)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            BumpVersion();
            return true;
        }
    }
}
