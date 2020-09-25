// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore.ChangeTracking.Internal
{
    /// <inheritdoc />
    public class InternalEntityEntryFactory : IInternalEntityEntryFactory
    {
        /// <inheritdoc />
        public virtual InternalEntityEntry Create(IStateManager stateManager, IEntityType entityType, object entity)
            => NewInternalEntityEntry(stateManager, entityType, entity);

        /// <inheritdoc />
        public virtual InternalEntityEntry Create(
            IStateManager stateManager,
            IEntityType entityType,
            object entity,
            in ValueBuffer valueBuffer)
            => NewInternalEntityEntry(stateManager, entityType, entity, valueBuffer);

        private static InternalEntityEntry NewInternalEntityEntry(IStateManager stateManager, IEntityType entityType, object entity)
        {
            if (!entityType.HasClrType())
            {
                return new InternalShadowEntityEntry(stateManager, entityType);
            }

            Check.DebugAssert(entity != null, "entity is null");

            return entityType.ShadowPropertyCount() > 0
                ? (InternalEntityEntry)new InternalMixedEntityEntry(stateManager, entityType, entity)
                : new InternalClrEntityEntry(stateManager, entityType, entity);
        }

        private static InternalEntityEntry NewInternalEntityEntry(
            IStateManager stateManager,
            IEntityType entityType,
            object entity,
            in ValueBuffer valueBuffer)
        {
            return !entityType.HasClrType()
                ? new InternalShadowEntityEntry(stateManager, entityType, valueBuffer)
                : entityType.ShadowPropertyCount() > 0
                    ? (InternalEntityEntry)new InternalMixedEntityEntry(stateManager, entityType, entity, valueBuffer)
                    : new InternalClrEntityEntry(stateManager, entityType, entity);
        }
    }
}
