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

    public void Insert(T record)
    {
        int index = (record.GetHashCode() % ModFunction) < SplitPointer
            ? (record.GetHashCode() % (ModFunction * 2))
            : (record.GetHashCode() % ModFunction);

        PrimaryHashBlock<T> primaryBlock = (PrimaryHashBlock<T>)PrimaryFile.LoadBlockFromFile(record, index);

        if (primaryBlock.InsertRecord(record))
        {
            return;
        }

    }

    public T? Get(T record)
    {
        int index = (record.GetHashCode() % ModFunction) < SplitPointer
            ? (record.GetHashCode() % (ModFunction * 2))
            : (record.GetHashCode() % ModFunction);

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

    public void Delete(T record)
    {

    }
}