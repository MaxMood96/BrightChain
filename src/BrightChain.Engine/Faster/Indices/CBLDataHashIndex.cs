﻿namespace BrightChain.Engine.Faster.Indices
{
    using BrightChain.Engine.Models.Hashes;

    public class CBLDataHashIndex : BrightChainIndexValue
    {
        public readonly DataHash DataHash;

        public CBLDataHashIndex(DataHash dataHash)
            : base(data: Serialize<DataHash>(dataHash))
        {
            this.DataHash = dataHash;
        }
    }
}