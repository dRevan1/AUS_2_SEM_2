using SEM_2_CORE.Files;
using System.Xml.Schema;

namespace SEM_2_CORE.Testers;

public class LinearHashFileTester
{

    private List<int> FreeBlocks = new List<int>();
    private List<PrimaryHashBlock<Person>> PrimaryFile = new List<PrimaryHashBlock<Person>>();
    private List<PrimaryHashBlock<Person>> OverflowFile = new List<PrimaryHashBlock<Person>>();
    private int Mod;
    private int SplitPointer = 0;
    private int TotalSpace = 0;
    private int TotalRecords = 0;
    private int PrimaryBlockFactor = 0;
    private int OverflowBlockFactor = 0;
    private int TotalChainLength = 0;
    private int UsedPrimaryBlocks = 0;

    private DateTime RandomDate(DateTime start, DateTime end)
    {
        Random rand = new Random();
        int range = (end - start).Days;
        return start.AddDays(rand.Next(range));
    }

    private int HashRecord(Person person)
    {
        int hash = person.GetHashCode();
        int primary = hash % Mod;
        int index = primary < SplitPointer
            ? hash % (Mod * 2)
            : primary;

        return index;
    }

    private int GetTotalSpace()
    {
        return (PrimaryFile.Count * PrimaryBlockFactor) + (OverflowFile.Count * OverflowBlockFactor);
    }

    // kombinácia metód create a write sequence z linear hash file
    private int WriteSequence(List<Person> recordsList, PrimaryHashBlock<Person> primaryBlock, Person record, int primaryIndex, bool append = false)
    {
        List<PrimaryHashBlock<Person>> overflowBlocks = new List<PrimaryHashBlock<Person>>();
        PrimaryHashBlock<Person> overflowBlock;
        int index;
        for (int i = 0; i < recordsList.Count; i++)  // naplnenie primárneho bloku
        {
            if (i < primaryBlock.RecordsCount)
            {
                primaryBlock.InsertRecord(recordsList[i]);
            }
            else   // keď sa naplní, tak ostatné sa pridávajú do jeho preplňovacích, ktoré treba vytvoriť
            {
                index = (i - PrimaryBlockFactor) / OverflowBlockFactor;
                if (index == overflowBlocks.Count)
                {
                    overflowBlock = new PrimaryHashBlock<Person>(OverflowBlockFactor, record, emptyBlock: true);
                    overflowBlocks.Add(overflowBlock);
                }
                overflowBlocks[index].InsertRecord(recordsList[i]);
            }
        }

        index = -1;
        if (overflowBlocks.Count > 0)
        {
            for (int i = overflowBlocks.Count - 1; i >= 0; i--)
            {
                overflowBlocks[i].NextBlockIndex = index;
                index = InsertBlock(overflowBlocks[i]);
            }
        }
        primaryBlock.NextBlockIndex = index;
        if (append == true)
        {
            PrimaryFile.Add(primaryBlock);
        }
        else
        {
            PrimaryFile[primaryIndex] = primaryBlock;
        }

        return overflowBlocks.Count;
    }

