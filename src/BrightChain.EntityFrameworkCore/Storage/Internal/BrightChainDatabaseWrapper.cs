// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BrightChain.EntityFrameworkCore.Metadata.Internal;
using BrightChain.EntityFrameworkCore.Properties;
using BrightChain.EntityFrameworkCore.Update.Internal;
using BrightChain.EntityFrameworkCore.Utilities;
using BrightChain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace BrightChain.EntityFrameworkCore.Storage.Internal
{
    /// <summary>
    ///     <para>
    ///         This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///         the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///         any release. You should only use it directly in your code with extreme caution and knowing that
    ///         doing so can result in application failures when updating to a new Entity Framework Core release.
    ///     </para>
    ///     <para>
    ///         The service lifetime is <see cref="ServiceLifetime.Scoped" />. This means that each
    ///         <see cref="DbContext" /> instance will use its own instance of this service.
    ///         The implementation may depend on other services registered with any lifetime.
    ///         The implementation does not need to be thread-safe.
    ///     </para>
    /// </summary>
    public class BrightChainDatabaseWrapper : Microsoft.EntityFrameworkCore.Storage.Database
    {
        private readonly Dictionary<IEntityType, DocumentSource> _documentCollections = new();

        private readonly IBrightChainClientWrapper _brightChainClient;
        private readonly bool _sensitiveLoggingEnabled;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public BrightChainDatabaseWrapper(
            DatabaseDependencies dependencies,
            IBrightChainClientWrapper brightChainClient,
            ILoggingOptions loggingOptions)
            : base(dependencies)
        {
            _brightChainClient = brightChainClient;

            if (loggingOptions.IsSensitiveDataLoggingEnabled)
            {
                _sensitiveLoggingEnabled = true;
            }
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public override int SaveChanges(IList<IUpdateEntry> entries)
        {
            var rowsAffected = 0;
            var entriesSaved = new HashSet<IUpdateEntry>();
            var rootEntriesToSave = new HashSet<IUpdateEntry>();

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var entityType = entry.EntityType;

                Check.DebugAssert(!entityType.IsAbstract(), $"{entityType} is abstract");

                if (!entityType.IsDocumentRoot())
                {
#pragma warning disable EF1001 // Internal EF Core API usage.
                    // #16707
                    var root = GetRootDocument((InternalEntityEntry)entry);
#pragma warning restore EF1001 // Internal EF Core API usage.
                    if (!entriesSaved.Contains(root)
                        && rootEntriesToSave.Add(root)
                        && root.EntityState == EntityState.Unchanged)
                    {
#pragma warning disable EF1001 // Internal EF Core API usage.
                        // #16707
                        ((InternalEntityEntry)root).SetEntityState(EntityState.Modified);
#pragma warning restore EF1001 // Internal EF Core API usage.
                    }

                    continue;
                }

                entriesSaved.Add(entry);

                try
                {
                    if (Save(entry))
                    {
                        rowsAffected++;
                    }
                }
                catch (BrightChainException ex)
                {
                    throw ThrowUpdateException(ex, entry);
                }
            }

            foreach (var rootEntry in rootEntriesToSave)
            {
                if (!entriesSaved.Contains(rootEntry)
                    && Save(rootEntry))
                {
                    rowsAffected++;
                }
            }

            return rowsAffected;
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public override async Task<int> SaveChangesAsync(
            IList<IUpdateEntry> entries,
            CancellationToken cancellationToken = default)
        {
            var rowsAffected = 0;
            var entriesSaved = new HashSet<IUpdateEntry>();
            var rootEntriesToSave = new HashSet<IUpdateEntry>();

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var entityType = entry.EntityType;

                Check.DebugAssert(!entityType.IsAbstract(), $"{entityType} is abstract");

                if (!entityType.IsDocumentRoot())
                {
                    var root = GetRootDocument((InternalEntityEntry)entry);
                    if (!entriesSaved.Contains(root)
                        && rootEntriesToSave.Add(root)
                        && root.EntityState == EntityState.Unchanged)
                    {
#pragma warning disable EF1001 // Internal EF Core API usage.
                        // #16707
                        ((InternalEntityEntry)root).SetEntityState(EntityState.Modified);
#pragma warning restore EF1001 // Internal EF Core API usage.
                    }

                    continue;
                }

                entriesSaved.Add(entry);
                try
                {
                    if (await SaveAsync(entry, cancellationToken).ConfigureAwait(false))
                    {
                        rowsAffected++;
                    }
                }
                catch (BrightChainException ex)
                {
                    throw ThrowUpdateException(ex, entry);
                }
            }

            foreach (var rootEntry in rootEntriesToSave)
            {
                if (!entriesSaved.Contains(rootEntry)
                    && await SaveAsync(rootEntry, cancellationToken).ConfigureAwait(false))
                {
                    rowsAffected++;
                }
            }

            return rowsAffected;
        }

        private bool Save(IUpdateEntry entry)
        {
            var entityType = entry.EntityType;
            var documentSource = GetDocumentSource(entityType);
            var collectionId = documentSource.GetContainerId();
            var state = entry.EntityState;

            if (entry.SharedIdentityEntry != null)
            {
                if (entry.EntityState == EntityState.Deleted)
                {
                    return false;
                }

                if (state == EntityState.Added)
                {
                    state = EntityState.Modified;
                }
            }

            switch (state)
            {
                case EntityState.Added:
                    var newDocument = documentSource.GetCurrentDocument(entry);
                    if (newDocument != null)
                    {
                        documentSource.UpdateDocument(newDocument, entry);
                    }
                    else
                    {
                        newDocument = documentSource.CreateDocument(entry);
                    }

                    return _brightChainClient.CreateItem(collectionId, newDocument, entry);

                case EntityState.Modified:
                    var document = documentSource.GetCurrentDocument(entry);
                    if (document != null)
                    {
                        if (documentSource.UpdateDocument(document, entry) == null)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        document = documentSource.CreateDocument(entry);

                        var propertyName = entityType.FindDiscriminatorProperty()?.GetJsonPropertyName();
                        if (propertyName != null)
                        {
                            document[propertyName] = (JsonNode)entityType.GetDiscriminatorValue();
                        }
                    }

                    return _brightChainClient.ReplaceItem(
                        collectionId, documentSource.GetId(entry.SharedIdentityEntry ?? entry), document, entry);

                case EntityState.Deleted:
                    return _brightChainClient.DeleteItem(collectionId, documentSource.GetId(entry), entry);

                default:
                    return false;
            }
        }

        private Task<bool> SaveAsync(IUpdateEntry entry, CancellationToken cancellationToken)
        {
            var entityType = entry.EntityType;
            var documentSource = GetDocumentSource(entityType);
            var collectionId = documentSource.GetContainerId();
            var state = entry.EntityState;

            if (entry.SharedIdentityEntry != null)
            {
                if (entry.EntityState == EntityState.Deleted)
                {
                    return Task.FromResult(false);
                }

                if (state == EntityState.Added)
                {
                    state = EntityState.Modified;
                }
            }

            switch (state)
            {
                case EntityState.Added:
                    var newDocument = documentSource.GetCurrentDocument(entry);
                    if (newDocument != null)
                    {
                        documentSource.UpdateDocument(newDocument, entry);
                    }
                    else
                    {
                        newDocument = documentSource.CreateDocument(entry);
                    }

                    return _brightChainClient.CreateItemAsync(
                        collectionId, newDocument, entry, cancellationToken);

                case EntityState.Modified:
                    var document = documentSource.GetCurrentDocument(entry);
                    if (document != null)
                    {
                        if (documentSource.UpdateDocument(document, entry) == null)
                        {
                            return Task.FromResult(false);
                        }
                    }
                    else
                    {
                        document = documentSource.CreateDocument(entry);

                        var propertyName = entityType.FindDiscriminatorProperty()?.GetJsonPropertyName();
                        if (propertyName != null)
                        {
                            document[propertyName] = (JsonNode)entityType.GetDiscriminatorValue();
                        }
                    }

                    return _brightChainClient.ReplaceItemAsync(
                        collectionId,
                        documentSource.GetId(entry.SharedIdentityEntry ?? entry),
                        document,
                        entry,
                        cancellationToken);

                case EntityState.Deleted:
                    return _brightChainClient.DeleteItemAsync(
                        collectionId, documentSource.GetId(entry), entry, cancellationToken);

                default:
                    return Task.FromResult(false);
            }
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual DocumentSource GetDocumentSource(IEntityType entityType)
        {
            if (!_documentCollections.TryGetValue(entityType, out var documentSource))
            {
                _documentCollections.Add(
                    entityType, documentSource = new DocumentSource(entityType, this));
            }

            return documentSource;
        }

#pragma warning disable EF1001 // Internal EF Core API usage.
        // Issue #16707
        private IUpdateEntry GetRootDocument(InternalEntityEntry entry)
        {
            var stateManager = entry.StateManager;
            var ownership = entry.EntityType.FindOwnership()!;
            var principal = stateManager.FindPrincipal(entry, ownership);
            if (principal == null)
            {
                if (_sensitiveLoggingEnabled)
                {
                    throw new InvalidOperationException(
                        BrightChainStrings.OrphanedNestedDocumentSensitive(
                            entry.EntityType.DisplayName(),
                            ownership.PrincipalEntityType.DisplayName(),
                            entry.BuildCurrentValuesString(entry.EntityType.FindPrimaryKey()!.Properties)));
                }

                throw new InvalidOperationException(
                    BrightChainStrings.OrphanedNestedDocument(
                        entry.EntityType.DisplayName(),
                        ownership.PrincipalEntityType.DisplayName()));
            }

            return principal.EntityType.IsDocumentRoot() ? principal : GetRootDocument(principal);
        }
#pragma warning restore EF1001 // Internal EF Core API usage.

        private Exception ThrowUpdateException(BrightChainException exception, IUpdateEntry entry)
        {
            var documentSource = GetDocumentSource(entry.EntityType);
            var id = documentSource.GetId(entry.SharedIdentityEntry ?? entry);
            throw exception.StatusCode switch
            {
                HttpStatusCode.PreconditionFailed =>
                    new DbUpdateConcurrencyException(BrightChainStrings.UpdateConflict(id), exception, new[] { entry }),
                HttpStatusCode.Conflict =>
                    new DbUpdateException(BrightChainStrings.UpdateConflict(id), exception, new[] { entry }),
                _ => Rethrow(exception),
            };
        }

        private static Exception Rethrow(Exception ex)
        {
            // Re-throw an exception, preserving the original stack and details, without being in the original "catch" block.
            ExceptionDispatchInfo.Capture(ex).Throw();
            return ex;
        }
    }
}
