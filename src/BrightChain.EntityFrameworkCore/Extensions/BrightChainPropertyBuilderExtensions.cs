// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BrightChain.EntityFrameworkCore.Metadata.Internal;
using BrightChain.EntityFrameworkCore.Utilities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

// ReSharper disable once CheckNamespace
namespace BrightChain.EntityFrameworkCore
{
    /// <summary>
    ///     BrightChain-specific extension methods for <see cref="PropertyBuilder" />.
    /// </summary>
    public static class BrightChainPropertyBuilderExtensions
    {
        /// <summary>
        ///     Configures the property name that the property is mapped to when targeting Azure BrightChain.
        /// </summary>
        /// <remarks> If an empty string is supplied then the property will not be persisted. </remarks>
        /// <param name="propertyBuilder"> The builder for the property being configured. </param>
        /// <param name="name"> The name of the property. </param>
        /// <returns> The same builder instance so that multiple calls can be chained. </returns>
        public static PropertyBuilder ToJsonProperty(
            this PropertyBuilder propertyBuilder,
            string name)
        {
            Check.NotNull(propertyBuilder, nameof(propertyBuilder));
            Check.NotNull(name, nameof(name));

            propertyBuilder.Metadata.SetJsonPropertyName(name);

            return propertyBuilder;
        }

        /// <summary>
        ///     Configures the property name that the property is mapped to when targeting Azure BrightChain.
        /// </summary>
        /// <typeparam name="TProperty"> The type of the property being configured. </typeparam>
        /// <param name="propertyBuilder"> The builder for the property being configured. </param>
        /// <param name="name"> The name of the property. </param>
        /// <returns> The same builder instance so that multiple calls can be chained. </returns>
        public static PropertyBuilder<TProperty> ToJsonProperty<TProperty>(
            this PropertyBuilder<TProperty> propertyBuilder,
            string name)
        {
            return (PropertyBuilder<TProperty>)ToJsonProperty((PropertyBuilder)propertyBuilder, name);
        }

        /// <summary>
        ///     <para>
        ///         Configures the property name that the property is mapped to when targeting Azure BrightChain.
        ///     </para>
        ///     <para>
        ///         If an empty string is supplied then the property will not be persisted.
        ///     </para>
        /// </summary>
        /// <param name="propertyBuilder"> The builder for the property being configured. </param>
        /// <param name="name"> The name of the property. </param>
        /// <param name="fromDataAnnotation"> Indicates whether the configuration was specified using a data annotation. </param>
        /// <returns>
        ///     The same builder instance if the configuration was applied,
        ///     <see langword="null" /> otherwise.
        /// </returns>
        public static IConventionPropertyBuilder? ToJsonProperty(
            this IConventionPropertyBuilder propertyBuilder,
            string? name,
            bool fromDataAnnotation = false)
        {
            if (!propertyBuilder.CanSetJsonProperty(name, fromDataAnnotation))
            {
                return null;
            }

            propertyBuilder.Metadata.SetJsonPropertyName(name, fromDataAnnotation);

            return propertyBuilder;
        }

        /// <summary>
        ///     Returns a value indicating whether the given property name can be set.
        /// </summary>
        /// <param name="propertyBuilder"> The builder for the property being configured. </param>
        /// <param name="name"> The name of the property. </param>
        /// <param name="fromDataAnnotation"> Indicates whether the configuration was specified using a data annotation. </param>
        /// <returns> <see langword="true" /> if the property name can be set. </returns>
        public static bool CanSetJsonProperty(
            this IConventionPropertyBuilder propertyBuilder,
            string? name,
            bool fromDataAnnotation = false)
        {
            return propertyBuilder.CanSetAnnotation(BrightChainAnnotationNames.PropertyName, name, fromDataAnnotation);
        }

        /// <summary>
        ///     Configures this property to be the etag concurrency token.
        /// </summary>
        /// <param name="propertyBuilder"> The builder for the property being configured. </param>
        /// <returns> The same builder instance so that multiple calls can be chained. </returns>
        public static PropertyBuilder IsETagConcurrency(this PropertyBuilder propertyBuilder)
        {
            Check.NotNull(propertyBuilder, nameof(propertyBuilder));
            propertyBuilder
                .IsConcurrencyToken()
                .ToJsonProperty("_etag")
                .ValueGeneratedOnAddOrUpdate();
            return propertyBuilder;
        }

        /// <summary>
        ///     Configures this property to be the etag concurrency token.
        /// </summary>
        /// <typeparam name="TProperty"> The type of the property being configured. </typeparam>
        /// <param name="propertyBuilder"> The builder for the property being configured. </param>
        /// <returns> The same builder instance so that multiple calls can be chained. </returns>
        public static PropertyBuilder<TProperty> IsETagConcurrency<TProperty>(
            this PropertyBuilder<TProperty> propertyBuilder)
        {
            return (PropertyBuilder<TProperty>)IsETagConcurrency((PropertyBuilder)propertyBuilder);
        }
    }
}
