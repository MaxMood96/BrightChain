using System;
using BrightChain.Engine.Enumerations;
using BrightChain.Engine.Exceptions;
using BrightChain.Engine.Helpers;
using BrightChain.Engine.Interfaces;
using BrightChain.Engine.Models.Blocks.DataObjects;
using BrightChain.Engine.Services;

namespace BrightChain.Engine.Models.Blocks
{
    /// <summary>
    /// Block that is able to be stored, rolled back, committed, or prevented from being stored.
    /// TODO: Currently heavily associated with underlying BPlusTree. Abstract
    /// TODO: base off TransactedCompoundFile?
    /// </summary>
    public class TransactableBlock : Block, IDisposable, ITransactable, ITransactableBlock, IComparable<TransactableBlock>, IComparable<ITransactableBlock>
    {

        public TransactableBlock(BlockCacheManager cacheManager, RestoredBlock sourceBlock, bool allowCommit)
            : base(
                blockParams: new BlockParams(
                    blockSize: sourceBlock.BlockSize,
                    requestTime: sourceBlock.StorageContract.RequestTime,
                    keepUntilAtLeast: sourceBlock.StorageContract.KeepUntilAtLeast,
                    redundancy: sourceBlock.RedundancyContract.RedundancyContractType,
                    privateEncrypted: sourceBlock.Revokable),
                data: sourceBlock.Data)
        {
            CacheManager = cacheManager;
        }

        public TransactableBlock(TransactableBlockParams blockParams, ReadOnlyMemory<byte> data) :
            base(
                blockParams: new BlockParams(
                    blockSize: blockParams.BlockSize,
                    requestTime: blockParams.RequestTime,
                    keepUntilAtLeast: blockParams.KeepUntilAtLeast,
                    redundancy: blockParams.Redundancy,
                    privateEncrypted: blockParams.PrivateEncrypted),
                data: data)
        {
            CacheManager = blockParams.CacheManager;
            disposedValue = false;
        }

        /// <summary>
        /// For test methods
        /// </summary>
        internal TransactableBlock() : base(
            blockParams: new BlockParams(
                blockSize: BlockSize.Message,
                requestTime: DateTime.Now,
                keepUntilAtLeast: DateTime.MaxValue,
                redundancy: RedundancyContractType.HeapAuto,
                privateEncrypted: false),
            data: new ReadOnlyMemory<byte>() { })
        {
        }

        /// <summary>
        /// Gets a bool indicating whether the block's data has been loaded from the attached cache, or kept after persisting to cache.
        /// </summary>
        public bool DataInMemory { get; }

        /// <summary>
        /// Boolean indicating whether our data has been disposed.
        /// </summary>
        private bool disposedValue;
        public ICacheManager<BlockHash, TransactableBlock> CacheManager { get; internal set; }
        public bool Committed { get; protected set; } = false;
        public bool AllowCommit { get; protected set; } = false;

        public void SetCacheManager(ICacheManager<BlockHash, TransactableBlock> cacheManager)
        {
            CacheManager = cacheManager;
        }

        public static bool operator ==(TransactableBlock a, TransactableBlock b) =>
            a.BlockSize == b.BlockSize && ReadOnlyMemoryComparer<byte>.Compare(a.Data, b.Data) == 0;

        public static bool operator !=(TransactableBlock a, TransactableBlock b)
        {
            return !a.Equals(b);
        }

        public void Commit()
        {
            if (!AllowCommit)
            {
                throw new BrightChainException("Block is not allowed to be committed");
            }

            Committed = true;
        }

        public void Rollback()
        {
            Committed = false;
        }

        public override TransactableBlock NewBlock(BlockParams blockParams, ReadOnlyMemory<byte> data)
        {
            return new TransactableBlock(
                blockParams: new TransactableBlockParams(
                    cacheManager: CacheManager,
                    allowCommit: this.AllowCommit,
                    blockParams: this.AsBlock.BlockParams),
                data: data);
        }

        public override bool Equals(object obj)
        {
            return obj is Block block ? block.BlockSize == this.BlockSize && ReadOnlyMemoryComparer<byte>.Compare(this.Data, block.Data) == 0 : false;
        }

        public override int GetHashCode()
        {
            return this.Data.GetHashCode();
        }

        public int CompareTo(TransactableBlock other)
        {
            return other.BlockSize == this.BlockSize ? ReadOnlyMemoryComparer<byte>.Compare(this.Data, other.Data) : (other.Data.Length > this.Data.Length ? -1 : 1);
        }

        public int CompareTo(ITransactableBlock other)
        {
            return other.BlockSize == this.BlockSize ? ReadOnlyMemoryComparer<byte>.Compare(Data, other.Data) : other.Data.Length > this.Data.Length ? -1 : 1;
        }

        public override TransactableBlockParams BlockParams
        {
            get => new TransactableBlockParams(
                cacheManager: this.CacheManager,
                allowCommit: this.AllowCommit,
                blockParams: new BlockParams(
                    blockSize: this.BlockSize,
                    requestTime: this.StorageContract.RequestTime,
                    keepUntilAtLeast: this.StorageContract.KeepUntilAtLeast,
                    redundancy: this.RedundancyContract.RedundancyContractType,
                    privateEncrypted: false));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                Rollback();
                Data = null;

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~TransactableBlock()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public override void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
