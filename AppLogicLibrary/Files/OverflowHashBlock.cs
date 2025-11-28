using SEM_2_CORE.Interfaces;

namespace SEM_2_CORE.Files;

public class OverflowHashBlock<T> : Block<T> where T : IDataClassOperations<T>, IByteOperations
{
    public int NextBlockIndex { get; set; } = -1;
    public OverflowHashBlock(int blockSize, T dataInstance) : base(blockSize, dataInstance)
    {
    }

    public override int GetSize()
    {
        return base.GetSize() + sizeof(int);  // pridáme veľkosť jedného intu pre index do ďalšieho bloku zo zreťazenia
    }

    public override byte[] GetBytes()  // pridané byty pre 2 atribúty v tomto potomkovi, posledný je index do preplňovacieho súboru
    {
        List<byte> byteBuffer = new List<byte>(base.GetBytes());
        byteBuffer.AddRange(BitConverter.GetBytes(NextBlockIndex));

        return byteBuffer.ToArray();
    }

    public override void FromBytes(byte[] bytes)  // načítanie do predka a pridanie atribútov pre tohto potomka
    {
        base.FromBytes(bytes);
        NextBlockIndex = BitConverter.ToInt32(bytes.AsSpan(base.GetSize(), 4).ToArray());
    }
}