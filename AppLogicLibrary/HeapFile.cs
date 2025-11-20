namespace SEM_2_CORE;

public class HeapFile<T> where T : IDataClassOperations<T>, IByteOperations
{
    public string FilePath { get; set; }
    public int BlockSize { get; set; }
    public int BlockFactor { get; set; }
    public List<int> FreeBlocks { get; set; }
    public List<int> PartiallyFreeBlocks { get; set; }

    public HeapFile(string filePath, int blockSize)
    {
        FilePath = filePath;
        BlockSize = blockSize;
        BlockFactor = (BlockSize - 4);
        FreeBlocks = new List<int>();
        PartiallyFreeBlocks = new List<int>();
    }

    public int Insert(T data)
    {
        return 0;
    }

    public T Get(int index, T data)
    {
        return data;
    }

    public T Delete(int index, T data)
    {
        return data;
    }

    public void CloseFile()
    {

    }
}
