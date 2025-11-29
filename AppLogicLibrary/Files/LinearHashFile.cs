using SEM_2_CORE.Interfaces;

namespace SEM_2_CORE.Files;

public class LinearHashFile<T> where T : IDataClassOperations<T>, IByteOperations
{
    private int ModFunction { get; set; }
    private int SplitPointer { get; set; } = 0;
    private HeapFile<T> PrimaryFile { get; set; }
    private HeapFile<T> OverflowFile { get; set; }
    private int TotalRecordsCount { get; set; } = 0;   // dočasne pre split podľa prednášky na testovanie a minimálnu hodnotu na benchmark
    private int TotalSpace { get; set; } = 0;


    public LinearHashFile(int minMod, string primaryFilePath, string overflowFilePath, int primaryBlockSize, int overflowBlockSize, T dataInstance)
    {
        ModFunction = minMod;
        PrimaryFile = new HeapFile<T>(primaryFilePath, primaryBlockSize, dataInstance);
        OverflowFile = new HeapFile<T>(overflowFilePath, overflowBlockSize, dataInstance);
    }

    private bool SplitCondition()
    {
        return TotalRecordsCount / (TotalSpace) > 0.8;  // dočasne, aby sa dalo otestovať insert
    }

    private bool MergeCondition()
    {
        return true;
    }

