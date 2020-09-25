// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;

namespace Microsoft.EntityFrameworkCore.Metadata.Conventions
{
    /// <summary>
    ///     A convention that configures the discriminator value for entity types in a hierarchy as the entity type name.
    /// </summary>
    public class DiscriminatorConvention : IEntityTypeBaseTypeChangedConvention, IEntityTypeRemovedConvention
    {
        /// <summary>
        ///     Creates a new instance of <see cref="DiscriminatorConvention" />.
        /// </summary>
        /// <param name="dependencies"> Parameter object containing dependencies for this convention. </param>
        public DiscriminatorConvention([NotNull] ProviderConventionSetBuilderDependencies dependencies)
        {
            Dependencies = dependencies;
        }

        /// <summary>
        ///     Parameter object containing service dependencies.
        /// </summary>
        protected virtual ProviderConventionSetBuilderDependencies Dependencies { get; }

        /// <inheritdoc />
        public virtual void ProcessEntityTypeBaseTypeChanged(
            IConventionEntityTypeBuilder entityTypeBuilder,
            IConventionEntityType newBaseType,
            IConventionEntityType oldBaseType,
            IConventionContext<IConventionEntityType> context)
        {
            if (oldBaseType != null
                && oldBaseType.BaseType == null
                && !oldBaseType.GetDirectlyDerivedTypes().Any())
            {
                oldBaseType.Builder?.HasNoDiscriminator();
            }

            var conventionEntityTypeBuilder = entityTypeBuilder;
            var entityType = entityTypeBuilder.Metadata;
            var derivedEntityTypes = entityType.GetDerivedTypes().ToList();

            IConventionDiscriminatorBuilder discriminator;
            if (newBaseType == null)
            {
                if (derivedEntityTypes.Count == 0)
                {
                    conventionEntityTypeBuilder.HasNoDiscriminator();
                    return;
                }

                discriminator = conventionEntityTypeBuilder.HasDiscriminator(typeof(string));
            }
            else
            {
                if (conventionEntityTypeBuilder.HasNoDiscriminator() == null)
                {
                    return;
                }

                var rootTypeBuilder = entityType.GetRootType().Builder;
                discriminator = rootTypeBuilder?.HasDiscriminator(typeof(string));

                if (newBaseType.BaseType == null)
                {
                    discriminator?.HasValue(newBaseType, newBaseType.ShortName());
                }
            }

            if (discriminator != null)
            {
                discriminator.HasValue(entityTypeBuilder.Metadata, entityTypeBuilder.Metadata.ShortName());
                SetDefaultDiscriminatorValues(derivedEntityTypes, discriminator);
            }
        }

        /// <inheritdoc />
        public virtual void ProcessEntityTypeRemoved(
            IConventionModelBuilder modelBuilder,
            IConventionEntityType entityType,
            IConventionContext<IConventionEntityType> context)
        {
            var oldBaseType = entityType.BaseType;
            if (oldBaseType != null
                && oldBaseType.BaseType == null
                && !oldBaseType.GetDirectlyDerivedTypes().Any())
            {
                oldBaseType.Builder?.HasNoDiscriminator();
            }
        }

        /// <summary>
        ///     Configures the discriminator values for the given entity types.
        /// </summary>
        /// <param name="entityTypes"> The entity types to configure. </param>
        /// <param name="discriminatorBuilder"> The discriminator builder. </param>
        protected virtual void SetDefaultDiscriminatorValues(
            [NotNull] IEnumerable<IConventionEntityType> entityTypes,
            [NotNull] IConventionDiscriminatorBuilder discriminatorBuilder)
        {
            foreach (var entityType in entityTypes)
            {
                discriminatorBuilder.HasValue(entityType, entityType.ShortName());
            }
        }
    }
}
