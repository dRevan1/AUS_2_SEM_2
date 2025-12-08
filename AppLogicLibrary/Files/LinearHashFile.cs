using SEM_2_CORE.Interfaces;
using System.Reflection.Emit;
using System.Reflection.PortableExecutable;

namespace SEM_2_CORE.Files;

public class LinearHashFile<T> where T : IDataClassOperations<T>, IByteOperations
{
    public int ModFunction { get; private set; }
    public int SplitPointer { get; private set; } = 0;
    public HeapFile<T> PrimaryFile { get; private set; }
    public HeapFile<T> OverflowFile { get; private set; }
    public int TotalRecordsCount { get; private set; } = 0;   // dočasne pre split podľa prednášky na testovanie a minimálnu hodnotu na benchmark
    public int TotalSpace { get; private set; } = 0;
    public int TotalChainLength { get; private set; } = 0;
    public int UsedPrimaryBlocks { get; private set; } = 0;  // všetky použité primárne bloky - na priemer pri split condition


    public LinearHashFile(int minMod, string primaryFilePath, string overflowFilePath, int primaryBlockSize, int overflowBlockSize, T dataInstance)
    {
        ModFunction = minMod;
        PrimaryFile = new HeapFile<T>(primaryFilePath, primaryBlockSize, dataInstance, startSize: ModFunction, mode: 1);
        OverflowFile = new HeapFile<T>(overflowFilePath, overflowBlockSize, dataInstance, mode: 2);
        TotalSpace = minMod * PrimaryFile.BlockFactor;
    }

    // načítanie údajov a potom pre heap súbory
    public LinearHashFile(string controlFilePath)
    {
        if (File.Exists(controlFilePath))
        {
            using StreamReader reader = new StreamReader(controlFilePath);
            ModFunction = int.Parse(reader.ReadLine()!);
            SplitPointer = int.Parse(reader.ReadLine()!);
            TotalRecordsCount = int.Parse(reader.ReadLine()!);
            TotalSpace = int.Parse(reader.ReadLine()!);
            TotalChainLength = int.Parse(reader.ReadLine()!);
            UsedPrimaryBlocks = int.Parse(reader.ReadLine()!);
            PrimaryFile = new HeapFile<T>(reader);
            OverflowFile = new HeapFile<T>(reader);
            reader.Close();
        }
    }

    public void SaveControlData(StreamWriter writer)
    {
        writer.WriteLine(ModFunction);
        writer.WriteLine(SplitPointer);
        writer.WriteLine(TotalRecordsCount);
        writer.WriteLine(TotalSpace);
        writer.WriteLine(TotalChainLength);
        writer.WriteLine(UsedPrimaryBlocks);
        PrimaryFile.SaveControlData(writer);
        OverflowFile.SaveControlData(writer);
        writer.Close();
    }

    // zápis blokov zo sekvencie primárneho bloku "primaryBlock" na indexe "primaryIndex"
    // zoznam preplňovacích blokov musí mať dopredu odstránené prázdne bloky na konci, zvyšné všetky majú nejaké platné záznamy, mali by byť defragmentované - napr. po splite či strasení
    private void WriteSequence(List<OverflowHashBlock<T>> overflowBlocks, PrimaryHashBlock<T> primaryBlock, int primaryIndex, bool append = false)
    {
        int index = -1;
        if (overflowBlocks.Count > 0)
        {
            for (int i = overflowBlocks.Count - 1; i >= 0; i--)
            {
                overflowBlocks[i].NextBlockIndex = index;
                index = OverflowFile.InsertAt(-1, overflowBlocks[i]);
            }
        }
        primaryBlock.NextBlockIndex = index;
        if (append == true)
        {
            PrimaryFile.InsertAt(-1, primaryBlock);
        }
        else
        {
            PrimaryFile.InsertAt(primaryIndex, primaryBlock);
        }
    }

