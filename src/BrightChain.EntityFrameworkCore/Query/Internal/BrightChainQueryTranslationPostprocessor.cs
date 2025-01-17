﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq.Expressions;
using BrightChain.EntityFrameworkCore.Utilities;
using Microsoft.EntityFrameworkCore.Query;

namespace BrightChain.EntityFrameworkCore.Query.Internal
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public class BrightChainQueryTranslationPostprocessor : QueryTranslationPostprocessor
    {
        private readonly ISqlExpressionFactory _sqlExpressionFactory;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public BrightChainQueryTranslationPostprocessor(
            QueryTranslationPostprocessorDependencies dependencies,
            ISqlExpressionFactory sqlExpressionFactory,
            QueryCompilationContext queryCompilationContext)
            : base(dependencies, queryCompilationContext)
        {
            Check.NotNull(sqlExpressionFactory, nameof(sqlExpressionFactory));

            _sqlExpressionFactory = sqlExpressionFactory;
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public override Expression Process(Expression query)
        {
            query = base.Process(query);

            if (query is ShapedQueryExpression shapedQueryExpression
                && shapedQueryExpression.QueryExpression is SelectExpression selectExpression)
            {
                // BrightChain does not have nested select expression so this should be safe.
                selectExpression.ApplyProjection();
            }

            query = new BrightChainValueConverterCompensatingExpressionVisitor(_sqlExpressionFactory).Visit(query);

            return query;
        }
    }
}
