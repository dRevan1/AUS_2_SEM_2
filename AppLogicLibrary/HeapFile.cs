using System;
using System.Reflection;

namespace SEM_2_CORE;

public class HeapFile<T> where T : IDataClassOperations<T>, IByteOperations
{
    public string FilePath { get; set; }
    public int BlockSize { get; set; }
    public int BlockFactor { get; set; }
    public int PaddingSize { get; set; }
    public List<int> FreeBlocks { get; set; }
    public List<int> PartiallyFreeBlocks { get; set; }

    public HeapFile(string filePath, int blockSize, T dataInstance)
    {
        FilePath = filePath;
        BlockSize = blockSize;
        BlockFactor = (BlockSize - 4) / dataInstance.GetSize();  // 4 byty sú na valid count v bloku, takže až zvyšok je pre záznamy
        FreeBlocks = new List<int>();
        PartiallyFreeBlocks = new List<int>();

        int usedSpace = 4 + (BlockFactor * dataInstance.GetSize());
        PaddingSize = BlockSize - usedSpace;
    }

    private T? TryToFindRecord(Block<T> block, T record)
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
    private FileStream? CheckIndex(int index, T data)
    {
        FileStream stream = new FileStream(FilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite); // kontrola indexu
        if (index == 0 && stream.Length == 0)
        {
            return stream;
        }
        if (stream.Length < (index + 1) * BlockSize || index < 0)
        {
            stream.Close();
            //Console.WriteLine($"Index {index} is out of range!");
            return default;
        }

        return stream;
    }

    private Block<T> LoadBlockFromFile(FileStream stream, T data, int index)
    {
        Block<T> block = new Block<T>(BlockFactor, data);
        stream.Seek(index * BlockSize, SeekOrigin.Begin);  // vytvorenie streamu a seekovanie na index, načítanie dát pre blok a naplnenie v inštancii
        byte[] blockBytes = new byte[BlockSize];
        stream.ReadExactly(blockBytes, 0, BlockSize);
        block.FromBytes(blockBytes);
        stream.Seek(index * BlockSize, SeekOrigin.Begin);

        return block;
    } 

    private void TruncateFile(FileStream stream)
    {
        FreeBlocks.Sort();
        int firstFree = FreeBlocks.Last();  // ak to bol posledný blok, tak sa iba skráti na length 0, inak toľko blokov, koľko je na konci voľných
        if (stream.Length / BlockSize == 1)
        {
            FreeBlocks.Clear();
            stream.SetLength(0);
            return;
        }

        for (int i = (FreeBlocks.Count - 2); i >= 0; i--)
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
        stream.SetLength(firstFree * BlockSize);
    }

    public int Insert(T data)
    {
        int index;
        using FileStream stream = new FileStream(FilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        Block<T> block;

        if (PartiallyFreeBlocks.Count > 0)  // prioritné použitie častočne voľného bloku, potom voľného
        {
            index = PartiallyFreeBlocks[0];  // budeme pracovať s prvým v zozname
            block = LoadBlockFromFile(stream, data, PartiallyFreeBlocks[0]);
            block.InsertRecord(data);

            if (block.ValidCount == BlockFactor)  // ak sa blok naplnil, odoberie sa zo zoznamu čiastočne voľných
            {
                PartiallyFreeBlocks.RemoveAt(0);
            }
        }
        else if (FreeBlocks.Count > 0)
        {
            index = FreeBlocks[0];
            block = LoadBlockFromFile(stream, data, FreeBlocks[0]);
            block.InsertRecord(data);

            FreeBlocks.RemoveAt(0);  // automaticky po vložení už nie je voľný, medzi čiastočne voľné sa potom pridá iba ak je blokovací faktor > 1
            if (BlockFactor > 1)
            {
                PartiallyFreeBlocks.Add(index);
            }
        }
        else
        {
            block = new Block<T>(BlockFactor, data, true);
            index = (int)(stream.Length / BlockSize);
            stream.SetLength(stream.Length + BlockSize);
            stream.Seek(index * BlockSize, SeekOrigin.Begin);

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

        stream.Write(finalBytes, 0, BlockSize);
        stream.Close();

        return index;
    }

    public T? Get(int index, T data)
    {
        T? result;
        using FileStream? stream = CheckIndex(index, data);  // kontrola indexu
        if (stream == null)
        {
            return default;
        }
        if (stream.Length == 0)
        {
            stream.Close();
            //Console.WriteLine($"No data at index {index}, the file is empty!");
            return default;
        }
        Block<T> block = LoadBlockFromFile(stream, data, index);  // načítanie bloku zo súboru
        result = TryToFindRecord(block, data);  // skúsi sa nájsť záznam v bloku

        if (result == null)
        {
            //Console.WriteLine($"Record was not found at the index {index}.");
        }
        stream.Close();

        return result;
    }

    public T? Delete(int index, T data)
    {
        T? result;
        using FileStream? stream = CheckIndex(index, data);  // kontrola indexu
        if (stream == null)
        {
            return default;
        }
        if (stream.Length == 0)
        {
            stream.Close();
            //Console.WriteLine($"No data at index {index}, the file is empty!");
            return default;
        }

        Block<T> block = LoadBlockFromFile(stream, data, index);  // načítanie bloku zo súboru;
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
            if (PartiallyFreeBlocks.Contains(index))
            {
                PartiallyFreeBlocks.Remove(index);
            }
        }
        else if (countBeforeDelete == BlockFactor)
        {
            PartiallyFreeBlocks.Add(index);
        }

        byte[] finalBytes = new byte[BlockSize];
        byte[] blockBytes = block.GetBytes();
        byte[] padding = new byte[PaddingSize];
        Buffer.BlockCopy(blockBytes, 0, finalBytes, 0, blockBytes.Length);
        if (PaddingSize > 0)
        {
            Buffer.BlockCopy(padding, 0, finalBytes, blockBytes.Length, padding.Length);
        }
        stream.Write(finalBytes, 0, BlockSize);

        if (index == (stream.Length / BlockSize) - 1 && block.ValidCount == 0)  // ak sa vymazal posledný záznam v bloku a bol to posledný blok, tak sa súbor skráti
        {
            TruncateFile(stream);
        }
        stream.Close();

        return result;
    }

    public void CloseFile()
    {

    }

    public List<Block<T>> GetFileContents(T dataInstance) 
    {
        List<Block<T>> blockLIst = new List<Block<T>>();
        using FileStream stream = new FileStream(FilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);

        if (stream.Length == 0)
        {
            stream.Close();
            return blockLIst;
        }
        int blockCount = (int)stream.Length / BlockSize;

        for (int i = 0; i < blockCount; i++)
        {
            Block<T> block = new Block<T>(BlockFactor, dataInstance);
            stream.Seek(i * BlockSize, SeekOrigin.Begin);
            byte[] blockBytes = new byte[BlockSize];
            stream.ReadExactly(blockBytes, 0, BlockSize);
            block.FromBytes(blockBytes);
            blockLIst.Add(block);
        }

        stream.Close();
        return blockLIst;
    }
}