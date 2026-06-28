using System;
using VoidSpectre.Core.Services;
using UnityEngine;

namespace VoidSpectreUnity.Services
{
    public abstract class SceneServiceBehaviour : MonoBehaviour, ISceneServiceProvider
    {
        [SerializeField] private string key;

        public virtual Type ContractType => GetType();
        public virtual object Key => string.IsNullOrEmpty(key) ? ServiceKey.None : key;

        public virtual void Register(ServiceLocator locator) =>
            locator.Register(ContractType, this, Key);
    }

    public abstract class SceneServiceBehaviour<TContract> : SceneServiceBehaviour
        where TContract : UnityEngine.Object
    {
        public override Type ContractType => typeof(TContract);
    }

    public abstract class SerializedScriptableObjectServiceProvider : ScriptableObject, ISceneServiceProvider
    {
        public virtual Type ContractType => GetType();
        public virtual object Key => ServiceKey.None;

        public virtual void Register(ServiceLocator locator) =>
            locator.Register(ContractType, this, Key);
    }

    public abstract class SerializedScriptableObjectServiceProvider<TContract> : SerializedScriptableObjectServiceProvider
        where TContract : UnityEngine.Object
    {
        public override Type ContractType => typeof(TContract);
    }
}
