// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BrightChain.EntityFrameworkCore.Metadata.Internal;
using BrightChain.EntityFrameworkCore.Utilities;
using Microsoft.EntityFrameworkCore.Metadata;

// ReSharper disable once CheckNamespace
namespace BrightChain.EntityFrameworkCore
{
    /// <summary>
    ///     Model extension methods for BrightChain metadata.
    /// </summary>
    public static class BrightChainModelExtensions
    {
        /// <summary>
        ///     Returns the default container name.
        /// </summary>
        /// <param name="model"> The model. </param>
        /// <returns> The default container name. </returns>
        public static string? GetDefaultContainer(this IReadOnlyModel model)
        {
            return (string?)model[BrightChainAnnotationNames.ContainerName];
        }

        /// <summary>
        ///     Sets the default container name.
        /// </summary>
        /// <param name="model"> The model. </param>
        /// <param name="name"> The name to set. </param>
        public static void SetDefaultContainer(this IMutableModel model, string? name)
        {
            model.SetOrRemoveAnnotation(
                           BrightChainAnnotationNames.ContainerName,
                           Check.NullButNotEmpty(name, nameof(name)));
        }

        /// <summary>
        ///     Sets the default container name.
        /// </summary>
        /// <param name="model"> The model. </param>
        /// <param name="name"> The name to set. </param>
        /// <param name="fromDataAnnotation"> Indicates whether the configuration was specified using a data annotation. </param>
        /// <returns> The configured value. </returns>
        public static string? SetDefaultContainer(
            this IConventionModel model,
            string? name,
            bool fromDataAnnotation = false)
        {
            model.SetOrRemoveAnnotation(
                BrightChainAnnotationNames.ContainerName,
                Check.NullButNotEmpty(name, nameof(name)),
                fromDataAnnotation);

            return name;
        }

        /// <summary>
        ///     Returns the configuration source for the default container name.
        /// </summary>
        /// <param name="model"> The model. </param>
        /// <returns> The configuration source for the default container name.</returns>
        public static ConfigurationSource? GetDefaultContainerConfigurationSource(this IConventionModel model)
        {
            return model.FindAnnotation(BrightChainAnnotationNames.ContainerName)?.GetConfigurationSource();
        }
    }
}
