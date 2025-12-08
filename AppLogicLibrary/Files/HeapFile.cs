using SEM_2_CORE.Files;
using SEM_2_CORE.Interfaces;

namespace SEM_2_CORE;

public class HeapFile<T> where T : IDataClassOperations<T>, IByteOperations
{
    public string FilePath { get; private set; }
    public int BlockSize { get; private set; }
    public int BlockFactor { get; private set; }
    public int PaddingSize { get; private set; }
    private FileStream Stream;
    public List<int> FreeBlocks { get; set; } = new List<int>();
    public List<int> PartiallyFreeBlocks { get; set; } = new List<int>();

    public HeapFile(string filePath, int blockSize, T dataInstance, int startSize = 0, uint mode = 0)
    {
        FilePath = filePath;
        BlockSize = blockSize;
        int extraBlockBytes = 4;
        if (mode == 1)
        {
            extraBlockBytes = 12;
        }
        else if (mode == 2)
        {
            extraBlockBytes = 8;
        }
        BlockFactor = (BlockSize - extraBlockBytes) / dataInstance.GetSize();  // 4 byty sú na valid count v bloku, takže až zvyšok je pre záznamy
        Stream = new FileStream(FilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        int usedSpace = extraBlockBytes + BlockFactor * dataInstance.GetSize();
        PaddingSize = BlockSize - usedSpace;

        if (startSize > 0)
        {
            InitializeFile(BlockFactor, dataInstance, startSize);
        }
    }

    public HeapFile(StreamReader reader)
    {
        FilePath = reader.ReadLine()!;
        BlockSize = int.Parse(reader.ReadLine()!);
        BlockFactor = int.Parse(reader.ReadLine()!);
        PaddingSize = int.Parse(reader.ReadLine()!);

        int freeBlocks = int.Parse(reader.ReadLine()!);
        for (int i = 0; i < freeBlocks; i++)
        {
            FreeBlocks.Add(int.Parse(reader.ReadLine()!));
        }
        int partiallyFreeBlocks = int.Parse(reader.ReadLine()!);
        for (int i = 0; i < partiallyFreeBlocks; i++)
        {
            PartiallyFreeBlocks.Add(int.Parse(reader.ReadLine()!));
        }
        Stream = new FileStream(FilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
    }

    ~HeapFile()
    {
        Stream.Close();
    }

    public void SaveControlData(StreamWriter writer)
    {
        writer.WriteLine(FilePath);
        writer.WriteLine(BlockSize);
        writer.WriteLine(BlockFactor);
        writer.WriteLine(PaddingSize);
        writer.WriteLine(FreeBlocks.Count);
        for (int i = 0; i < FreeBlocks.Count; i++)
        {
            writer.WriteLine(FreeBlocks[i]);
        }

        writer.WriteLine(PartiallyFreeBlocks.Count);
        for (int i = 0; i < PartiallyFreeBlocks.Count; i++)
        {
            writer.WriteLine(PartiallyFreeBlocks[i]);
        }

        Stream.Close();
    }

    public int GetTotalSpace()
    {
        return (int)((Stream.Length / BlockSize) * BlockFactor);
    }

    public void TruncateFile(T dataInstance)
    {
        if (Stream.Length > 0)
        {
            Block<T> block = LoadBlockFromFile<Block<T>>(dataInstance, (int)(Stream.Length / BlockSize) - 1);
            if (block.ValidCount != 0)
            {
                return;
            }
        }
        else
        {
            return;
        }

        FreeBlocks.Sort();
        int firstFree = FreeBlocks.Last();  // ak to bol posledný blok, tak sa iba skráti na length 0, inak toľko blokov, koľko je na konci voľných
        if (Stream.Length / BlockSize == 1)
        {
            FreeBlocks.Clear();
            Stream.SetLength(0);
            return;
        }

        for (int i = FreeBlocks.Count - 2; i >= 0; i--)
        {
            if (FreeBlocks[i] == firstFree - 1)
            {
                firstFree = FreeBlocks[i];
                FreeBlocks.RemoveAt(i + 1);
            }
            else
            {
                break;
            }
        }

        FreeBlocks.RemoveAt(FreeBlocks.Count - 1);
        Stream.SetLength(firstFree * BlockSize);
    }

    private void InitializeFile(int blockFactor, T dataInstance, int startSize)
    {
        List<byte> buffer = new List<byte>();
        PrimaryHashBlock<T> block = new PrimaryHashBlock<T>(blockFactor, dataInstance, emptyBlock: true);
        byte[] finalBytes = new byte[BlockSize];
        byte[] blockBytes = block.GetBytes();
        byte[] padding = new byte[PaddingSize];
        Buffer.BlockCopy(blockBytes, 0, finalBytes, 0, blockBytes.Length);
        if (PaddingSize > 0)
        {
            Buffer.BlockCopy(padding, 0, finalBytes, blockBytes.Length, padding.Length);
        }

        for (int i = 0; i < startSize; i++)
        {
            buffer.AddRange(finalBytes);
        }
        Stream.Write(buffer.ToArray(), 0, buffer.Count);
    }

    private void WriteBlock(Block<T> block)  // už treba byť naseekovaný na správnom indexe
    {
        byte[] finalBytes = new byte[BlockSize];
        byte[] blockBytes = block.GetBytes();
        byte[] padding = new byte[PaddingSize];
        Buffer.BlockCopy(blockBytes, 0, finalBytes, 0, blockBytes.Length);
        if (PaddingSize > 0)
        {
            Buffer.BlockCopy(padding, 0, finalBytes, blockBytes.Length, padding.Length);
        }
        Stream.Write(finalBytes, 0, BlockSize);
    }

    public T? TryToFindRecord(Block<T> block, T record)
    {
        T result;
        for (int i = 0; i < block.ValidCount; i++)  // prejdú sa validné záznamy a skúsi sa nájsť hľadaný záznam
        {
            if (block.RecordsList[i].Equals(record))  // po načítaní z bytov ak má T string tak bude bez paddingu, teda porovnáva sa bez paddingu
            {
                result = block.RecordsList[i];
                return result;
            }
        }

        return default;
    }

    public bool CheckIndex(int index)
    {
        if (Stream.Length < (index + 1) * BlockSize || index < 0)
        {
            //Console.WriteLine($"Index {index} is out of range!");
            return false;
        }

        return true;
    }

    // s generikom Blok aby to mohol byť rôzny typ bloku - napr. aj primary alebo overflow pre hash file
    public Block LoadBlockFromFile<Block>(T data, int index) where Block : Block<T>, new()
    {
        Block block = new Block();
        block.DataInstance = data;
        block.RecordsCount = BlockFactor;

        Stream.Seek(index * BlockSize, SeekOrigin.Begin);  // vytvorenie streamu a seekovanie na index, načítanie dát pre blok a naplnenie v inštancii
        byte[] blockBytes = new byte[BlockSize];
        Stream.ReadExactly(blockBytes, 0, BlockSize);
        block.FromBytes(blockBytes);
        Stream.Seek(index * BlockSize, SeekOrigin.Begin); // seek naspät, lebo sa bude do bloku potenciálne zapisovať

        return block;
    } 

    public int Insert(T data)
    {
        int index;
        Block<T> block;

        if (PartiallyFreeBlocks.Count > 0)  // prioritné použitie častočne voľného bloku, potom voľného
        {
            index = PartiallyFreeBlocks[0];  // budeme pracovať s prvým v zozname
            block = LoadBlockFromFile<Block<T>>(data, PartiallyFreeBlocks[0]);
            block.InsertRecord(data);

            if (block.ValidCount == BlockFactor)  // ak sa blok naplnil, odoberie sa zo zoznamu čiastočne voľných
            {
                PartiallyFreeBlocks.RemoveAt(0);
            }
        }
        else if (FreeBlocks.Count > 0)
        {
            index = FreeBlocks[0];
            block = LoadBlockFromFile<Block<T>>(data, FreeBlocks[0]);
            block.InsertRecord(data);

            FreeBlocks.RemoveAt(0);  // automaticky po vložení už nie je voľný, medzi čiastočne voľné sa potom pridá iba ak je blokovací faktor > 1
            if (BlockFactor > 1)
            {
                PartiallyFreeBlocks.Add(index);
                PartiallyFreeBlocks.Sort();
            }
        }
        else
        {
            block = new Block<T>(BlockFactor, data, true);
            index = (int)(Stream.Length / BlockSize);
            Stream.Seek(index * BlockSize, SeekOrigin.Begin);

            if (BlockFactor > 1)  // ak je blokovací faktor 1, tak nový blok je automaticky plný, inak sa hneď pridá do zoznamu čiastočne voľných
            {
                PartiallyFreeBlocks.Add(index);
            }
        }

        WriteBlock(block);
        return index;
    }

    public T? Get(int index, T data)
    {
        T? result;
        if (!CheckIndex(index))   // kontrola indexu
        {
            return default;
        }
        if (Stream.Length == 0)
        {
            //Console.WriteLine($"No data at index {index}, the file is empty!");
            return default;
        }
        Block<T> block = LoadBlockFromFile<Block<T>>(data, index);  // načítanie bloku zo súboru
        result = TryToFindRecord(block, data);  // skúsi sa nájsť záznam v bloku

        if (result == null)
        {
            //Console.WriteLine($"Record was not found at the index {index}.");
        }

        return result;
    }

    public T? Delete(int index, T data)
    {
        T? result;
        if (!CheckIndex(index))  // kontrola indexu
        {
            return default;
        }
        if (Stream.Length == 0)
        {
            //Console.WriteLine($"No data at index {index}, the file is empty!");
            return default;
        }

        Block<T> block = LoadBlockFromFile<Block<T>>(data, index);  // načítanie bloku zo súboru;
        result = TryToFindRecord(block, data);  // skúsi sa nájsť záznam v bloku

        if (result == null)
        {
            //Console.WriteLine($"Record was not found at the index {index}");
            return result;
        }
        int countBeforeDelete = block.ValidCount;
        block.DeleteRecord(data);  // vymazanie záznamu

        if (countBeforeDelete == 1)
        {
            FreeBlocks.Add(index);
            FreeBlocks.Sort();
            if (PartiallyFreeBlocks.Contains(index))
            {
                PartiallyFreeBlocks.Remove(index);
            }
        }
        else if (countBeforeDelete == BlockFactor)
        {
            PartiallyFreeBlocks.Add(index);
            PartiallyFreeBlocks.Sort();
        }

        WriteBlock(block);
        if (index == Stream.Length / BlockSize - 1 && block.ValidCount == 0)  // ak sa vymazal posledný záznam v bloku a bol to posledný blok, tak sa súbor skráti
        {
            TruncateFile(data);
        }

        return result;
    }

    public void CloseFile()
    {
        
    }

    // s generikom Blok aby to mohol byť zoznam rôzneho typu bloku - napr. aj primary alebo overflow pre hash file
    public List<Block>GetFileContents<Block>(T dataInstance) where Block : Block<T>, new()
    {
        List<Block> blockList = new List<Block>();

        if (Stream.Length == 0)
        {
            return blockList;
        }
        int blockCount = (int)Stream.Length / BlockSize;

        for (int i = 0; i < blockCount; i++)
        {
            Block block = new Block();
            block.DataInstance = dataInstance;
            block.RecordsCount = BlockFactor;
            Stream.Seek(i * BlockSize, SeekOrigin.Begin);
            byte[] blockBytes = new byte[BlockSize];
            Stream.ReadExactly(blockBytes, 0, BlockSize);
            block.FromBytes(blockBytes);
            blockList.Add(block);
        }

        return blockList;
    }

    public int InsertAt(int index, Block<T> block)
    {
        if (!CheckIndex(index) && index != -1)
        {
            Console.WriteLine($"Index {index} is out of range!");
        }

        if (index == -1)
        {
            if (FreeBlocks.Count > 0)
            {
                index = FreeBlocks[0];
                FreeBlocks.RemoveAt(0);
            }
            else
            {
                Stream.SetLength(Stream.Length + BlockSize);
                index = (int)(Stream.Length / BlockSize) - 1;
            }
        }
        Stream.Seek(index * BlockSize, SeekOrigin.Begin);
        WriteBlock(block);

        return index;
    }
}