using BrightChain.Enumerations;
using BrightChain.Helpers;
using BrightChain.Services;
using System;

namespace BrightChain.Models.Blocks
{
    /// <summary>
    /// Input blocks to the whitener service that consist of purely CSPRNG data of the specified block size
    /// </summary>
    public class RandomizerBlock : TransactableBlock, IComparable<RandomizerBlock>
    {
        public RandomizerBlock(BlockCacheManager pregeneratedRandomizerCache, BlockSize blockSize, DateTime requestTime, DateTime keepUntilAtLeast, RedundancyContractType redundancy, bool allowCommit) :
            base(
                cacheManager: pregeneratedRandomizerCache,
                blockSize: blockSize,
                requestTime: requestTime,
                keepUntilAtLeast: keepUntilAtLeast,
                redundancy: redundancy,
                data: RandomDataHelper.RandomReadOnlyBytes(BlockSizeMap.BlockSize(blockSize)),
                allowCommit: allowCommit) => this.CacheManager.Set(this.Id, this);
        /// <summary>
        /// replace incoming data (will be empty byte array to fit conventions) with random data
        /// </summary>
        /// <param name="requestTime"></param>
        /// <param name="keepUntilAtLeast"></param>
        /// <param name="redundancy"></param>
        /// <param name="_"></param>
        /// <param name="allowCommit"></param>
        /// <returns></returns>
        public override Block NewBlock(DateTime requestTime, DateTime keepUntilAtLeast, RedundancyContractType redundancy, ReadOnlyMemory<byte> _, bool allowCommit) => new RandomizerBlock(
pregeneratedRandomizerCache: this.CacheManager as BlockCacheManager,
blockSize: this.BlockSize,
requestTime: requestTime,
keepUntilAtLeast: keepUntilAtLeast,
redundancy: redundancy,
allowCommit: allowCommit);

        public int CompareTo(RandomizerBlock other) => ReadOnlyMemoryComparer<byte>.Compare(this.Data, other.Data);

        public override void Dispose()
        {

        }
    }
}