    // na vytvorenie sekvencie pre 1 primárny blok
    // napr. po splite sa pošle zoznam prehešovaných dát na primaryBlock a rozdelia sa do primárneho bloku a prípadných preplňovacích blokov
    private void CreateSequence(List<T> recordsList, PrimaryHashBlock<T> primaryBlock, List<OverflowHashBlock<T>> overflowBlocks, T record)
    {
        OverflowHashBlock<T> overflowBlock;
        for (int i = 0; i < recordsList.Count; i++)  // naplnenie primárneho bloku
        {
            if (i < primaryBlock.RecordsCount)
            {
                primaryBlock.InsertRecord(recordsList[i]);
            }
            else   // keď sa naplní, tak ostatné sa pridávajú do jeho preplňovacích, ktoré treba vytvoriť
            {
                int index = (i - PrimaryFile.BlockFactor) / OverflowFile.BlockFactor;
                if (index == overflowBlocks.Count)
                {
                    overflowBlock = new OverflowHashBlock<T>(OverflowFile.BlockFactor, record, emptyBlock: true);
                    overflowBlocks.Add(overflowBlock);
                }
                overflowBlocks[index].InsertRecord(recordsList[i]);
            }
        }
    }

    private bool SplitCondition()
    {
        double loadFactor = (double)TotalRecordsCount / (double)TotalSpace;
        double averageChainLength = (double)TotalChainLength / (double)UsedPrimaryBlocks;

        if (loadFactor > 0.88)
        {
            return true;
        }
        else if (averageChainLength > 0.82)
        {
            return true;
        }

        return false;
    }

    private void Split(T record)
    {
        while (SplitCondition())
        {
            PrimaryHashBlock<T> splitBlock = PrimaryFile.LoadBlockFromFile<PrimaryHashBlock<T>>(record, SplitPointer);
            PrimaryHashBlock<T> newBlock = new PrimaryHashBlock<T>(PrimaryFile.BlockFactor, record, emptyBlock: true);
            List<OverflowHashBlock<T>> splitOverflowBlocks = new List<OverflowHashBlock<T>>();
            List<OverflowHashBlock<T>> newOverflowBlocks = new List<OverflowHashBlock<T>>();
            OverflowHashBlock<T> overflowBlock;
            List<T> splitRecords = new List<T>();
            List<T> newRecords = new List<T>();

            for (int i = 0; i < splitBlock.ValidCount; i++)  // prehešovanie záznamov v split bloku
            {
                int index = splitBlock.RecordsList[i].GetHashCode();
                index %= (ModFunction * 2);
                if (index != SplitPointer)
                {
                    newRecords.Add(splitBlock.RecordsList[i]);
                }
                else
                {
                    splitRecords.Add(splitBlock.RecordsList[i]);
                }
            }
            splitBlock.ValidCount = 0;

            if (splitBlock.NextBlockIndex != -1)   // ak má split blok preplňovacie bloky, ich záznamy sa tiež prehešujú a bloky sa načítajú
            {
                overflowBlock = OverflowFile.LoadBlockFromFile<OverflowHashBlock<T>>(record, splitBlock.NextBlockIndex);
                OverflowFile.FreeBlocks.Add(splitBlock.NextBlockIndex);
                while (true)
                {
                    TotalChainLength--;
                    for (int i = 0; i < overflowBlock.ValidCount; i++)
                    {
                        int index = overflowBlock.RecordsList[i].GetHashCode();
                        index %= (ModFunction * 2);
                        if (index != SplitPointer)
                        {
                            newRecords.Add(overflowBlock.RecordsList[i]);
                        }
                        else
                        {
                            splitRecords.Add(overflowBlock.RecordsList[i]);
                        }
                    }
                    if (overflowBlock.NextBlockIndex == -1)
                    {
                        break;
                    }
                    OverflowFile.FreeBlocks.Add(overflowBlock.NextBlockIndex);
                    overflowBlock = OverflowFile.LoadBlockFromFile<OverflowHashBlock<T>>(record, overflowBlock.NextBlockIndex);
                }
                OverflowFile.FreeBlocks.Sort();
            }

            splitBlock.TotalRecordsCount = (uint)splitRecords.Count;
            newBlock.TotalRecordsCount = (uint)newRecords.Count;
            CreateSequence(splitRecords, splitBlock, splitOverflowBlocks, record); // zo zoznamu dát, ktoré majú zostať na split bloku sa vytvorí zreťazenie blokov na zápis
            WriteSequence(splitOverflowBlocks, splitBlock, SplitPointer); // zápis zreťazenia pre split block - na ktorý ukazuje split pointer
            CreateSequence(newRecords, newBlock, newOverflowBlocks, record);  // zo zoznamu dát, ktoré majú byť na novom bloku sa vytvorí zreťazenie blokov na zápis
            WriteSequence(newOverflowBlocks, newBlock, SplitPointer + ModFunction, append: true);  // zapíše sa zreťazenie blokov pre nový block "newBlock"
            OverflowFile.TruncateFile(record);  // ak by zostali voľné bloky na konci tak sa skráti súbor
            TotalSpace = GetTotalSpace();
            TotalChainLength += (splitOverflowBlocks.Count + newOverflowBlocks.Count);
            if (splitBlock.TotalRecordsCount > 0 && newBlock.TotalRecordsCount > 0) // ak sa záznamy rozdelili do oboch hlavných blokov - použité sa zvýšia o 1
            {
                UsedPrimaryBlocks++;
            }

            if (SplitPointer + 1 == ModFunction)
            {
                ModFunction *= 2;
                SplitPointer = 0;
            }
            else
            {
                SplitPointer++;
            }
        }
    }

