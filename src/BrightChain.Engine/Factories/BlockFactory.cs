﻿namespace BrightChain.Engine.Factories
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using BrightChain.Engine.Extensions;
    using BrightChain.Engine.Models.Blocks;
    using BrightChain.Engine.Models.Blocks.Chains;
    using BrightChain.Engine.Models.Blocks.DataObjects;
    using BrightChain.Engine.Services;

    /// <summary>
    /// The block factory will upcast the deserialized blocks from storage into the correct types
    /// </summary>
    public static class BlockFactory
    {
        public static Type TypeFrom(ReadOnlyMemory<byte> metadataBytes)
        {
            var jsonString = new string(metadataBytes.ToArray().Select(c => (char)c).ToArray());
            try
            {
                object metaDataObject = JsonSerializer.Deserialize(jsonString, typeof(Dictionary<string, object>), new JsonSerializerOptions
                {
                    WriteIndented = false,
                    Converters =
                {
                    new HashJsonFactory(),
                },
                });

                Dictionary<string, object> metadataDictionary = (Dictionary<string, object>)metaDataObject;

                return Type.GetType(((JsonElement)metadataDictionary["_t"]).ToObject<string>());
            }
            catch (Exception _)
            {
                return null;
            }
        }

        public static TransactableBlock ConvertRestored(RestoredBlock block, BlockCacheManager cacheManager)
        {
            var originalType = Type.GetType(block.OriginalType);
            if (originalType.IsAssignableFrom(typeof(global::BrightChain.Engine.Models.Blocks.Chains.BrightChain)))
            {
                throw new NotImplementedException();
            }
            else if (originalType.IsAssignableFrom(typeof(global::BrightChain.Engine.Models.Blocks.Chains.SuperConstituentBlockListBlock)))
            {
                throw new NotImplementedException();
            }
            else if (originalType.IsAssignableFrom(typeof(global::BrightChain.Engine.Models.Blocks.Chains.ConstituentBlockListBlock)))
            {
                throw new NotImplementedException();
            }
            else if (originalType.IsGenericType)
            {
                Type chainLinqType = typeof(ChainLinqObjectBlock<>).GetGenericTypeDefinition();
                if (originalType.GetGenericTypeDefinition().Equals(chainLinqType))
                {
                    var p = new object[] { new ChainLinqBlockParams(block.BlockParams), block.Data };
                    object i = Activator.CreateInstance(originalType, p);
                    return (TransactableBlock)i;
                }
            }

            return block.MakeTransactable(
                cacheManager: cacheManager,
                allowCommit: true);
        }
    }
}
