namespace BrightChain.Engine.Models.Blocks
{
    using System;
    using BrightChain.Engine.Exceptions;
    using BrightChain.Engine.Interfaces;
    using BrightChain.Engine.Models.Blocks.DataObjects;
    using BrightChain.Engine.Services;

    /// <summary>
    /// Block that can be contained in a MemoryBlockCacheManager / Btree
    /// </summary>
    public class MemoryBlock : TransactableBlock, IBlock
    {
        public MemoryBlock(TransactableBlockParams blockParams, ReadOnlyMemory<byte> data)
            : base(
                blockParams: blockParams,
                data: data)
        {
            if (!(this.CacheManager is MemoryDictionaryBlockCacheManager))
            {
                throw new BrightChainException(this.CacheManager.GetType().Name);
            }

            this.CacheManager.Set(this);
            this.OriginalType = typeof(MemoryBlock).AssemblyQualifiedName;
        }

        public override MemoryBlock NewBlock(BlockParams blockParams, ReadOnlyMemory<byte> data)
        {
            return new MemoryBlock(
                blockParams: new TransactableBlockParams(
                    cacheManager: this.CacheManager,
                    allowCommit: this.AllowCommit,
                    blockParams: blockParams),
                data: data);
        }

        public override void Dispose()
        {
        }
    }
}