    private int GetRecordIndex(T record)
    {
        int hash = record.GetHashCode();
        int primary = hash % ModFunction;
        int index = primary < SplitPointer
            ? hash % (ModFunction * 2)
            : primary;

        return index;
    }

    public int GetTotalSpace()
    {
        return PrimaryFile.GetTotalSpace() + OverflowFile.GetTotalSpace();
    }

    public int Insert(T record)
    {
        int index = GetRecordIndex(record);
        int nextBlockIndex = -1;

        PrimaryHashBlock<T> primaryBlock = PrimaryFile.LoadBlockFromFile<PrimaryHashBlock<T>>(record, index);  // načítanie bloku a seek na pozíciu, ak tam je miesto tak sa potom rovno blok uloží sem
        OverflowHashBlock<T> overflowBlock;

        if (primaryBlock.InsertRecord(record))  // ak sa záznam vložil do bloku - je tam voľné miesto, inak je false a ide sa na overflow bloky
        {
            primaryBlock.TotalRecordsCount++;
            if (primaryBlock.TotalRecordsCount == 1)   // ak sa pridal prvý záznam do bloku tak sa zvýši o 1
            {
                UsedPrimaryBlocks++;
            }
            PrimaryFile.InsertAt(index, primaryBlock);
            TotalRecordsCount++;
            if (SplitCondition())
            {
                Split(record);
            }
            return index;
        }
        if (primaryBlock.NextBlockIndex == -1)  // blok je plný ale nemá preplňovací blok - vytvoríme nový a zapíšeme na index -1, takže sa v HeapFile zapíše do nového voľného miesta
        {
            overflowBlock = new OverflowHashBlock<T>(OverflowFile.BlockFactor, record, true);  // vytvorí sa nový blok - vyplní sa vkladaným záznamom a valid count = 1
            nextBlockIndex = OverflowFile.InsertAt(-1, overflowBlock);
            primaryBlock.NextBlockIndex = nextBlockIndex;
            primaryBlock.TotalRecordsCount++;
            PrimaryFile.InsertAt(index, primaryBlock);
            TotalRecordsCount++;
            TotalSpace += OverflowFile.BlockFactor;
            TotalChainLength += 1;
            if (SplitCondition())
            {
                Split(record);
            }
            return index;
        }

        overflowBlock = OverflowFile.LoadBlockFromFile<OverflowHashBlock<T>>(record, primaryBlock.NextBlockIndex);
        nextBlockIndex = primaryBlock.NextBlockIndex;
        // to isté, ale teraz cez zreťazenie overflow blokov -> hľadanie voľného miesta, prípadne sa spraví nový blok
        while (!overflowBlock.InsertRecord(record))
        {
            if (overflowBlock.NextBlockIndex == -1)
            {
                OverflowHashBlock<T> newOverflowBlock = new OverflowHashBlock<T>(OverflowFile.BlockFactor, record, newBlock: true);
                int lastIndex = OverflowFile.InsertAt(-1, newOverflowBlock);
                overflowBlock.NextBlockIndex = lastIndex;
                TotalSpace += OverflowFile.BlockFactor;
                TotalChainLength += 1;
                break;
            }
            nextBlockIndex = overflowBlock.NextBlockIndex;
            overflowBlock = OverflowFile.LoadBlockFromFile<OverflowHashBlock<T>>(record, overflowBlock.NextBlockIndex);
        }
        OverflowFile.InsertAt(nextBlockIndex, overflowBlock);
        primaryBlock.TotalRecordsCount++;
        PrimaryFile.InsertAt(index, primaryBlock);
        TotalRecordsCount++;

        if (SplitCondition())
        {
            Split(record);
        }

        return index;
    }

