using SEM_2_CORE.Interfaces;

namespace SEM_2_CORE.Files;

public class LinearHashFile<T> where T : IDataClassOperations<T>, IByteOperations
{
    private int ModFunction { get; set; }
    private uint SplitPointer { get; set; } = 0;
    private HeapFile<T> PrimaryFile { get; set; }
    private HeapFile<T> OverflowFile { get; set; }


    public LinearHashFile(int minMod, string primaryFilePath, string overflowFilePath, int primaryBlockSize, int overflowBlockSize, T dataInstance)
    {
        ModFunction = minMod;
        PrimaryFile = new HeapFile<T>(primaryFilePath, primaryBlockSize, dataInstance);
        OverflowFile = new HeapFile<T>(overflowFilePath, overflowBlockSize, dataInstance);
    }

    private bool SplitCondition()
    {
        return true;
    }

    private bool MergeCondition()
    {
        return true;
    }

    private void Split()
    {

    }

    private void Merge()
    {

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
            return index;
        }
        if (primaryBlock.NextBlockIndex == -1)  // blok je plný ale nemá preplňovací blok - vytvoríme nový a zapíšeme na index -1, takže sa v HeapFile zapíše do nového voľného miesta
        {
            overflowBlock = new OverflowHashBlock<T>(OverflowFile.BlockSize, record, true);  // vytvorí sa nový blok - vyplní sa vkladaným záznamom a valid count = 1
            nextBlockIndex = OverflowFile.InsertAt(-1, overflowBlock);
            primaryBlock.NextBlockIndex = nextBlockIndex;
            primaryBlock.TotalRecordsCount++;
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
                break;
            }
            overflowBlock = (OverflowHashBlock<T>)OverflowFile.LoadBlockFromFile(record, overflowBlock.NextBlockIndex);
        } 
        while (overflowBlock.NextBlockIndex != -1);  // prejdenie zreťazenia overflow blokov až na koniec
        primaryBlock.TotalRecordsCount++;

        if (SplitCondition())
        {
            Split();
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