using System;
using System.Collections.Generic;
using Sirenix.Serialization;
using VoidSpectre.Core.Component;

namespace VoidSpectre.Core.Context
{
    [Serializable]
    public sealed class ContextMembershipComponent : TrackableComponentBase
    {
        [OdinSerialize] private HashSet<ComponentStore.EntityId> _memberEntities = new();

        public HashSet<ComponentStore.EntityId> MemberEntities => _memberEntities;

        public void AddMember(ComponentStore.EntityId entity)
        {
            if (_memberEntities.Add(entity))
                BumpVersion();
        }

        public void RemoveMember(ComponentStore.EntityId entity)
        {
            if (_memberEntities.Remove(entity))
                BumpVersion();
        }
    }
}
