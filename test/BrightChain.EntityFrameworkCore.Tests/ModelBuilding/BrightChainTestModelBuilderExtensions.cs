// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ModelBuilding;

namespace BrightChain.EntityFrameworkCore.ModelBuilding
{
    public static class BrightChainTestModelBuilderExtensions
    {
        public static ModelBuilderTest.TestEntityTypeBuilder<TEntity> HasPartitionKey<TEntity, TProperty>(
            this ModelBuilderTest.TestEntityTypeBuilder<TEntity> builder,
            Expression<Func<TEntity, TProperty>> propertyExpression)
            where TEntity : class
        {
            switch (builder)
            {
                case IInfrastructure<EntityTypeBuilder<TEntity>> genericBuilder:
                    genericBuilder.Instance.HasPartitionKey(propertyExpression);
                    break;
                case IInfrastructure<EntityTypeBuilder> nonGenericBuilder:
                    var memberInfo = propertyExpression.GetMemberAccess();
                    nonGenericBuilder.Instance.HasPartitionKey(memberInfo.Name);
                    break;
            }

            return builder;
        }

        public static ModelBuilderTest.TestEntityTypeBuilder<TEntity> HasPartitionKey<TEntity>(
            this ModelBuilderTest.TestEntityTypeBuilder<TEntity> builder,
            string name)
            where TEntity : class
        {
            switch (builder)
            {
                case IInfrastructure<EntityTypeBuilder<TEntity>> genericBuilder:
                    genericBuilder.Instance.HasPartitionKey(name);
                    break;
                case IInfrastructure<EntityTypeBuilder> nonGenericBuilder:
                    nonGenericBuilder.Instance.HasPartitionKey(name);
                    break;
            }

            return builder;
        }

        public static ModelBuilderTest.TestPropertyBuilder<TProperty> ToJsonProperty<TProperty>(
            this ModelBuilderTest.TestPropertyBuilder<TProperty> builder,
            string name)
        {
            switch (builder)
            {
                case IInfrastructure<PropertyBuilder<TProperty>> genericBuilder:
                    genericBuilder.Instance.ToJsonProperty(name);
                    break;
                case IInfrastructure<PropertyBuilder> nonGenericBuilder:
                    nonGenericBuilder.Instance.ToJsonProperty(name);
                    break;
            }

            return builder;
        }
    }
}
