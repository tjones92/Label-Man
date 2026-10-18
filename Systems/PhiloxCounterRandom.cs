/// <summary>Philox4x32-10: stateless random blocks with explicit counter and key.</summary>
internal static class PhiloxCounterRandom {
    // Published Philox constants/round mapping, Salmon et al. (SC11).
    // Reference vectors: github.com/DEShawResearch/random123/tests/kat_vectors.
    internal static (uint A, uint B, uint C, uint D) Block(uint a, uint b, uint c, uint d, uint key0, uint key1) {
        unchecked {
            for (int round = 0; round < 10; round++) {
                ulong left = 0xD2511F53UL * a, right = 0xCD9E8D57UL * c;
                (a, b, c, d) = ((uint)(right >> 32) ^ b ^ key0, (uint)right,
                    (uint)(left >> 32) ^ d ^ key1, (uint)left);
                key0 += 0x9E3779B9U; key1 += 0xBB67AE85U;
            }
            return (a, b, c, d);
        }
    }
    // Separate fields occupy separate counter words. IDs must retain their meaning
    // between experiments; list positions and iteration order are never inputs.
    internal static double Unit(ulong seed, string entityId, string regionId, int week, uint decision) {
        ulong entity = Identity(entityId), key = seed ^ Identity(regionId);
        var block = Block((uint)entity, (uint)(entity >> 32), unchecked((uint)week), decision, (uint)key, (uint)(key >> 32));
        return (((ulong)block.A << 21) | (block.B >> 11)) * (1.0 / 9007199254740992.0);
    }
    private static ulong Identity(string value) {
        ulong hash = 14695981039346656037UL;
        foreach (char character in value) { hash ^= character; hash = unchecked(hash * 1099511628211UL); }
        return hash;
    }
}
