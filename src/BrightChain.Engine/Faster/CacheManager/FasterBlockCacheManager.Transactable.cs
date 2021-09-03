﻿namespace BrightChain.Engine.Faster.CacheManager
{
    using System;
    using BrightChain.Engine.Exceptions;
    using BrightChain.Engine.Faster;
    using BrightChain.Engine.Faster.Enumerations;
    using FASTER.core;

    public partial class FasterBlockCacheManager
    {
        private BlockSessionCheckpoint lastCheckpoint;
        private BlockSessionAddresses lastAddresses;

        public BlockSessionCheckpoint TakeFullCheckpoint(CheckpointType checkpointType = CheckpointType.Snapshot)
        {
            return this.CheckpointFunc(FasterCheckpointOperation.Full, checkpointType);
        }

        private async Task<BlockSessionCheckpoint> TakeFullCheckpointAsync(CheckpointType checkpointType = CheckpointType.Snapshot)
        {
            return await this.CheckpointFuncAsync(() => new Dictionary<CacheStoreType, Task<(bool, Guid)>>()
            {
                { CacheStoreType.PrimaryMetadata, this.primaryMetadataKV.TakeFullCheckpointAsync(checkpointType: checkpointType).AsTask() },
                { CacheStoreType.PrimaryData, this.primaryDataKV.TakeFullCheckpointAsync(checkpointType: checkpointType).AsTask() },
                { CacheStoreType.PrimaryExpiration, this.primaryExpirationKV.TakeFullCheckpointAsync(checkpointType: checkpointType).AsTask() },
                { CacheStoreType.CBLIndices, this.cblIndicesKV.TakeFullCheckpointAsync(checkpointType: checkpointType).AsTask() },
            }).ConfigureAwait(false);
        }

        public BlockSessionCheckpoint TakeHybridCheckpoint(CheckpointType checkpointType)
        {
            return this.CheckpointFunc(FasterCheckpointOperation.Hybrid, checkpointType);
        }

        public async Task<BlockSessionCheckpoint> TakeHybridCheckpointAsync(CheckpointType checkpointType = CheckpointType.Snapshot)
        {
            return await this.CheckpointFuncAsync(() => new Dictionary<CacheStoreType, Task<(bool, Guid)>>()
            {
                { CacheStoreType.PrimaryMetadata, this.primaryMetadataKV.TakeHybridLogCheckpointAsync(checkpointType: checkpointType).AsTask() },
                { CacheStoreType.PrimaryData, this.primaryDataKV.TakeHybridLogCheckpointAsync(checkpointType: checkpointType).AsTask() },
                { CacheStoreType.PrimaryExpiration, this.primaryExpirationKV.TakeHybridLogCheckpointAsync(checkpointType: checkpointType).AsTask() },
                { CacheStoreType.CBLIndices, this.cblIndicesKV.TakeHybridLogCheckpointAsync(checkpointType: checkpointType).AsTask() },
            }).ConfigureAwait(false);
        }

        public BlockSessionCheckpoint TakeIndexCheckpoint()
        {
            return this.CheckpointFunc(FasterCheckpointOperation.Index);
        }

        public async Task<BlockSessionCheckpoint> TakeIndexCheckPointAsync()
        {
            return await this.CheckpointFuncAsync(() => new Dictionary<CacheStoreType, Task<(bool, Guid)>>()
            {
                { CacheStoreType.PrimaryMetadata, this.primaryMetadataKV.TakeIndexCheckpointAsync().AsTask() },
                { CacheStoreType.PrimaryData, this.primaryDataKV.TakeIndexCheckpointAsync().AsTask() },
                { CacheStoreType.PrimaryExpiration, this.primaryExpirationKV.TakeIndexCheckpointAsync().AsTask() },
                { CacheStoreType.CBLIndices, this.cblIndicesKV.TakeIndexCheckpointAsync().AsTask() },
            }).ConfigureAwait(false);
        }

        public BlockSessionCheckpoint CheckpointFunc(FasterCheckpointOperation operation, CheckpointType checkpointType = CheckpointType.Snapshot)
        {
            bool metadataResult, dataResult, expirationResult, cblResult, cblCorrelationResult;
            Guid metadataToken, dataToken, expirationToken, cblToken, cblCorrelationsToken;
            switch (operation)
            {
                case FasterCheckpointOperation.Full:
                    metadataResult = this.primaryMetadataKV.TakeFullCheckpoint(token: out metadataToken, checkpointType: checkpointType);
                    dataResult = this.primaryDataKV.TakeFullCheckpoint(token: out dataToken, checkpointType: checkpointType);
                    expirationResult = this.primaryExpirationKV.TakeFullCheckpoint(token: out expirationToken, checkpointType: checkpointType);
                    cblCorrelationResult = this.cblIndicesKV.TakeFullCheckpoint(token: out cblCorrelationsToken, checkpointType: checkpointType);
                    break;
                case FasterCheckpointOperation.Hybrid:
                    metadataResult = this.primaryMetadataKV.TakeHybridLogCheckpoint(out metadataToken);
                    dataResult = this.primaryDataKV.TakeHybridLogCheckpoint(out dataToken);
                    expirationResult = this.primaryExpirationKV.TakeHybridLogCheckpoint(token: out expirationToken, checkpointType: checkpointType);
                    cblCorrelationResult = this.cblIndicesKV.TakeHybridLogCheckpoint(out cblCorrelationsToken);
                    break;
                case FasterCheckpointOperation.Index:
                    metadataResult = this.primaryMetadataKV.TakeIndexCheckpoint(out metadataToken);
                    dataResult = this.primaryDataKV.TakeIndexCheckpoint(out dataToken);
                    expirationResult = this.primaryExpirationKV.TakeIndexCheckpoint(token: out expirationToken);
                    cblCorrelationResult = this.cblIndicesKV.TakeIndexCheckpoint(out cblCorrelationsToken);
                    break;
                default:
                    throw new BrightChainExceptionImpossible("Unexpected type");
            }

            return new BlockSessionCheckpoint(
                success: metadataResult && dataResult && expirationResult && cblCorrelationResult,
                results: new Dictionary<CacheStoreType, bool>()
                    {
                    { CacheStoreType.PrimaryMetadata, metadataResult },
                    { CacheStoreType.PrimaryData, dataResult },
                    { CacheStoreType.PrimaryExpiration, expirationResult },
                    { CacheStoreType.CBLIndices, cblCorrelationResult },
                    },
                guids: new Dictionary<CacheStoreType, Guid>()
                    {
                    { CacheStoreType.PrimaryMetadata, metadataToken },
                    { CacheStoreType.PrimaryData, dataToken },
                    { CacheStoreType.PrimaryExpiration, expirationToken },
                    { CacheStoreType.CBLIndices, cblCorrelationsToken },
                    });
        }

        public async Task<BlockSessionCheckpoint> CheckpointFuncAsync(Func<Dictionary<CacheStoreType, Task<(bool, Guid)>>> func)
        {
            var taskDict = func();

            await Task.WhenAll(taskDict.Values)
                .ConfigureAwait(false);

            var resultDict = new Dictionary<CacheStoreType, bool>();
            var guidDict = new Dictionary<CacheStoreType, Guid>();
            var allGood = true;
            foreach (var task in taskDict)
            {
                var result = task.Value.Result;
                resultDict.Add(task.Key, result.Item1);
                guidDict.Add(task.Key, result.Item2);

                allGood = allGood && result.Item1;
            }

            return new BlockSessionCheckpoint(allGood, resultDict, guidDict);
        }

        public async Task CompleteCheckpointAsync()
        {
            await Task
                .WhenAll(new Task[]
                    {
                        this.primaryMetadataKV.CompleteCheckpointAsync().AsTask(),
                        this.primaryDataKV.CompleteCheckpointAsync().AsTask(),
                        this.primaryExpirationKV.CompleteCheckpointAsync().AsTask(),
                        this.cblIndicesKV.CompleteCheckpointAsync().AsTask(),
                    })
                .ConfigureAwait(false);
        }

        public BlockSessionAddresses NextSerials()
        {
            return new BlockSessionAddresses(addresses: new Dictionary<CacheStoreType, long>
            {
                {
                    CacheStoreType.PrimaryMetadata,
                    this.sessionContext.MetadataSession.NextSerialNo
                },
                {
                    CacheStoreType.PrimaryData,
                    this.sessionContext.DataSession.NextSerialNo
                },
                {
                    CacheStoreType.PrimaryExpiration,
                    this.sessionContext.ExpirationSession.NextSerialNo
                },
                {
                    CacheStoreType.CBLIndices,
                    this.sessionContext.CblIndicesSession.NextSerialNo
                },
            });
        }

        public BlockSessionAddresses HeadAddresses()
        {
            return new BlockSessionAddresses(addresses: new Dictionary<CacheStoreType, long>
            {
                {
                    CacheStoreType.PrimaryMetadata,
                    this.primaryMetadataKV.Log.HeadAddress
                },
                {
                    CacheStoreType.PrimaryData,
                    this.primaryDataKV.Log.HeadAddress
                },
                {
                    CacheStoreType.PrimaryExpiration,
                    this.primaryExpirationKV.Log.HeadAddress
                },
                {
                    CacheStoreType.CBLIndices,
                    this.cblIndicesKV.Log.HeadAddress
                },
            });
        }

        public BlockSessionAddresses Compact(bool shiftBeginAddress = true)
        {
            return new BlockSessionAddresses(addresses: new Dictionary<CacheStoreType, long>
            {
                {
                    CacheStoreType.PrimaryMetadata,
                    this.sessionContext.MetadataSession.Compact(
                        untilAddress: this.primaryMetadataKV.Log.HeadAddress,
                        shiftBeginAddress: shiftBeginAddress)
                },
                {
                    CacheStoreType.PrimaryData,
                    this.sessionContext.DataSession.Compact(
                        untilAddress: this.primaryDataKV.Log.HeadAddress,
                        shiftBeginAddress: shiftBeginAddress)
                },
                {
                    CacheStoreType.PrimaryExpiration,
                    this.sessionContext.ExpirationSession.Compact(
                        untilAddress: this.primaryExpirationKV.Log.HeadAddress,
                        shiftBeginAddress: shiftBeginAddress)
                },
                {
                    CacheStoreType.CBLIndices,
                    this.sessionContext.CblIndicesSession.Compact(
                        untilAddress: this.cblIndicesKV.Log.HeadAddress,
                        shiftBeginAddress: shiftBeginAddress)
                },
            });
        }

        public async void Recover()
        {
            Task.WaitAll(new Task[]
            {
                this.primaryMetadataKV.RecoverAsync().AsTask(),
                this.primaryDataKV.RecoverAsync().AsTask(),
                this.primaryExpirationKV.RecoverAsync().AsTask(),
                this.cblIndicesKV.RecoverAsync().AsTask(),
            });
        }
    }
}