    public T? Get(T record)
    {
        return FindRecordBlock(record, out _, out _, out _);
    }

    private T? FindRecordBlock(T record, out PrimaryHashBlock<T>? primaryBlock, out OverflowHashBlock<T>? overflowBlock, out int index)
    {
        index = GetRecordIndex(record);
        overflowBlock = null;
        primaryBlock = null;
        if (!PrimaryFile.CheckIndex(index))
        {
            return default;
        }
        primaryBlock = PrimaryFile.LoadBlockFromFile<PrimaryHashBlock<T>>(record, index);
        T? foundRecord = PrimaryFile.TryToFindRecord(primaryBlock, record);   // skúsi sa nájsť v primárnom bloku záznam, ak tam nie je a existuje index na overflow blok, tak sa to skúsi tam

        if (foundRecord != null)
        {
            return foundRecord;
        }
        if (primaryBlock.NextBlockIndex == -1)
        {
            return default;
        }
        overflowBlock = OverflowFile.LoadBlockFromFile<OverflowHashBlock<T>>(record, primaryBlock.NextBlockIndex);
        index = primaryBlock.NextBlockIndex;
        foundRecord = OverflowFile.TryToFindRecord(overflowBlock, record);

        if (foundRecord != null)
        {
            return foundRecord;
        }
        while (overflowBlock.NextBlockIndex != -1)
        {
            index = overflowBlock.NextBlockIndex;
            overflowBlock = OverflowFile.LoadBlockFromFile<OverflowHashBlock<T>>(record, overflowBlock.NextBlockIndex);  // prejdenie zreťazenia overflow blokov a pokus o nájdenie záznamu
            foundRecord = OverflowFile.TryToFindRecord(overflowBlock, record);

            if (foundRecord != null)
            {
                return foundRecord;
            }
        }

        return foundRecord;  // v tomto bode bude return vždy null
    }

    public int Update(T record)
    {
        PrimaryHashBlock<T>? primaryBlock;
        OverflowHashBlock<T>? overflowBlock;
        int index;

        T? findRecord = FindRecordBlock(record, out primaryBlock, out overflowBlock, out index);
        if (findRecord == null)
        {
            return -1;
        }
        else if (overflowBlock != null)
        {
            overflowBlock.UpdateRecord(record);
            OverflowFile.InsertAt(index, overflowBlock);
        }
        else if (primaryBlock != null)
        {
            primaryBlock!.UpdateRecord(record);
            PrimaryFile.InsertAt(index, primaryBlock);
        }

        return index;
    }
}