    private void Split(T record)
    {
        while (SplitCondition())
        {
            PrimaryHashBlock<T> splitBlock = (PrimaryHashBlock<T>)PrimaryFile.LoadBlockFromFile(record, SplitPointer);
            PrimaryHashBlock<T> newBlock = new PrimaryHashBlock<T>(PrimaryFile.BlockSize, record, emptyBlock: true);
            List<OverflowHashBlock<T>> splitOverflowBlocks = new List<OverflowHashBlock<T>>();
            List<OverflowHashBlock<T>> newOverflowBlocks = new List<OverflowHashBlock<T>>();
            OverflowHashBlock<T> overflowBlock;
            List<T> splitRecords = new List<T>();
            List<T> newRecords = new List<T>();

            foreach (var vrecord in splitBlock.RecordsList)
            {
                int index = vrecord.GetHashCode();
                if (index < SplitPointer)
                {
                    splitRecords.Add(vrecord);
                }
                else
                {
                    newRecords.Add(vrecord);
                }
            }
            splitBlock.ValidCount = 0;

            if (splitBlock.NextBlockIndex != -1)
            {
                overflowBlock = (OverflowHashBlock<T>)OverflowFile.LoadBlockFromFile(record, splitBlock.NextBlockIndex);
                while (overflowBlock.NextBlockIndex != -1)
                {
                    foreach (var vrecord in overflowBlock.RecordsList)
                    {
                        int index = vrecord.GetHashCode();
                        if (index < SplitPointer)
                        {
                            splitRecords.Add(vrecord);
                        }
                        else
                        {
                            newRecords.Add(vrecord);
                        }
                    }
                    splitOverflowBlocks.Add(overflowBlock);
                    overflowBlock.ValidCount = 0;
                    overflowBlock = (OverflowHashBlock<T>)OverflowFile.LoadBlockFromFile(record, overflowBlock.NextBlockIndex);
                }
            }
            int overflowBlocksCount = splitOverflowBlocks.Count;

            for (int i = 0; i < splitRecords.Count; i++ )   // na začiatku sa pridávajú, ktoré majú ísť do split bloku (pôvodného) do primárneho bloku
            {
                if (i < splitBlock.RecordsCount)
                {
                    splitBlock.InsertRecord(splitRecords[i]);
                }
                else   // keď sa naplní, tak ostatné sa pridávajú do jeho preplňovacích podľa indexu
                {
                    int index = (i - PrimaryFile.BlockFactor) / OverflowFile.BlockFactor;
                    splitOverflowBlocks[index].InsertRecord(splitRecords[i]);
                }
            }

            if (splitOverflowBlocks.Count > 0)
            {
                overflowBlock = splitOverflowBlocks.Last();
                while (overflowBlock.ValidCount == 0)
                {
                    int index = (splitOverflowBlocks.Count > 1) ? splitOverflowBlocks[splitOverflowBlocks.Count - 2].NextBlockIndex : splitBlock.NextBlockIndex;
                    OverflowFile.FreeBlocks.Add(index);
                    splitOverflowBlocks.RemoveAt(splitOverflowBlocks.Count - 1);
                    if (splitOverflowBlocks.Count == 0)
                    {
                        break;
                    }
                    overflowBlock = splitOverflowBlocks.Last();
                }
                if (splitOverflowBlocks.Count > 0)
                {
                    splitOverflowBlocks.Last().NextBlockIndex = -1;
                }

                OverflowFile.FreeBlocks.Sort();
            }
            else
            {
                splitBlock.NextBlockIndex = -1;
            }
            PrimaryFile.InsertAt(SplitPointer, splitBlock);

            for (int i = 0; i < newRecords.Count; i++)
            {
                if (i < newBlock.RecordsCount)
                {
                    newBlock.InsertRecord(newRecords[i]);
                }
                else   // keď sa naplní, tak ostatné sa pridávajú do jeho preplňovacích, ktoré treba vytvoriť
                {
                    int index = (i - PrimaryFile.BlockFactor) / OverflowFile.BlockFactor;
                    if (index == newOverflowBlocks.Count)
                    {
                        overflowBlock = new OverflowHashBlock<T>(OverflowFile.BlockSize, record, emptyBlock: true);
                    }
                    newOverflowBlocks[index].InsertRecord(newRecords[i]);
                }
            }


            overflowBlocksCount = (splitOverflowBlocks.Count + newOverflowBlocks.Count) - overflowBlocksCount;
            TotalSpace += overflowBlocksCount * OverflowFile.BlockFactor;
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

    private void Merge()
    {
        while (MergeCondition())
        {


        }
    }

    public int Insert(T record)
    {
        int index = (record.GetHashCode() % ModFunction) < SplitPointer
            ? (record.GetHashCode() % (ModFunction * 2))
            : (record.GetHashCode() % ModFunction);
        int nextBlockIndex;

        PrimaryHashBlock<T> primaryBlock = (PrimaryHashBlock<T>)PrimaryFile.LoadBlockFromFile(record, index);  // načítanie bloku a seek na pozíciu, ak tam je miesto tak sa potom rovno blok uloží sem
        OverflowHashBlock<T> overflowBlock;

        if (primaryBlock.InsertRecord(record))  // ak sa záznam vložil do bloku - je tam voľné miesto, inak je false a ide sa na overflow bloky
        {
            PrimaryFile.InsertAt(index, primaryBlock);
            primaryBlock.TotalRecordsCount++;
            TotalRecordsCount++;
            return index;
        }
        if (primaryBlock.NextBlockIndex == -1)  // blok je plný ale nemá preplňovací blok - vytvoríme nový a zapíšeme na index -1, takže sa v HeapFile zapíše do nového voľného miesta
        {
            overflowBlock = new OverflowHashBlock<T>(OverflowFile.BlockSize, record, true);  // vytvorí sa nový blok - vyplní sa vkladaným záznamom a valid count = 1
            nextBlockIndex = OverflowFile.InsertAt(-1, overflowBlock);
            primaryBlock.NextBlockIndex = nextBlockIndex;
            primaryBlock.TotalRecordsCount++;
            TotalRecordsCount++;
            TotalSpace += (OverflowFile.BlockFactor - 1);
            return index;
        }

        overflowBlock = (OverflowHashBlock<T>)OverflowFile.LoadBlockFromFile(record, primaryBlock.NextBlockIndex);
        do   // to isté, ale teraz cez zreťazenie overflow blokov -> hľadanie voľného miesta, prípadne sa spraví nový blok
        {
            if (overflowBlock.InsertRecord(record))
            {
                OverflowFile.InsertAt(index, overflowBlock);
                break;
            }
            if (overflowBlock.NextBlockIndex == -1)
            {
                OverflowHashBlock<T> newOverflowBlock = new OverflowHashBlock<T>(OverflowFile.BlockSize, record, true);
                nextBlockIndex = OverflowFile.InsertAt(-1, newOverflowBlock);
                overflowBlock.NextBlockIndex = nextBlockIndex;
                TotalSpace += (OverflowFile.BlockFactor - 1);
                break;
            }
            overflowBlock = (OverflowHashBlock<T>)OverflowFile.LoadBlockFromFile(record, overflowBlock.NextBlockIndex);
        } 
        while (overflowBlock.NextBlockIndex != -1);  // prejdenie zreťazenia overflow blokov až na koniec
        primaryBlock.TotalRecordsCount++;
        TotalRecordsCount++;

        if (SplitCondition())
        {
            Split(record);
        }

        return index;
    }

    public T? Get(T record)
    {
        int index = (record.GetHashCode() % ModFunction) < SplitPointer
            ? (record.GetHashCode() % (ModFunction * 2))
            : (record.GetHashCode() % ModFunction);

        if (PrimaryFile.CheckIndex(index))
        {
            return default;
        }

        PrimaryHashBlock<T> primaryBlock = (PrimaryHashBlock<T>)PrimaryFile.LoadBlockFromFile(record, index);
        T? foundRecord = PrimaryFile.TryToFindRecord(primaryBlock, record);   // skúsi sa nájsť v primárnom bloku záznam, ak tam nie je a existuje index na overflow blok, tak sa to skúsi tam
        if (foundRecord != null)
        {
            return foundRecord;
        }
        if (primaryBlock.NextBlockIndex == -1)
        {
            return default;
        }
        OverflowHashBlock<T> overflowBlock = (OverflowHashBlock<T>)OverflowFile.LoadBlockFromFile(record, primaryBlock.NextBlockIndex);
        foundRecord = OverflowFile.TryToFindRecord(overflowBlock, record);
        if (foundRecord != null)
        {
            return foundRecord;
        }

        while (overflowBlock.NextBlockIndex == -1)
        {
            overflowBlock = (OverflowHashBlock<T>)OverflowFile.LoadBlockFromFile(record, primaryBlock.NextBlockIndex);  // prejdenie zreťazenia overflow blokov a pokus o nájdenie záznamu
            foundRecord = OverflowFile.TryToFindRecord(overflowBlock, record);

            if (foundRecord != null)
            {
                return foundRecord;
            }
        }

        return foundRecord;  // v tomto bode bude return vždy null
    }

    public void Update(T record)
    {
        T? findRecord = Get(record);
        if (findRecord == null)
        {
            return;
        }
    }

    public int Delete(T record)
    {
        int index = (record.GetHashCode() % ModFunction) < SplitPointer
            ? (record.GetHashCode() % (ModFunction * 2))
            : (record.GetHashCode() % ModFunction);

        if (PrimaryFile.CheckIndex(index))
        {
            return default;
        }



        if (MergeCondition())
        {
            Merge();
        }

        return index;
    }
}