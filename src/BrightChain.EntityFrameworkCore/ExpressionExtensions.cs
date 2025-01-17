// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

#nullable enable

// ReSharper disable once CheckNamespace
namespace System.Linq.Expressions
{
    [DebuggerStepThrough]
    internal static class ExpressionExtensions
    {
        public static bool IsNullConstantExpression(this Expression expression)
        {
            return RemoveConvert(expression) is ConstantExpression constantExpression
                           && constantExpression.Value == null;
        }

        public static LambdaExpression UnwrapLambdaFromQuote(this Expression expression)
        {
            return (LambdaExpression)(expression is UnaryExpression unary && expression.NodeType == ExpressionType.Quote
                           ? unary.Operand
                           : expression);
        }

        [return: NotNullIfNotNull("expression")]
        public static Expression? UnwrapTypeConversion(this Expression? expression, out Type? convertedType)
        {
            convertedType = null;
            while (expression is UnaryExpression unaryExpression
                && (unaryExpression.NodeType == ExpressionType.Convert
                    || unaryExpression.NodeType == ExpressionType.ConvertChecked
                    || unaryExpression.NodeType == ExpressionType.TypeAs))
            {
                expression = unaryExpression.Operand;
                if (unaryExpression.Type != typeof(object) // Ignore object conversion
                    && !unaryExpression.Type.IsAssignableFrom(expression.Type)) // Ignore casting to base type/interface
                {
                    convertedType = unaryExpression.Type;
                }
            }

            return expression;
        }

        private static Expression RemoveConvert(Expression expression)
        {
            if (expression is UnaryExpression unaryExpression
                && (expression.NodeType == ExpressionType.Convert
                    || expression.NodeType == ExpressionType.ConvertChecked))
            {
                return RemoveConvert(unaryExpression.Operand);
            }

            return expression;
        }

        public static T GetConstantValue<T>(this Expression expression)
        {
            return (T)(expression is ConstantExpression constantExpression
                           ? (constantExpression.Value!)
                           : throw new InvalidOperationException());
        }
    }
}
