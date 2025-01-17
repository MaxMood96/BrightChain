// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq.Expressions;
using BrightChain.EntityFrameworkCore.Utilities;
using Microsoft.EntityFrameworkCore.Query;
using Newtonsoft.Json.Linq;

#nullable disable

namespace BrightChain.EntityFrameworkCore.Query.Internal
{
    public partial class BrightChainShapedQueryCompilingExpressionVisitor
    {
        private sealed class JObjectInjectingExpressionVisitor : ExpressionVisitor
        {
            private int _currentEntityIndex;

            protected override Expression VisitExtension(Expression extensionExpression)
            {
                Check.NotNull(extensionExpression, nameof(extensionExpression));

                switch (extensionExpression)
                {
                    case EntityShaperExpression shaperExpression:
                    {
                        _currentEntityIndex++;

                        var valueBufferExpression = shaperExpression.ValueBufferExpression;

                        var jObjectVariable = Expression.Variable(
                            typeof(JObject),
                            "jObject" + _currentEntityIndex);
                        var variables = new List<ParameterExpression> { jObjectVariable };

                        var expressions = new List<Expression>
                        {
                            Expression.Assign(
                                jObjectVariable,
                                Expression.TypeAs(
                                    valueBufferExpression,
                                    typeof(JObject))),
                            Expression.Condition(
                                Expression.Equal(jObjectVariable, Expression.Constant(null, jObjectVariable.Type)),
                                Expression.Constant(null, shaperExpression.Type),
                                shaperExpression)
                        };

                        return Expression.Block(
                            shaperExpression.Type,
                            variables,
                            expressions);
                    }

#pragma warning disable CS0618 // Type or member is obsolete
                    case CollectionShaperExpression collectionShaperExpression:
#pragma warning restore CS0618 // Type or member is obsolete
                    {
                        _currentEntityIndex++;

                        var jArrayVariable = Expression.Variable(
                            typeof(JArray),
                            "jArray" + _currentEntityIndex);
                        var variables = new List<ParameterExpression> { jArrayVariable };

                        var expressions = new List<Expression>
                        {
                            Expression.Assign(
                                jArrayVariable,
                                Expression.TypeAs(
                                    collectionShaperExpression.Projection,
                                    typeof(JArray))),
                            Expression.Condition(
                                Expression.Equal(jArrayVariable, Expression.Constant(null, jArrayVariable.Type)),
                                Expression.Constant(null, collectionShaperExpression.Type),
                                collectionShaperExpression)
                        };

                        return Expression.Block(
                            collectionShaperExpression.Type,
                            variables,
                            expressions);
                    }
                }

                return base.VisitExtension(extensionExpression);
            }
        }
    }
}
