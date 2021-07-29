﻿namespace BrightChain.Engine.Models.Blocks
{
    using System;
    using BrightChain.Engine.Attributes;
    using BrightChain.Engine.Enumerations;
    using BrightChain.Engine.Exceptions;
    using BrightChain.Engine.Interfaces;
    using BrightChain.Engine.Models.Blocks.DataObjects;
    using BrightChain.Engine.Services;

    /// <summary>
    /// The root block is the key / control node for the cache. Everything gets signed from here.
    /// There can only be one.
    /// </summary>
    public class RootBlock : TransactableBlock, IBlock, IComparable<IBlock>
    {
        public RootBlock(Guid databaseGuid, BlockSize blockSize = BlockSize.Large)
            : base(
                blockParams: new TransactableBlockParams(
                    cacheManager: null,
                    allowCommit: true,
                    blockParams: new BlockParams(
                        blockSize: blockSize,
                        requestTime: DateTime.Now,
                        keepUntilAtLeast: DateTime.MaxValue,
                        redundancy: RedundancyContractType.HeapHighPriority,
                        privateEncrypted: false)),
                data: Helpers.RandomDataHelper.DataFiller(default(ReadOnlyMemory<byte>), blockSize))
        {
            this.Guid = databaseGuid;
        }

        [BrightChainMetadata]
        public Guid Guid { get; set; }
    }
}