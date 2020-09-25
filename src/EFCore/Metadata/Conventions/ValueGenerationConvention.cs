// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;

namespace Microsoft.EntityFrameworkCore.Metadata.Conventions
{
    /// <summary>
    ///     A convention that configures store value generation as <see cref="ValueGenerated.OnAdd" /> on properties that are
    ///     part of the primary key and not part of any foreign keys.
    /// </summary>
    public class ValueGenerationConvention :
        IEntityTypePrimaryKeyChangedConvention,
        IForeignKeyAddedConvention,
        IForeignKeyRemovedConvention,
        IForeignKeyPropertiesChangedConvention,
        IEntityTypeBaseTypeChangedConvention,
        IForeignKeyOwnershipChangedConvention
    {
        /// <summary>
        ///     Creates a new instance of <see cref="ValueGenerationConvention" />.
        /// </summary>
        /// <param name="dependencies"> Parameter object containing dependencies for this convention. </param>
        public ValueGenerationConvention([NotNull] ProviderConventionSetBuilderDependencies dependencies)
        {
            Dependencies = dependencies;
        }

        /// <summary>
        ///     Parameter object containing service dependencies.
        /// </summary>
        protected virtual ProviderConventionSetBuilderDependencies Dependencies { get; }

        /// <inheritdoc />
        public virtual void ProcessForeignKeyAdded(
            IConventionForeignKeyBuilder relationshipBuilder,
            IConventionContext<IConventionForeignKeyBuilder> context)
        {
            var foreignKey = relationshipBuilder.Metadata;
            if (!foreignKey.IsBaseLinking())
            {
                foreach (var property in foreignKey.Properties)
                {
                    property.Builder.ValueGenerated(ValueGenerated.Never);
                }
            }
        }

        /// <inheritdoc />
        public virtual void ProcessForeignKeyRemoved(
            IConventionEntityTypeBuilder entityTypeBuilder,
            IConventionForeignKey foreignKey,
            IConventionContext<IConventionForeignKey> context)
        {
            OnForeignKeyRemoved(foreignKey.Properties);
        }

        /// <inheritdoc />
        public virtual void ProcessForeignKeyPropertiesChanged(
            IConventionForeignKeyBuilder relationshipBuilder,
            IReadOnlyList<IConventionProperty> oldDependentProperties,
            IConventionKey oldPrincipalKey,
            IConventionContext<IReadOnlyList<IConventionProperty>> context)
        {
            var foreignKey = relationshipBuilder.Metadata;
            if (!foreignKey.Properties.SequenceEqual(oldDependentProperties))
            {
                OnForeignKeyRemoved(oldDependentProperties);

                if (relationshipBuilder.Metadata.Builder != null
                    && !foreignKey.IsBaseLinking())
                {
                    foreach (var property in foreignKey.Properties)
                    {
                        property.Builder.ValueGenerated(ValueGenerated.Never);
                    }
                }
            }
        }

        private void OnForeignKeyRemoved(IReadOnlyList<IConventionProperty> foreignKeyProperties)
        {
            foreach (var property in foreignKeyProperties)
            {
                var pk = property.FindContainingPrimaryKey();
                if (pk == null)
                {
                    property.Builder?.ValueGenerated(GetValueGenerated(property));
                }
                else
                {
                    foreach (var keyProperty in pk.Properties)
                    {
                        keyProperty.Builder.ValueGenerated(GetValueGenerated(property));
                    }
                }
            }
        }

        /// <inheritdoc />
        public virtual void ProcessEntityTypePrimaryKeyChanged(
            IConventionEntityTypeBuilder entityTypeBuilder,
            IConventionKey newPrimaryKey,
            IConventionKey previousPrimaryKey,
            IConventionContext<IConventionKey> context)
        {
            if (previousPrimaryKey != null)
            {
                foreach (var property in previousPrimaryKey.Properties)
                {
                    property.Builder?.ValueGenerated(ValueGenerated.Never);
                }
            }

            if (newPrimaryKey?.Builder != null)
            {
                foreach (var property in newPrimaryKey.Properties)
                {
                    property.Builder.ValueGenerated(GetValueGenerated(property));
                }
            }
        }

        /// <inheritdoc />
        public virtual void ProcessEntityTypeBaseTypeChanged(
            IConventionEntityTypeBuilder entityTypeBuilder,
            IConventionEntityType newBaseType,
            IConventionEntityType oldBaseType,
            IConventionContext<IConventionEntityType> context)
        {
            if (entityTypeBuilder.Metadata.BaseType != newBaseType)
            {
                return;
            }

            foreach (var property in entityTypeBuilder.Metadata.GetProperties())
            {
                property.Builder.ValueGenerated(GetValueGenerated(property));
            }
        }

        /// <summary>
        ///     Returns the store value generation strategy to set for the given property.
        /// </summary>
        /// <param name="property"> The property. </param>
        /// <returns> The store value generation strategy to set for the given property. </returns>
        protected virtual ValueGenerated? GetValueGenerated([NotNull] IConventionProperty property)
            => GetValueGenerated((IProperty)property);

        /// <summary>
        ///     Returns the store value generation strategy to set for the given property.
        /// </summary>
        /// <param name="property"> The property. </param>
        /// <returns> The store value generation strategy to set for the given property. </returns>
        public static ValueGenerated? GetValueGenerated([NotNull] IProperty property)
            => !property.GetContainingForeignKeys().Any(fk => !fk.IsBaseLinking())
                && ShouldHaveGeneratedProperty(property.FindContainingPrimaryKey())
                && CanBeGenerated(property)
                    ? ValueGenerated.OnAdd
                    : (ValueGenerated?)null;

        private static bool ShouldHaveGeneratedProperty(IKey key)
        {
            var onOwnedType = key?.DeclaringEntityType.IsOwned();
            return key != null
                && (onOwnedType.Value && key.Properties.Count(p => !p.IsForeignKey()) == 1
                    || !onOwnedType.Value && key.Properties.Count == 1);
        }

        /// <summary>
        ///     Indicates whether the specified property can have the value generated by the store or by a non-temporary value generator
        ///     when not set.
        /// </summary>
        /// <param name="property"> The key property that might be store generated. </param>
        /// <returns> A value indicating whether the specified property should have the value generated by the store. </returns>
        private static bool CanBeGenerated(IProperty property)
        {
            var propertyType = property.ClrType.UnwrapNullableType();
            return (propertyType.IsInteger()
                    && propertyType != typeof(byte))
                || propertyType == typeof(Guid);
        }

        /// <summary>
        ///     Called after the ownership value for a foreign key is changed.
        /// </summary>
        /// <param name="relationshipBuilder"> The builder for the foreign key. </param>
        /// <param name="context"> Additional information associated with convention execution. </param>
        public virtual void ProcessForeignKeyOwnershipChanged(
            IConventionForeignKeyBuilder relationshipBuilder,
            IConventionContext<bool?> context)
        {
            foreach (var property in relationshipBuilder.Metadata.DeclaringEntityType.GetProperties())
            {
                property.Builder.ValueGenerated(GetValueGenerated(property));
            }
        }
    }
}
