using SEM_2_CORE.Interfaces;

namespace SEM_2_CORE;

public class HeapFile<T> where T : IDataClassOperations<T>, IByteOperations
{
    public string FilePath { get; set; }
    public int BlockSize { get; set; }
    public int BlockFactor { get; set; }
    public int PaddingSize { get; set; }
    private FileStream Stream;
    public List<int> FreeBlocks { get; set; }
    public List<int> PartiallyFreeBlocks { get; set; }

    public HeapFile(string filePath, int blockSize, T dataInstance)
    {
        FilePath = filePath;
        BlockSize = blockSize;
        BlockFactor = (BlockSize - 4) / dataInstance.GetSize();  // 4 byty sú na valid count v bloku, takže až zvyšok je pre záznamy
        FreeBlocks = new List<int>();
        PartiallyFreeBlocks = new List<int>();
        Stream = new FileStream(FilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);

        int usedSpace = 4 + BlockFactor * dataInstance.GetSize();
        PaddingSize = BlockSize - usedSpace;
    }

    ~HeapFile()
    {
        Stream.Close();
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

    // using lebo keď to padlo pred close, tak bez vymazania súboru zostal vysieť -> expection being used by another process, takto je uvoľnený aj pri exception
    private bool CheckIndex(int index, T data)
    {
        if (Stream.Length < (index + 1) * BlockSize || index < 0)
        {
            //Console.WriteLine($"Index {index} is out of range!");
            return false;
        }

        return true;
    }

    public Block<T> LoadBlockFromFile(T data, int index)
    {
        Block<T> block = new Block<T>(BlockFactor, data);
        Stream.Seek(index * BlockSize, SeekOrigin.Begin);  // vytvorenie streamu a seekovanie na index, načítanie dát pre blok a naplnenie v inštancii
        byte[] blockBytes = new byte[BlockSize];
        Stream.ReadExactly(blockBytes, 0, BlockSize);
        block.FromBytes(blockBytes);
        Stream.Seek(index * BlockSize, SeekOrigin.Begin); // seek naspät, lebo sa bude do bloku potenciálne zapisovať

        return block;
    } 

    private void TruncateFile()
    {
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

    public int Insert(T data)
    {
        int index;
        Block<T> block;

        if (PartiallyFreeBlocks.Count > 0)  // prioritné použitie častočne voľného bloku, potom voľného
        {
            index = PartiallyFreeBlocks[0];  // budeme pracovať s prvým v zozname
            block = LoadBlockFromFile(data, PartiallyFreeBlocks[0]);
            block.InsertRecord(data);

            if (block.ValidCount == BlockFactor)  // ak sa blok naplnil, odoberie sa zo zoznamu čiastočne voľných
            {
                PartiallyFreeBlocks.RemoveAt(0);
            }
        }
        else if (FreeBlocks.Count > 0)
        {
            index = FreeBlocks[0];
            block = LoadBlockFromFile(data, FreeBlocks[0]);
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
            Stream.SetLength(Stream.Length + BlockSize);
            Stream.Seek(index * BlockSize, SeekOrigin.Begin);

            if (BlockFactor > 1)  // ak je blokovací faktor 1, tak nový blok je automaticky plný, inak sa hneď pridá do zoznamu čiastočne voľných
            {
                PartiallyFreeBlocks.Add(index);
            }
        }

        byte[] finalBytes = new byte[BlockSize];
        byte[] blockBytes = block.GetBytes();
        byte[] padding = new byte[PaddingSize];
        Buffer.BlockCopy(blockBytes, 0, finalBytes, 0, blockBytes.Length);
        if (PaddingSize > 0)
        {
            Buffer.BlockCopy(padding, 0, finalBytes, blockBytes.Length, padding.Length);
        }
        Stream.Write(finalBytes, 0, BlockSize);

        return index;
    }

    public T? Get(int index, T data)
    {
        T? result;
        if (!CheckIndex(index, data))   // kontrola indexu
        {
            return default;
        }
        if (Stream.Length == 0)
        {
            //Console.WriteLine($"No data at index {index}, the file is empty!");
            return default;
        }
        Block<T> block = LoadBlockFromFile(data, index);  // načítanie bloku zo súboru
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
        if (!CheckIndex(index, data))  // kontrola indexu
        {
            return default;
        }
        if (Stream.Length == 0)
        {
            //Console.WriteLine($"No data at index {index}, the file is empty!");
            return default;
        }

        Block<T> block = LoadBlockFromFile(data, index);  // načítanie bloku zo súboru;
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

        byte[] finalBytes = new byte[BlockSize];
        byte[] blockBytes = block.GetBytes();
        byte[] padding = new byte[PaddingSize];
        Buffer.BlockCopy(blockBytes, 0, finalBytes, 0, blockBytes.Length);
        if (PaddingSize > 0)
        {
            Buffer.BlockCopy(padding, 0, finalBytes, blockBytes.Length, padding.Length);
        }
        Stream.Write(finalBytes, 0, BlockSize);

        if (index == Stream.Length / BlockSize - 1 && block.ValidCount == 0)  // ak sa vymazal posledný záznam v bloku a bol to posledný blok, tak sa súbor skráti
        {
            TruncateFile();
        }

        return result;
    }

    public void CloseFile()
    {
        
    }

    public List<Block<T>> GetFileContents(T dataInstance) 
    {
        List<Block<T>> blockList = new List<Block<T>>();

        if (Stream.Length == 0)
        {
            return blockList;
        }
        int blockCount = (int)Stream.Length / BlockSize;

        for (int i = 0; i < blockCount; i++)
        {
            Block<T> block = new Block<T>(BlockFactor, dataInstance);
            Stream.Seek(i * BlockSize, SeekOrigin.Begin);
            byte[] blockBytes = new byte[BlockSize];
            Stream.ReadExactly(blockBytes, 0, BlockSize);
            block.FromBytes(blockBytes);
            blockList.Add(block);
        }

        return blockList;
    }
}