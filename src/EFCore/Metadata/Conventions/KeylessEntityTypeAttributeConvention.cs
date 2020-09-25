// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;

namespace Microsoft.EntityFrameworkCore.Metadata.Conventions
{
    /// <summary>
    ///     A convention that ignores entity types that have the <see cref="KeylessAttribute" />.
    /// </summary>
    public class KeylessEntityTypeAttributeConvention : EntityTypeAttributeConventionBase<KeylessAttribute>
    {
        /// <summary>
        ///     Creates a new instance of <see cref="KeylessEntityTypeAttributeConvention" />.
        /// </summary>
        /// <param name="dependencies"> Parameter object containing dependencies for this convention. </param>
        public KeylessEntityTypeAttributeConvention([NotNull] ProviderConventionSetBuilderDependencies dependencies)
            : base(dependencies)
        {
        }

        /// <inheritdoc />
        protected override void ProcessEntityTypeAdded(
            IConventionEntityTypeBuilder entityTypeBuilder,
            KeylessAttribute attribute,
            IConventionContext<IConventionEntityTypeBuilder> context)
        {
            entityTypeBuilder.HasNoKey(fromDataAnnotation: true);
        }
    }
}
