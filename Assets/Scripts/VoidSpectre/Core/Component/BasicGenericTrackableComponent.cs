using Sirenix.Serialization;

namespace VoidSpectre.Core.Component
{
    [System.Serializable]
    public class BasicGenericTrackableComponent<T> : TrackableComponentBase
    {
        [OdinSerialize]
        protected T _value;

        public T Value
        {
            get => _value;
            set => SetField(ref _value, value);
        }

        public BasicGenericTrackableComponent() { }

        public BasicGenericTrackableComponent(T value) => _value = value;
    }
}
