﻿using System;
using System.Linq;
using BrightChain.Engine.Models.Blocks;
using BrightChain.Engine.Models.Blocks.DataObjects;

namespace BrightChain.Engine.Services
{
    /// <summary>
    /// This tiny little class is actually the lynchpin of the owner free filesystem.
    /// It is responsible for XORing blocks with random data blocks in order to correlate
    /// the user data with random data and other user blocks.
    /// </summary>
    public class BlockBrightener
    {
        public static byte TupleCount { get; } = 5;

        private readonly MemoryBlockCacheManager pregeneratedRandomizerCache;

        public BlockBrightener(MemoryBlockCacheManager pregeneratedRandomizerCache)
        {
            this.pregeneratedRandomizerCache = pregeneratedRandomizerCache;
        }

        /// <summary>
        /// Brightening is the process of XORing to correlate the data with block of random data and make it appear more random or "white" as white light is broad multi-spectrum light.
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public BrightenedBlock Brighten(SourceBlock block, out Block[] randomizersUsed)
        {
            // the incoming block should be a raw disk block and is never used again
            randomizersUsed = new Block[TupleCount - 1];
            for (int i = 0; i < randomizersUsed.Length; i++)
            {
                // TODO: select or generate pre-generated random blocks (determine mixing)
                // for now just generate on demand, but these can be pre-seeded
                randomizersUsed[i] = new RandomizerBlock(
                    new TransactableBlockParams(
                    cacheManager: this.pregeneratedRandomizerCache,
                    allowCommit: true,
                    blockParams: new BlockParams(
                        blockSize: block.BlockSize,
                        requestTime: DateTime.Now,
                        keepUntilAtLeast: block.StorageContract.KeepUntilAtLeast,
                        redundancy: block.RedundancyContract.RedundancyContractType,
                        privateEncrypted: false)));
            }

            var xorBlock = block.XOR(randomizersUsed);
            return new BrightenedBlock(
                blockParams: xorBlock.BlockParams,
                data: xorBlock.Data,
                constituentBlocks: randomizersUsed.Select(b => b.Id).ToArray());
        }
    }
}
