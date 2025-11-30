using SEM_2_CORE.Interfaces;
namespace SEM_2_CORE.Files;

public class PrimaryHashBlock<T> : Block<T> where T : IDataClassOperations<T>, IByteOperations
{
    public int NextBlockIndex { get; set; } = -1;
    public uint TotalRecordsCount { get; set; } = 0;
    public PrimaryHashBlock(int blockFactor, T dataInstance, bool newBlock = false, bool emptyBlock = false) : base(blockFactor, dataInstance, newBlock, emptyBlock)
    {
    }

    public PrimaryHashBlock() : base()
    {
    }

    public override int GetSize()
    {
        return base.GetSize() + sizeof(int) + sizeof(uint);  // pridáme 2 int size pre index zreťazenia na prvý preplňovací blok a celkový počet záznamov aj v preplňovacom súbore
    }

    public override byte[] GetBytes()  // pridané byty pre 2 atribúty v tomto potomkovi, posledný je index do preplňovacieho súboru
    {
        List<byte> byteBuffer = new List<byte>(base.GetBytes());
        byteBuffer.AddRange(BitConverter.GetBytes(TotalRecordsCount));
        byteBuffer.AddRange(BitConverter.GetBytes(NextBlockIndex));

        return byteBuffer.ToArray();
    }

    public override void FromBytes(byte[] bytes)  // načítanie do predka a pridanie atribútov pre tohto potomka
    {
        base.FromBytes(bytes);
        int position = base.GetSize();
        TotalRecordsCount = BitConverter.ToUInt32(bytes.AsSpan(position, 4).ToArray());
        NextBlockIndex = BitConverter.ToInt32(bytes.AsSpan(position + 4, 4).ToArray());
    }
}