    private void TruncateFile()
    {
        if (FreeBlocks.Count > 0)
        {
            if (OverflowFile.Last().ValidCount != 0)
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
        if (FreeBlocks.Count == 1)
        {
            FreeBlocks.Clear();
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
    }

    private int InsertBlock(PrimaryHashBlock<Person> block, int index = -1)
    {
        int insertIndex;
        if (index == -1)
        {
            if (FreeBlocks.Count > 0)
            {
                insertIndex = FreeBlocks.First();
                OverflowFile[insertIndex] = block;
                FreeBlocks.RemoveAt(0);
                return insertIndex;
            }
            OverflowFile.Add(block);
            return OverflowFile.Count - 1;
        }
        OverflowFile[index] = block;
        return index;
    }

    // p1 je v testovacej štruktúre, p2 je zo súboru
    private bool ComparePerson(Person? p1, Person? p2, int block = -1, int record = -1, string customMsg = "")  // na testovanie pre istotu porovnanie všetkých načítaných hodnôt
    {
        if (p1 == null && p2 == null)
        {
            return true;
        }
        if (p1 == null && p2 != null)
        {
            Console.WriteLine($"Record mismatch at block {block}, record {record}:");
            Console.WriteLine($"Expected null record, got record with ID {p2.ID}");
            return false;
        }
        if (p1 != null && p2 == null)
        {
            Console.WriteLine($"Record mismatch at block {block}, record {record}:");
            Console.WriteLine($"Expected record with ID {p1.ID}, got null record");
            return false;
        }

        bool result = p1.Name == p2.Name &&
               p1.Surname == p2.Surname &&
               p1.DayOfBirth == p2.DayOfBirth &&
               p1.MonthOfBirth == p2.MonthOfBirth &&
               p1.YearOfBirth == p2.YearOfBirth &&
               p1.ID == p2.ID;
        for (int i = 0; i < p1.Tests.Length; i++)
        {
            if (p1.Tests[i] != p2.Tests[i])
            {
                result = false;
                break;
            }
        }

        if (!result)
        {
            if (string.IsNullOrWhiteSpace(customMsg))
            {
                Console.WriteLine($"Record mismatch at block {block}, record {record}:");
            }
            else
            {
                Console.WriteLine(customMsg);
            }
            Console.WriteLine($"Name inserted: {p1.Name}, Name from file: {p2.Name}");
            Console.WriteLine($"Surname inserted: {p1.Surname} , Surname from file:  {p2.Surname}");
            Console.WriteLine($"Day of birth inserted: {p1.DayOfBirth} , Day of birth from file:  {p2.DayOfBirth}");
            Console.WriteLine($"Month of birth inserted: {p1.MonthOfBirth} , Month of birth from file:  {p2.MonthOfBirth}");
            Console.WriteLine($"Year of birth inserted: {p1.YearOfBirth} , Year of birth from file:  {p2.YearOfBirth}");
            Console.WriteLine($"ID inserted: {p1.ID} , ID from file:  {p2.ID}");
            for (int i = 0; i < p1.Tests.Length; i++)
            {
                Console.WriteLine($"Test {i+1} ID inserted: {p1.Tests[i]}, test {i+1} ID from file: {p2.Tests[i]}");
            }
        }

        return result;
    }

    private bool CheckFileContents(LinearHashFile<Person> linHashFile)
    {
        Person dataInstance = new Person("Gordon", "Freeman", 9, 4, 1995, "3");
        if (linHashFile.ModFunction != Mod)  // kontrola modulo
        {
            Console.WriteLine($"Linear hash file test failed - expected mod function: {Mod}, function in file: {linHashFile.ModFunction}");
            return false;
        }
        if (linHashFile.SplitPointer != SplitPointer)  // kontrola split pointer
        {
            Console.WriteLine($"Linear hash file test failed - expected split pointer: {SplitPointer}, split pointer in file: {linHashFile.SplitPointer}");
            return false;
        }
        if (linHashFile.TotalChainLength != TotalChainLength)
        {
            Console.WriteLine($"Linear hash file test failed - expected total chain length: {TotalChainLength}, total chain length in file: {linHashFile.TotalChainLength}");
            return false;
        }
        if (linHashFile.UsedPrimaryBlocks != UsedPrimaryBlocks)
        {
            Console.WriteLine($"Linear hash file test failed - expected used primary blocks count: {UsedPrimaryBlocks}, used primary blocks count in file: {linHashFile.UsedPrimaryBlocks}");
            return false;
        }
        if (linHashFile.OverflowFile.FreeBlocks.Count != FreeBlocks.Count)  // kontrola počet voľných blokov a následne jednotlivé indexy voľných blokov
        {
            Console.WriteLine($"Linear hash file test failed - free blocks count: {FreeBlocks.Count}, free blocks count in file: {linHashFile.OverflowFile.FreeBlocks.Count}");
            return false;
        }
        for (int i = 0; i < FreeBlocks.Count; i++)
        {
            if (FreeBlocks[i] != linHashFile.OverflowFile.FreeBlocks[i])  // kontrola indexov jednotlivých voľných blokov
            {
                Console.WriteLine($"Linear hash file test failed - free block index expected: {FreeBlocks[i]}, free block index in file: {linHashFile.OverflowFile.FreeBlocks[i]}");
                return false;
            }
        }

        List<PrimaryHashBlock<Person>> primaryFileContents = linHashFile.PrimaryFile.GetFileContents<PrimaryHashBlock<Person>>(dataInstance);
        List<OverflowHashBlock<Person>> overflowFileContents = linHashFile.OverflowFile.GetFileContents<OverflowHashBlock<Person>>(dataInstance);
        if (primaryFileContents.Count != PrimaryFile.Count)
        {
            Console.WriteLine($"Linear hash file test failed - expected primary file count: {PrimaryFile.Count}, primary count in file: {primaryFileContents.Count}");
            return false;
        }
        if (overflowFileContents.Count != OverflowFile.Count)
        {
            Console.WriteLine($"Linear hash file test failed - expected overflow file count: {OverflowFile.Count}, overflow count in file: {overflowFileContents.Count}");
            return false;
        }
        for (int i = 0; i < PrimaryFile.Count; i++)
        {
            if (PrimaryFile[i].ValidCount != primaryFileContents[i].ValidCount)
            {
                Console.WriteLine($"Linear hash file test failed at primary block {i} - expected valid count: {PrimaryFile[i].ValidCount}, valid count in file: {primaryFileContents[i].ValidCount}");
                return false;
            }
            if (PrimaryFile[i].NextBlockIndex != primaryFileContents[i].NextBlockIndex)
            {
                Console.WriteLine($"Linear hash file test failed at primary block {i} - expected next block index: {PrimaryFile[i].NextBlockIndex}, next block index in file: {primaryFileContents[i].NextBlockIndex}");
                return false;
            }
            if (PrimaryFile[i].TotalRecordsCount != primaryFileContents[i].TotalRecordsCount)
            {
                Console.WriteLine($"Linear hash file test failed at primary block {i} - expected total records count: {PrimaryFile[i].TotalRecordsCount}, total records count in file: {primaryFileContents[i].TotalRecordsCount}");
                return false;
            }
            for (int j = 0; j < PrimaryFile[i].ValidCount; j++)
            {
                if (!ComparePerson(PrimaryFile[i].RecordsList[j], primaryFileContents[i].RecordsList[j], block:i, record:j))
                {
                    return false;
                }
            }
        }
        for (int i = 0; i < OverflowFile.Count; i++)
        {
            if (OverflowFile[i].ValidCount != overflowFileContents[i].ValidCount)
            {
                Console.WriteLine($"Linear hash file test failed at overflow block {i} - expected valid count: {OverflowFile[i].ValidCount}, valid count in file: {overflowFileContents[i].ValidCount}");
                return false;
            }
            if (OverflowFile[i].NextBlockIndex != overflowFileContents[i].NextBlockIndex)
            {
                Console.WriteLine($"Linear hash file test failed at overflow block {i} - expected next block index: {OverflowFile[i].NextBlockIndex}, next block index in file: {overflowFileContents[i].NextBlockIndex}");
                return false;
            }
            for (int j = 0; j < OverflowFile[i].ValidCount; j++)
            {
                if (!ComparePerson(OverflowFile[i].RecordsList[j], overflowFileContents[i].RecordsList[j], block:i, record:j))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void GetBenchmark(LinearHashFile<Person> linHashFile)
    {
        Console.WriteLine($"Average chain length: {(double)linHashFile.TotalChainLength / (double)linHashFile.UsedPrimaryBlocks}");
        Console.WriteLine($"Total chain length: {linHashFile.TotalChainLength}");
        Console.WriteLine($"Used primary blocks count: {linHashFile.UsedPrimaryBlocks}");
        Console.WriteLine($"Used space ratio/total space ratio: {(double)linHashFile.TotalRecordsCount / (double)linHashFile.TotalSpace}");
    }

    // kontorla výsledkov getov, index je index na ktorý bol záznam zahešovaný a expected je osoba, ktorá sa hľadala, actual osoba, ktorá sa našla
    private bool CheckGetResults(List<(int, Person?, Person?)> successfulGetList, List<(int, Person?, Person?)> failedGetList)
    {
        foreach (var (index, expected, actual) in successfulGetList)
        {
            if (!ComparePerson(expected, actual, customMsg:"Linear hash file test failed at successful get:"))
            {
                return false;
            }
        }

        foreach (var (index, expected, actual) in failedGetList)
        {
            if (actual != null)
            {
                Console.WriteLine($"Linear hash file test failed at unsuccessful get - expected null, got person with ID {actual.ID}");
                return false;
            }
        }

        return true;
    }


    private bool SplitCondition()
    {
        double loadFactor = (double)TotalRecords / (double)TotalSpace;
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

    private void Split(Person record)
    {
        while (SplitCondition())
        {
            PrimaryHashBlock<Person> splitBlock = PrimaryFile[SplitPointer];
            PrimaryHashBlock<Person> block;
            PrimaryHashBlock<Person> newBlock = new PrimaryHashBlock<Person>(PrimaryBlockFactor, record, emptyBlock: true);
            List<Person> splitRecords = new List<Person>();
            List<Person> newRecords = new List<Person>();

            block = splitBlock;
            int index;
            while (true)
            {
                for (int i = 0; i < block.ValidCount; i++)
                {
                    int hash = block.RecordsList[i].GetHashCode();
                    hash %= (Mod * 2);
                    if (hash != SplitPointer)
                    {
                        newRecords.Add(block.RecordsList[i]);
                    }
                    else
                    {
                        splitRecords.Add(block.RecordsList[i]);
                    }
                }
                if (block.NextBlockIndex == -1)
                {
                    break;
                }
                index = block.NextBlockIndex;
                block = OverflowFile[index];
                TotalChainLength--;
                FreeBlocks.Add(index);
            }

            splitBlock.ValidCount = 0;
            FreeBlocks.Sort();
            splitBlock.TotalRecordsCount = (uint)splitRecords.Count;
            newBlock.TotalRecordsCount = (uint)newRecords.Count;
            int splitOverflowBlocks = WriteSequence(splitRecords, splitBlock, record, SplitPointer);
            int newOverflowBlocks = WriteSequence(newRecords, newBlock, record, SplitPointer + Mod, append: true);
            TruncateFile();
            TotalSpace = GetTotalSpace();
            TotalChainLength += (splitOverflowBlocks + newOverflowBlocks);
            if (splitBlock.TotalRecordsCount > 0 && newBlock.TotalRecordsCount > 0)
            {
                UsedPrimaryBlocks++;
            }

            if (SplitPointer + 1 == Mod)
            {
                Mod *= 2;
                SplitPointer = 0;
            }
            else
            {
                SplitPointer++;
            }
        }
    }

    public Person GeneratePerson(int id, DateTime start = default, DateTime end = default)
    {
        DateTime randomDate = (start == default || end == default) ? RandomDate(new DateTime(1960, 1, 1), new DateTime(2023, 12, 31)) : RandomDate(start, end);
        byte day = (byte)randomDate.Day;
        byte month = (byte)randomDate.Month;
        ushort year = (ushort)randomDate.Year;
        Person person = new Person("Name" + id, "Surname" + id, day, month, year, id.ToString());

        return person;
    }

    public PCRTest GeneratePCRTest(uint testID, int personID, DateTime start = default, DateTime end = default)
    {
        DateTime randomDate = (start == default || end == default) ? RandomDate(new DateTime(1960, 1, 1), new DateTime(2023, 12, 31)) : RandomDate(start, end);
        Random rand = new Random();
        byte day = (byte)randomDate.Day;
        byte month = (byte)randomDate.Month;
        ushort year = (ushort)randomDate.Year;
        byte minute = (byte)randomDate.Minute;
        byte hour = (byte)randomDate.Hour;
        bool result = (rand.Next() < 0.5) ? true : false;
        double value = rand.Next() * 100;
        string note = $"Note {testID}";

        PCRTest test = new PCRTest(day, month, year, minute, hour, personID.ToString(), testID, result, value, note);

        return test;
    }

    public void LinearHashTest(LinearHashFile<Person> linHashFile, int operations)
    {
        PrimaryHashBlock<Person> block;
        Person dataInstance = new Person("Gordon", "Freeman", 9, 4, 1995, "3");
        PrimaryBlockFactor = linHashFile.PrimaryFile.BlockFactor;
        OverflowBlockFactor = linHashFile.OverflowFile.BlockFactor;
        TotalSpace += PrimaryBlockFactor * linHashFile.ModFunction;
        Mod = linHashFile.ModFunction;

        List<Person> insertedPeople = new List<Person>();
        List<(int, Person?, Person?)> successfulGetList = new List<(int, Person?, Person?)>(); // zoznam hľadaní, ktoré vrátia osobu
        List<(int, Person?, Person?)> failedGetList = new List<(int, Person?, Person?)>();  // zoznam hľadaní, ktoré osobu nenájdu - vrátia null


        for (int i = 0; i < Mod; i++)
        {
            PrimaryFile.Add(new PrimaryHashBlock<Person>(PrimaryBlockFactor, dataInstance, emptyBlock: true));
        }

        Random rand = new Random();
        for (int i = 0; i < operations; i++)
        {
            double operationType = rand.NextDouble();
            Person record = GeneratePerson(i), randomRecord;
            int index = HashRecord(record);

            if (operationType < 0.5)  // INSERT
            {
                insertedPeople.Add(record);
                block = PrimaryFile[index];
                block.TotalRecordsCount++;
                if (!block.InsertRecord(record))
                {
                    if (block.NextBlockIndex != -1)
                    {
                        while (block.NextBlockIndex != -1)
                        {
                            block = OverflowFile[block.NextBlockIndex];
                            if (block.InsertRecord(record))
                            {
                                break;
                            }
                            if (block.NextBlockIndex == -1)
                            {
                                PrimaryHashBlock<Person> newBlock = new PrimaryHashBlock<Person>(OverflowBlockFactor, record, newBlock: true);
                                block.NextBlockIndex = InsertBlock(newBlock);
                                TotalSpace += OverflowBlockFactor;
                                TotalChainLength++;
                                break;
                            }
                        }
                    }
                    else
                    {
                        PrimaryHashBlock<Person> newBlock = new PrimaryHashBlock<Person>(OverflowBlockFactor, record, newBlock: true);
                        block.NextBlockIndex = InsertBlock(newBlock);
                        TotalSpace += OverflowBlockFactor;
                        TotalChainLength++;
                    }
                }
                else if (block.TotalRecordsCount == 1)  // ak sa záznam vložil do bloku a je to jeho prvý záznam - počet použitých blokov +1
                {
                    UsedPrimaryBlocks++;
                }
                TotalRecords++;
                linHashFile.Insert(record);

                if (SplitCondition())
                {
                    Split(record);
                }
            }
            else   // GET
            {
                double successfulGetChance = rand.NextDouble();
                Person? foundPerson;

                if (successfulGetChance < 0.5)  // úspešný GET
                {
                    if (insertedPeople.Count > 0)
                    {
                        randomRecord = insertedPeople[rand.Next(insertedPeople.Count)];
                        index = HashRecord(randomRecord);
                        foundPerson = linHashFile.Get(randomRecord);
                        successfulGetList.Add((index, randomRecord, foundPerson));
                    }
                    else
                    {
                        foundPerson = linHashFile.Get(record);
                        failedGetList.Add((index, record, foundPerson));
                    }
                }
                else  // neúspešný GET
                {
                    foundPerson = linHashFile.Get(record);
                    failedGetList.Add((index, record, foundPerson));
                }
            }
        }

        if (!CheckFileContents(linHashFile) && CheckGetResults(successfulGetList, failedGetList))
        {
            Console.WriteLine("Check file contents test failed");
            return;
        }
        if (!CheckGetResults(successfulGetList, failedGetList))
        {
            return;
        }
        Console.WriteLine($"Linear hash file test PASSED with {operations} operations:");
        Console.WriteLine($"Primary block factor - {linHashFile.PrimaryFile.BlockFactor}");
        Console.WriteLine($"Overflow block factor - {linHashFile.OverflowFile.BlockFactor}");

        GetBenchmark(linHashFile);
    }
}