using SEM_2_CORE.Interfaces;

namespace SEM_2_CORE;

public class Block<T> : IByteOperations where T : IDataClassOperations<T>, IByteOperations
{
    public List<T> RecordsList { get; set; }
    public int RecordsCount { get; set; }
    public int ValidCount { get; set; }
    public T DataInstance { get; set; }

    public Block(int blockFactor, T instance, bool newBlock = false)
    {
        RecordsCount = blockFactor;
        ValidCount = 0;
        RecordsList = new List<T>();
        DataInstance = instance;

        if (newBlock)
        {
            ValidCount++;
            for (int i = 0; i < RecordsCount; i++)
            {
                RecordsList.Add(DataInstance.CreateClass());  // keď sa insertuje nový blok, teda nenačíta sa zo súboru, tak sa naplní inštanciou toho záznamu, ktorý tam ide, použije sa pri inserte
            }
        }
    }

    public override string ToString()
    {
        string result = "";
        for (int i = 0; i < RecordsCount; i++)
        {
            result += $"Record {i + 1}:\n";
            result += RecordsList[i].ToString();
            result += "--------------------\n";
        }
        return result;
    }

    public int GetSize()
    {
        int size = sizeof(int); // ínt pre ValidCount, následne jednotlivé záznamy
        size += RecordsCount * DataInstance.GetSize();
        return size;
    }

    public byte[] GetBytes()
    {
        List<byte> byteBuffer = new List<byte>();
        byteBuffer.AddRange(BitConverter.GetBytes(ValidCount));  // konverzia valid count

        for (int i = 0; i < RecordsCount; i++)
        {
            byteBuffer.AddRange(RecordsList[i].GetBytes());
        }

        return byteBuffer.ToArray();
    }

    public void FromBytes(byte[] bytes)
    {
        int position = 0, step = DataInstance.GetSize();
        ValidCount = BitConverter.ToInt32(bytes.AsSpan(position, 4).ToArray()); // prečíta sa valid count a nastaví sa na prvý záznam
        position = 4;

        for (int i = 0; i < RecordsCount; i++) // postupne sa načítajú záznamy po jednom zavolaním FromBytes na ich inštanciách
        {
            T readRecord = DataInstance.CreateClass();
            readRecord.FromBytes(bytes.AsSpan(position, step).ToArray());
            RecordsList.Add(readRecord);
            position += step;
        }
    }

    public void InsertRecord(T record)
    {
        if (ValidCount < RecordsCount)
        {
            RecordsList[ValidCount] = record.CreateClass();
            ValidCount++;
            return;
        }

        Console.WriteLine($"Cannot insert record, block is full with valid count {ValidCount} and blocking factor {RecordsCount}");
    }

    public void DeleteRecord(T record)
    {
        int recordIndex = -1;
        for (int i = 0; i < RecordsList.Count; i++)
        {
            if (record.Equals(RecordsList[i]))
            {
                recordIndex = i;
                break;
            }
        }

        if (recordIndex == -1)
        {
            Console.WriteLine("Record to delete not found in block.");
            return;
        }

        RecordsList[recordIndex] = RecordsList[ValidCount - 1].CreateClass();
        ValidCount--;
    }
}