namespace BinStorage.Index
{
    internal struct BlockInfo
    {
        public static readonly int Size = sizeof(long) + sizeof(int);
        public static BlockInfo Empty => new BlockInfo();

        public long Offset;

        public int Length;
    }
}