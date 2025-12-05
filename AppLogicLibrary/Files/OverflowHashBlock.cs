using SEM_2_CORE.Interfaces;

namespace SEM_2_CORE.Files;

public class OverflowHashBlock<T> : Block<T> where T : IDataClassOperations<T>, IByteOperations
{
    public int NextBlockIndex { get; set; } = -1;
    public OverflowHashBlock(int blockFactor, T dataInstance, bool newBlock = false, bool emptyBlock = false) : base(blockFactor, dataInstance, newBlock, emptyBlock)
    {
    }

    public OverflowHashBlock() : base()
    {
    }

    public override int GetSize()
    {
        return base.GetSize() + sizeof(int);  // pridáme veľkosť jedného intu pre index do ďalšieho bloku zo zreťazenia
    }

    public override byte[] GetBytes()  // pridané byty pre index do preplňovacieho súboru
    {
        List<byte> byteBuffer = new List<byte>(base.GetBytes());
        byteBuffer.AddRange(BitConverter.GetBytes(NextBlockIndex));

        return byteBuffer.ToArray();
    }

    public override void FromBytes(byte[] bytes)  // načítanie do predka a pridanie atribútu pre tohto potomka
    {
        base.FromBytes(bytes);
        NextBlockIndex = BitConverter.ToInt32(bytes.AsSpan(base.GetSize(), 4).ToArray());
    }
}