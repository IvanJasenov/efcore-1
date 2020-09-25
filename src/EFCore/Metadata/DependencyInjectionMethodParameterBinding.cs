// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Utilities;

namespace Microsoft.EntityFrameworkCore.Metadata
{
    /// <inheritdoc />
    public class DependencyInjectionMethodParameterBinding : DependencyInjectionParameterBinding
    {
        /// <summary>
        ///     Creates a new <see cref="DependencyInjectionParameterBinding" /> instance for the given method
        ///     of the given service type.
        /// </summary>
        /// <param name="parameterType"> The parameter CLR type. </param>
        /// <param name="serviceType"> The service CLR types, as resolved from dependency injection </param>
        /// <param name="method"> The method of the service to bind to. </param>
        /// <param name="serviceProperty"> The associated <see cref="IServiceProperty" />, or null. </param>
        public DependencyInjectionMethodParameterBinding(
            [NotNull] Type parameterType,
            [NotNull] Type serviceType,
            [NotNull] MethodInfo method,
            [CanBeNull] IPropertyBase serviceProperty = null)
            : base(parameterType, serviceType, serviceProperty)
        {
            Check.NotNull(method, nameof(method));

            Method = method;
        }

        /// <summary>
        ///     The method being bound to, as defined on the dependency injection service interface.
        /// </summary>
        public virtual MethodInfo Method { get; }

        /// <inheritdoc />
        public override Expression BindToParameter(
            Expression materializationExpression,
            Expression entityTypeExpression)
        {
            Check.NotNull(materializationExpression, nameof(materializationExpression));
            Check.NotNull(entityTypeExpression, nameof(entityTypeExpression));

            var parameters = Method.GetParameters().Select(
                (p, i) => Expression.Parameter(p.ParameterType, "param" + i)).ToArray();

            var serviceVariable = Expression.Variable(ServiceType, "service");
            var delegateVariable = Expression.Variable(ParameterType, "delegate");

            return Expression.Block(
                new[] { serviceVariable, delegateVariable },
                new List<Expression>
                {
                    Expression.Assign(
                        serviceVariable,
                        base.BindToParameter(materializationExpression, entityTypeExpression)),
                    Expression.Assign(
                        delegateVariable,
                        Expression.Condition(
                            Expression.ReferenceEqual(serviceVariable, Expression.Constant(null)),
                            Expression.Constant(null, ParameterType),
                            Expression.Lambda(
                                Expression.Call(
                                    serviceVariable,
                                    Method,
                                    parameters),
                                parameters))),
                    delegateVariable
                });
        }
    }
}
