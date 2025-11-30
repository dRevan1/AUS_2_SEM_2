using SEM_2_CORE.Interfaces;

namespace SEM_2_CORE.Files;

public class LinearHashFile<T> where T : IDataClassOperations<T>, IByteOperations
{
    public int ModFunction { get; set; }
    public int SplitPointer { get; set; } = 0;
    private HeapFile<T> PrimaryFile { get; set; }
    private HeapFile<T> OverflowFile { get; set; }
    private int TotalRecordsCount { get; set; } = 0;   // dočasne pre split podľa prednášky na testovanie a minimálnu hodnotu na benchmark
    private int TotalSpace { get; set; } = 0;


    public LinearHashFile(int minMod, string primaryFilePath, string overflowFilePath, int primaryBlockSize, int overflowBlockSize, T dataInstance)
    {
        ModFunction = minMod;
        PrimaryFile = new HeapFile<T>(primaryFilePath, primaryBlockSize, dataInstance, startSize: ModFunction, mode: 1);
        OverflowFile = new HeapFile<T>(overflowFilePath, overflowBlockSize, dataInstance, mode: 2);
        TotalSpace = minMod * PrimaryFile.BlockFactor;
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

    private void TruncateSequence()
    {

    }

    private bool SplitCondition()
    {
        return (TotalRecordsCount / TotalSpace) > 0.8;  // dočasne, aby sa dalo otestovať insert
    }

    private bool MergeCondition()
    {
        return true;
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
                int index = GetRecordIndex(splitBlock.RecordsList[i]);
                if (index == SplitPointer)
                {
                    splitRecords.Add(splitBlock.RecordsList[i]);
                }
                else
                {
                    newRecords.Add(splitBlock.RecordsList[i]);
                }
            }
            splitBlock.ValidCount = 0;

            int overflowBlocksCount = 0;
            if (splitBlock.NextBlockIndex != -1)   // ak má split blok preplňovacie bloky, ich záznamy sa tiež prehešujú a bloky sa načítajú
            {
                overflowBlock = OverflowFile.LoadBlockFromFile<OverflowHashBlock<T>>(record, splitBlock.NextBlockIndex);
                OverflowFile.FreeBlocks.Add(splitBlock.NextBlockIndex);
                overflowBlocksCount++;

                do
                {
                    for (int i = 0; i < overflowBlock.ValidCount; i++)
                    {
                        int index = GetRecordIndex(overflowBlock.RecordsList[i]);
                        if (index == SplitPointer)
                        {
                            splitRecords.Add(overflowBlock.RecordsList[i]);
                        }
                        else
                        {
                            newRecords.Add(overflowBlock.RecordsList[i]);
                        }
                    }
                    if (overflowBlock.NextBlockIndex != -1)
                    {
                        OverflowFile.FreeBlocks.Add(overflowBlock.NextBlockIndex);
                        overflowBlock = OverflowFile.LoadBlockFromFile<OverflowHashBlock<T>>(record, overflowBlock.NextBlockIndex);
                    }
                    overflowBlocksCount++;
                } while (overflowBlock.NextBlockIndex != -1);
                OverflowFile.FreeBlocks.Sort();
            }
            splitBlock.TotalRecordsCount = (uint)splitRecords.Count;
            newBlock.TotalRecordsCount = (uint)newRecords.Count;

            CreateSequence(splitRecords, splitBlock, splitOverflowBlocks, record); // zo zoznamu dát, ktoré majú zostať na split bloku sa vytvorí zreťazenie blokov na zápis
            WriteSequence(splitOverflowBlocks, splitBlock, SplitPointer); // zápis zreťazenia pre split block - na ktorý ukazuje split pointer

            CreateSequence(newRecords, newBlock, newOverflowBlocks, record);  // zo zoznamu dát, ktoré majú byť na novom bloku sa vytvorí zreťazenie blokov na zápis
            WriteSequence(newOverflowBlocks, newBlock, SplitPointer + ModFunction, append: true);  // zapíše sa zreťazenie blokov pre nový block "newBlock"
            
            overflowBlocksCount = (splitOverflowBlocks.Count + newOverflowBlocks.Count) - overflowBlocksCount;
            TotalSpace += overflowBlocksCount * OverflowFile.BlockFactor + PrimaryFile.BlockFactor;

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

    private void Merge()
    {
        while (MergeCondition())
        {


        }
    }

    private int GetRecordIndex(T record)
    {
        int hash = record.GetHashCode();
        uint uhash = (uint)hash;

        int index = (uhash % (uint)ModFunction) < SplitPointer
            ? (int)(uhash % (uint)(ModFunction * 2))
            : (int)(uhash % (uint)ModFunction);

        return index;
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
            TotalSpace += (OverflowFile.BlockFactor - 1);
            if (SplitCondition())
            {
                Split(record);
            }
            return index;
        }

        overflowBlock = OverflowFile.LoadBlockFromFile<OverflowHashBlock<T>>(record, primaryBlock.NextBlockIndex);
        nextBlockIndex = primaryBlock.NextBlockIndex;
        do   // to isté, ale teraz cez zreťazenie overflow blokov -> hľadanie voľného miesta, prípadne sa spraví nový blok
        {
            if (overflowBlock.InsertRecord(record))
            {
                OverflowFile.InsertAt(nextBlockIndex, overflowBlock);
                break;
            }
            if (overflowBlock.NextBlockIndex == -1)
            {
                OverflowHashBlock<T> newOverflowBlock = new OverflowHashBlock<T>(OverflowFile.BlockFactor, record, true);
                int lastIndex = OverflowFile.InsertAt(-1, newOverflowBlock);
                overflowBlock.NextBlockIndex = lastIndex;
                OverflowFile.InsertAt(nextBlockIndex, overflowBlock);
                TotalSpace += (OverflowFile.BlockFactor - 1);
                break;
            }
            nextBlockIndex = overflowBlock.NextBlockIndex;
            overflowBlock = OverflowFile.LoadBlockFromFile<OverflowHashBlock<T>>(record, overflowBlock.NextBlockIndex);
        } while (overflowBlock.NextBlockIndex != -1);  // prejdenie zreťazenia overflow blokov až na koniec
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
        int index = GetRecordIndex(record);

        if (PrimaryFile.CheckIndex(index))
        {
            return default;
        }

        PrimaryHashBlock<T> primaryBlock = PrimaryFile.LoadBlockFromFile<PrimaryHashBlock<T>>(record, index);
        T? foundRecord = PrimaryFile.TryToFindRecord(primaryBlock, record);   // skúsi sa nájsť v primárnom bloku záznam, ak tam nie je a existuje index na overflow blok, tak sa to skúsi tam
        if (foundRecord != null)
        {
            return foundRecord;
        }
        if (primaryBlock.NextBlockIndex == -1)
        {
            return default;
        }
        OverflowHashBlock<T> overflowBlock = OverflowFile.LoadBlockFromFile<OverflowHashBlock<T>>(record, primaryBlock.NextBlockIndex);
        foundRecord = OverflowFile.TryToFindRecord(overflowBlock, record);
        if (foundRecord != null)
        {
            return foundRecord;
        }

        while (overflowBlock.NextBlockIndex == -1)
        {
            overflowBlock = OverflowFile.LoadBlockFromFile<OverflowHashBlock<T>>(record, primaryBlock.NextBlockIndex);  // prejdenie zreťazenia overflow blokov a pokus o nájdenie záznamu
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
        int index = GetRecordIndex(record);

        if (PrimaryFile.CheckIndex(index))
        {
            Console.WriteLine("Cannot delete - record not found.");
            return -1;
        }
        PrimaryHashBlock<T> primaryBlock = PrimaryFile.LoadBlockFromFile<PrimaryHashBlock<T>>(record, index);
        List<OverflowHashBlock<T>> overflowBlocks = new List<OverflowHashBlock<T>>();

        T? foundRecord = PrimaryFile.TryToFindRecord(primaryBlock, record);   // skúsi sa nájsť v primárnom bloku záznam, ak tam nie je a existuje index na overflow blok, tak sa to skúsi tam
        if (foundRecord != null)
        {
            primaryBlock.DeleteRecord(foundRecord);   // asi prepísať delete v primary blocku aby dával --
            primaryBlock.TotalRecordsCount--;
        }
        else if (primaryBlock.NextBlockIndex == -1)
        {
            Console.WriteLine("Cannot delete - record not found.");
            return -1;
        }

        OverflowHashBlock<T> overflowBlock = OverflowFile.LoadBlockFromFile<OverflowHashBlock<T>>(record, primaryBlock.NextBlockIndex);
        if (foundRecord == null)
        {
            do
            {
                foundRecord = OverflowFile.TryToFindRecord(overflowBlock, record);
                overflowBlocks.Add(overflowBlock);
                if (foundRecord != null)
                {
                    overflowBlock.DeleteRecord(foundRecord);
                    primaryBlock.TotalRecordsCount--;
                    break;
                }
                if (primaryBlock.NextBlockIndex != -1)
                {
                    overflowBlock = OverflowFile.LoadBlockFromFile<OverflowHashBlock<T>>(record, overflowBlock.NextBlockIndex);  // prejdenie zreťazenia overflow blokov a pokus o nájdenie záznamu
                }

            } while (overflowBlock.NextBlockIndex != -1);
        }

        if (foundRecord == null)  // záznam sa nenašiel ani v preplňovacích blokoch
        {
            Console.WriteLine("Cannot delete - record not found.");
            return -1;
        }

        // if else - treba striasenie - zapíše sa tam, inak zapísať ten blok, kde sa mazalo - asi pridať nejaký indikátor alebo tak nejak


        if (MergeCondition())
        {
            Merge();
        }

        return index;
    }
}