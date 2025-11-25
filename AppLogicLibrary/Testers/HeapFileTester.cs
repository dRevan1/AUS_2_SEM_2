namespace SEM_2_CORE.Testers;

public class HeapFileTester
{
    public List<List<Person>> InsertData(HeapFile<Person> heapFile, int dataCount)  // naplní file a vráti list "blockov" (list listov)
    {
        Person dataInstance = new Person("Gordon", "Freeman", 9, 4, 1995, "3");
        List<List<Person>> insertedBlocksRecord = new List<List<Person>>();
        int blockFactor = heapFile.BlockFactor;
        for (int i = 0; i < dataCount; i++)
        {
            DateTime randomDate = RandomDate(new DateTime(1960, 1, 1), new DateTime(2023, 12, 31));
            byte day = (byte)randomDate.Day;
            byte month = (byte)randomDate.Month;
            ushort year = (ushort)randomDate.Year;

            Person person = new Person("Name" + i, "Surname" + i, day, month, year, i.ToString());
            int index = i % blockFactor;

            if (index == 0)
            {
                insertedBlocksRecord.Add(new List<Person>());
            }
            insertedBlocksRecord.Last().Add(person);
            heapFile.Insert(person);
        }

        return insertedBlocksRecord;
    }

    private DateTime RandomDate(DateTime start, DateTime end)
    {
        Random rand = new Random();
        int range = (end - start).Days;
        return start.AddDays(rand.Next(range));
    }

    private bool ComparePerson(Person? p1, Person? p2)  // na testovanie pre istotu porovnanie všetkých načítaných hodnôt
    {
        if (p1 == null && p2 == null)
        {
            return true;
        }
        if (p1 == null || p2 == null)
        {
            return false;
        }
        return p1.Name == p2.Name &&
               p1.Surname == p2.Surname &&
               p1.DayOfBirth == p2.DayOfBirth &&
               p1.MonthOfBirth == p2.MonthOfBirth &&
               p1.YearOfBirth == p2.YearOfBirth &&
               p1.ID == p2.ID;
    }

    public void InsertTest(string filePath, int blockSize, int insertCount)
    {
        Person dataInstance = new Person("Gordon", "Freeman", 9, 4, 1995, "3");
        HeapFile<Person> heapFile = new HeapFile<Person>(filePath, blockSize, dataInstance);
        List<List<Person>> insertedBlocksRecord = InsertData(heapFile, insertCount);  // naplní sa file, zatvorí a vráti sa zoznam na kontrolu
        List<Block<Person>> insertedBlocksActual = heapFile.GetFileContents(dataInstance);

        if (insertedBlocksActual.Count != insertedBlocksRecord.Count)
        {
            Console.WriteLine($"Insert test failed - block counts are not equal - record {insertedBlocksRecord.Count}, actual in file {insertedBlocksActual}.");
            return;
        }

        for (int i = 0; i < insertedBlocksActual.Count; i++)
        {
            if (insertedBlocksActual[i].RecordsList.Count != heapFile.BlockFactor)
            {
                Console.WriteLine($"Block at index {i} has incorrect record count - {insertedBlocksActual[i].RecordsList.Count}, block factor is {heapFile.BlockFactor}.");
                return;
            }
            if (insertedBlocksActual[i].ValidCount != insertedBlocksRecord[i].Count)
            {
                Console.WriteLine($"Block at index {i} has incorrect valid records - {insertedBlocksActual[i].ValidCount}, inserted {insertedBlocksRecord[i].Count}.");
                return;
            }
            for (int j = 0; j < insertedBlocksRecord[i].Count; j++)
            {
                if (!ComparePerson(insertedBlocksActual[i].RecordsList[j], insertedBlocksRecord[i][j]))
                {
                    Console.WriteLine($"Record mismatch at block {i}, record {j}:");
                    Console.WriteLine($"Name inserted: {insertedBlocksRecord[i][j].Name}, Name from file: {insertedBlocksActual[i].RecordsList[j].Name}");
                    Console.WriteLine($"Surname inserted: {insertedBlocksRecord[i][j].Surname}, Surname from file: {insertedBlocksActual[i].RecordsList[j].Surname}");
                    Console.WriteLine($"Day of birth inserted: {insertedBlocksRecord[i][j].DayOfBirth}, Day of birth from file: {insertedBlocksActual[i].RecordsList[j].DayOfBirth}");
                    Console.WriteLine($"Month of birth inserted: {insertedBlocksRecord[i][j].MonthOfBirth}, Month of birth from file: {insertedBlocksActual[i].RecordsList[j].MonthOfBirth}");
                    Console.WriteLine($"Year of birth inserted: {insertedBlocksRecord[i][j].YearOfBirth}, Year of birth from file: {insertedBlocksActual[i].RecordsList[j].YearOfBirth}");
                    Console.WriteLine($"ID inserted: {insertedBlocksRecord[i][j].ID}, ID from file: {insertedBlocksActual[i].RecordsList[j].ID}");
                    return;
                }
            }
        }

        Console.WriteLine($"Insert test passed, path - {filePath}, block size - {blockSize}, insert count - {insertCount}.");
    }

    public void GetTest(string filePath, int blockSize, int dataCount, int getCount)
    {
        Person dataInstance = new Person("Gordon", "Freeman", 9, 4, 1995, "3");
        HeapFile<Person> heapFile = new HeapFile<Person>(filePath, blockSize, dataInstance);
        List<List<Person>> insertedBlocksRecord = InsertData(heapFile, dataCount);

        List<(int, Person?, Person?)> successfulGetList = new List<(int, Person?, Person?)>();  // zoznam, kde každý prvok je hľadaná osoba na indexe a výsledok hľadania
        List<(int, Person?, Person?)> failedGetList = new List<(int, Person?, Person?)>();  // rovnaký zoznam pre vyhľadávania, ktoré majú zlyhať - nesprávny index alebo neexistujúci záznam

        Random rand = new Random();
        for (int i = 0; i < getCount; i++)
        {
            double successfulGetChance = rand.NextDouble();
            if (successfulGetChance < 0.5) // get má byž neúspešný, teda zadá sa zlý index alebo záznam, ktorý sa na danom mmieste nenachádza
            {
                double incorrectIndex = rand.NextDouble();
                if (incorrectIndex < 0.5)  // index bude platný, priradíme nejaký neplatný záznam
                {
                    int blockIndex = rand.Next(0, insertedBlocksRecord.Count);
                    Person? foundPerson, invalidPerson = new Person("Invalid", "Person", 1, 1, 1900, (dataCount + 2).ToString());
                    foundPerson = heapFile.Get(blockIndex, invalidPerson);
                    failedGetList.Add((blockIndex, invalidPerson, foundPerson));
                }
                else  // index nebude platný
                {
                    int invalidBlockIndex = insertedBlocksRecord.Count + rand.Next(1, 10);
                    Person? foundPerson, getPerson = new Person("Name0", "Surname0", 10, 10, 2001, "0");
                    foundPerson = heapFile.Get(invalidBlockIndex, getPerson);
                    failedGetList.Add((invalidBlockIndex, getPerson, foundPerson));
                }
            }
            else // get má byť úspešný, vyberie sa jeden "block" a z neho záznam, ktorý by v ňom mal byť
            {
                int blockIndex = rand.Next(0, insertedBlocksRecord.Count);
                int recordIndex = rand.Next(0, insertedBlocksRecord[blockIndex].Count);
                Person? foundPerson, getPerson = insertedBlocksRecord[blockIndex][recordIndex];
                foundPerson = heapFile.Get(blockIndex, getPerson);
                successfulGetList.Add((blockIndex, getPerson, foundPerson));
            }
        }

        foreach (var (blockIndex, getPerson, foundPerson) in successfulGetList)  // kontrola či sa našli všetky osoby, ktoré sa mali
        {
            if (foundPerson == null)
            {
                Console.WriteLine($"Get test failed - successful get - block at index {blockIndex} - got null instead of valid result.");
                return;
            }
            if (!ComparePerson(getPerson, foundPerson))
            {
                Console.WriteLine($"Get test failed - successful get - block index {blockIndex} - found person ID {foundPerson.ID} is different than expected ID {getPerson!.ID}.");
                return;
            }
        }

        foreach (var (blockIndex, getPerson, foundPerson) in failedGetList) // kontrola, či všetky neúspešné gety vrátili null
        {
            if (foundPerson != null)
            {
                Console.WriteLine($"Get test failed - failed get - block at index {blockIndex} - expected null but got valid result with ID {foundPerson.ID}.");
                return;
            }
        }

        Console.WriteLine($"Get test passed, path - {filePath}, block size - {blockSize}, data count - {dataCount}, get count - {getCount}.");
    }

    public void DeleteTest(string filePath, int blockSize, int dataCount, int deleteCount)
    {
        Person dataInstance = new Person("Gordon", "Freeman", 9, 4, 1995, "3");
        HeapFile<Person> heapFile = new HeapFile<Person>(filePath, blockSize, dataInstance);
        List<List<Person>> insertedBlocksRecord = InsertData(heapFile, dataCount);
        List<(int, Person?, Person?)> successfulDeleteList = new List<(int, Person?, Person?)>();  // zoznam, kde každý prvok je mazaná osoba na indexe a vrátená inštancia mazanej osoby
        List<(int, Person?, Person?)> failedDeleteList = new List<(int, Person?, Person?)>();  // neúspešné mazania - neplatný index alebo neexistujúci záznam

        Random rand = new Random();
        for (int i = 0; i < deleteCount; i++)
        {
            double successfulDeleteChance = rand.NextDouble();
            if (successfulDeleteChance < 0.5) // delete má byž neúspešný, teda zadá sa zlý index alebo záznam, ktorý sa na danom mmieste nenachádza
            {
                double incorrectIndex = rand.NextDouble();
                if (incorrectIndex < 0.5)  // index bude platný, priradíme nejaký neplatný záznam
                {
                    int blockIndex = rand.Next(0, insertedBlocksRecord.Count);
                    Person? deletedPerson, invalidPerson = new Person("Invalid", "Person", 1, 1, 1900, (dataCount + 2).ToString());
                    deletedPerson = heapFile.Delete(blockIndex, invalidPerson);
                    failedDeleteList.Add((blockIndex, invalidPerson, deletedPerson));
                }
                else  // index nebude platný
                {
                    int invalidBlockIndex = insertedBlocksRecord.Count + rand.Next(1, 10);
                    Person? deletedPerson, personToDelete = new Person("Name0", "Surname0", 10, 10, 2001, "0");
                    deletedPerson = heapFile.Delete(invalidBlockIndex, personToDelete);
                    failedDeleteList.Add((invalidBlockIndex, personToDelete, deletedPerson));
                }
            }
            else // delete má byť úspešný, vyberie sa jeden "block" a z neho záznam, ktorý by v ňom mal byť
            {
                Person? deletedPerson;
                if (insertedBlocksRecord.Count == 0)
                {
                    Person? personToDelete = new Person("Invalid", "Person", 1, 1, 1900, (dataCount + 2).ToString());
                    deletedPerson = heapFile.Delete(0, personToDelete);
                    failedDeleteList.Add((0, personToDelete, deletedPerson));
                    continue;
                }

                int blockIndex = rand.Next(0, insertedBlocksRecord.Count);
                while (insertedBlocksRecord[blockIndex].Count == 0)  // ak je block prázdny, vyberieme iný
                {
                    blockIndex = rand.Next(0, insertedBlocksRecord.Count);
                }
                int recordIndex = rand.Next(0, insertedBlocksRecord[blockIndex].Count);
                Person? deletePerson = insertedBlocksRecord[blockIndex][recordIndex];
                deletedPerson = heapFile.Delete(blockIndex, deletePerson);
                successfulDeleteList.Add((blockIndex, deletePerson, deletedPerson));
                insertedBlocksRecord[blockIndex][recordIndex] = insertedBlocksRecord[blockIndex].Last().CreateClass();  // ako v súbore sa vymení, inak nebude test dobre fungovať
                insertedBlocksRecord[blockIndex].RemoveAt(insertedBlocksRecord[blockIndex].Count - 1);

                while (insertedBlocksRecord.Last().Count == 0)
                {
                    insertedBlocksRecord.RemoveAt(insertedBlocksRecord.Count - 1); // ako v samotnom súbore sa skráti o všetky prázdne "blocky" na konci listu
                    if (insertedBlocksRecord.Count == 0)
                    {
                        break;
                    }
                }
            }
        }

        List<Block<Person>> blocksAfterDelete = heapFile.GetFileContents(dataInstance);
        if (blocksAfterDelete.Count != insertedBlocksRecord.Count)
        {
            Console.WriteLine($"Delete test failed - block counts are not equal after delete - record {insertedBlocksRecord.Count}, actual in file {blocksAfterDelete.Count}.");
            return;
        }
        if (insertedBlocksRecord.Count == 0)
        {
            if (heapFile.FreeBlocks.Count > 0 || heapFile.PartiallyFreeBlocks.Count > 0)
            {
                Console.WriteLine($"Delete test failed - file is empty, but heap file lists contain {heapFile.FreeBlocks.Count} free blocks and {heapFile.PartiallyFreeBlocks.Count} partially free blocks.");
                return;
            }
        }

        for (int i = 0; i < insertedBlocksRecord.Count; i++)  // kontrola obsahu súboru, čo zostalo po mazaní, samotné záznamy sa kontrolujú iba platné, ale kontroluje sa aj celkový počet načítaných
        {
            if (blocksAfterDelete[i].RecordsList.Count != heapFile.BlockFactor)
            {
                Console.WriteLine($"Block at index {i} has incorrect record count - {blocksAfterDelete[i].RecordsList.Count}, block factor is {heapFile.BlockFactor}.");
                return;
            }
            if (blocksAfterDelete[i].ValidCount != insertedBlocksRecord[i].Count)
            {
                Console.WriteLine($"Block at index {i} has incorrect valid records - {blocksAfterDelete[i].ValidCount}, inserted {insertedBlocksRecord[i].Count}.");
                return;
            }
            for (int j = 0; j < insertedBlocksRecord[i].Count; j++)
            {
                if (!ComparePerson(blocksAfterDelete[i].RecordsList[j], insertedBlocksRecord[i][j]))
                {
                    Console.WriteLine($"Record mismatch at block {i}, record {j}:");
                    Console.WriteLine($"Name: {insertedBlocksRecord[i][j].Name}, Name from file: {blocksAfterDelete[i].RecordsList[j].Name}");
                    Console.WriteLine($"Surname: {insertedBlocksRecord[i][j].Surname}, Surname from file: {blocksAfterDelete[i].RecordsList[j].Surname}");
                    Console.WriteLine($"Day of birth: {insertedBlocksRecord[i][j].DayOfBirth}, Day of birth from file: {blocksAfterDelete[i].RecordsList[j].DayOfBirth}");
                    Console.WriteLine($"Month of birth: {insertedBlocksRecord[i][j].MonthOfBirth}, Month of birth from file: {blocksAfterDelete[i].RecordsList[j].MonthOfBirth}");
                    Console.WriteLine($"Year of birth: {insertedBlocksRecord[i][j].YearOfBirth}, Year of birth from file: {blocksAfterDelete[i].RecordsList[j].YearOfBirth}");
                    Console.WriteLine($"ID: {insertedBlocksRecord[i][j].ID}, ID from file: {blocksAfterDelete[i].RecordsList[j].ID}");
                    return;
                }
            }
        }

        List<int> freeBlocks = new List<int>();
        List<int> partiallyFreeBlocks = new List<int>();
        for (int i = 0; i < insertedBlocksRecord.Count; i++)
        {
            if (insertedBlocksRecord[i].Count == 0)
            {
                freeBlocks.Add(i);
            }
            else if (insertedBlocksRecord[i].Count < heapFile.BlockFactor)
            {
                partiallyFreeBlocks.Add(i);
            }
        }
        heapFile.FreeBlocks.Sort();
        heapFile.PartiallyFreeBlocks.Sort();

        if (heapFile.FreeBlocks.Count != freeBlocks.Count)  // kontrola voľných blokov
        {
            Console.WriteLine($"Delete test failed - free block counts are not equal - record {freeBlocks.Count}, actual in file {heapFile.FreeBlocks.Count}.");
            return;
        }
        if (heapFile.PartiallyFreeBlocks.Count != partiallyFreeBlocks.Count)
        {
            Console.WriteLine($"Delete test failed - partially free block counts are not equal - record {partiallyFreeBlocks.Count}, actual in file {heapFile.PartiallyFreeBlocks.Count}.");
            return;
        }

        for (int i = 0; i < freeBlocks.Count; i++)  // kontrola samotných indexov voľných blokov
        {
            if (heapFile.FreeBlocks[i] != freeBlocks[i])
            {
                Console.WriteLine($"Delete test failed - free block at index {i} is not equal - record {freeBlocks[i]}, actual in file {heapFile.FreeBlocks[i]}.");
                return;
            }
        }
        for (int i = 0; i < partiallyFreeBlocks.Count; i++)
        {
            if (heapFile.PartiallyFreeBlocks[i] != partiallyFreeBlocks[i])
            {
                Console.WriteLine($"Delete test failed - partially free block at index {i} is not equal - record {partiallyFreeBlocks[i]}, actual in file {heapFile.PartiallyFreeBlocks[i]}.");
                return;
            }
        }

        foreach (var (blockIndex, personToDelete, deletedPerson) in successfulDeleteList)  // kontrola či sa vymazali všetky osoby, ktoré sa mali
        {
            if (deletedPerson == null)
            {
                Console.WriteLine($"Delete test failed - successful delete - block at index {blockIndex} - got null instead of valid result.");
                return;
            }
            if (!ComparePerson(personToDelete, deletedPerson))
            {
                Console.WriteLine($"Delete test failed - successful delete - block index {blockIndex} - deleted person ID {deletedPerson.ID} is different than expected ID {personToDelete!.ID}.");
                return;
            }
        }

        foreach (var (blockIndex, personToDelete, deletedPerson) in failedDeleteList) // kontrola, či všetky neúspešné delety vrátili null
        {
            if (deletedPerson != null)
            {
                Console.WriteLine($"Delete test failed - failed delete - block at index {blockIndex} - expected null but got valid result with ID {deletedPerson.ID}.");
                return;
            }
        }

        Console.WriteLine($"Delete test passed, path - {filePath}, block size - {blockSize}, data count - {dataCount}, delete count - {deleteCount}.");
    }


    public void HeapFileTest(string filePath, int blockSize, int operations)  // v tejto metóde sú použité časti z ostatných testov jednotlivých operácií
    {
        Person dataInstance = new Person("Gordon", "Freeman", 9, 4, 1995, "3");
        HeapFile<Person> heapFile = new HeapFile<Person>(filePath, blockSize, dataInstance); // bloky, ktoré majú byť v súbore a zoznamy pre voľné bloky
        List<List<Person>> blocksRecord = new List<List<Person>>();
        List<int> freeBlocks = new List<int>();
        List<int> partiallyFreeBlocks = new List<int>();

        List<(int, Person?, Person?)> successfulGetList = new List<(int, Person?, Person?)>();  // zoznam, kde každý prvok je hľadaná osoba na indexe a výsledok hľadania
        List<(int, Person?, Person?)> failedGetList = new List<(int, Person?, Person?)>();  // rovnaký zoznam pre vyhľadávania, ktoré majú zlyhať - nesprávny index alebo neexistujúci záznam

        List<(int, Person?, Person?)> successfulDeleteList = new List<(int, Person?, Person?)>();  // zoznam, kde každý prvok je mazaná osoba na indexe a vrátená inštancia mazanej osoby
        List<(int, Person?, Person?)> failedDeleteList = new List<(int, Person?, Person?)>();  // neúspešné mazania - neplatný index alebo neexistujúci záznam


        Random rand = new Random();
        // operácie
        for (int i = 0; i < operations; i++)
        {
            double operationType = rand.NextDouble();
            Person person = new Person("Name" + i, "Surname" + i, 10, 10, 2001, i.ToString());
            if (operationType < 0.5) // insert
            {
                heapFile.Insert(person);
                int blockIndex;
                if (partiallyFreeBlocks.Count > 0)  // ako v heapfile sa určí, do ktorého voľného bloku, prípadne do nového, sa vloží záznam a aktualizujú sa zoznamy
                {
                    blockIndex = partiallyFreeBlocks[0];
                    blocksRecord[blockIndex].Add(person);
                    if (blocksRecord[blockIndex].Count == heapFile.BlockFactor)
                    {
                        partiallyFreeBlocks.RemoveAt(0);
                    }
                }
                else if (freeBlocks.Count > 0)
                {
                    blockIndex = freeBlocks[0];
                    blocksRecord[blockIndex].Add(person);
                    freeBlocks.RemoveAt(0);
                    if (blocksRecord[blockIndex].Count < heapFile.BlockFactor)
                    {
                        partiallyFreeBlocks.Add(blockIndex);
                    }
                }
                else
                {
                    blockIndex = blocksRecord.Count;
                    blocksRecord.Add(new List<Person>());
                    blocksRecord[blockIndex].Add(person);
                    if (heapFile.BlockFactor > 1)
                    {
                        partiallyFreeBlocks.Add(blockIndex);
                    }
                }
            }
            else if (operationType < 0.8)  // get
            {
                double successfulGetChance = rand.NextDouble();
                Person? foundPerson;
                if (successfulGetChance < 0.5) // get má byž neúspešný, teda zadá sa zlý index alebo záznam, ktorý sa na danom mmieste nenachádza
                {
                    double incorrectIndex = rand.NextDouble();
                    if (incorrectIndex < 0.5)  // index bude platný, priradíme nejaký neplatný záznam
                    {
                        int blockIndex = rand.Next(0, blocksRecord.Count);
                        foundPerson = heapFile.Get(blockIndex, person);
                        failedGetList.Add((blockIndex, person, foundPerson));
                    }
                    else  // index nebude platný
                    {
                        int invalidBlockIndex = blocksRecord.Count + rand.Next(1, 10);
                        Person? getPerson = new Person("Name0", "Surname0", 10, 10, 2001, "0");
                        foundPerson = heapFile.Get(invalidBlockIndex, getPerson);
                        failedGetList.Add((invalidBlockIndex, getPerson, foundPerson));
                    }
                }
                else // get má byť úspešný, vyberie sa jeden "block" a z neho záznam, ktorý by v ňom mal byť
                {
                    if (blocksRecord.Count == 0)
                    {
                        foundPerson = heapFile.Get(0, person);
                        failedGetList.Add((0, person, foundPerson));
                        continue;
                    }
                    int blockIndex = rand.Next(0, blocksRecord.Count);
                    int recordIndex = rand.Next(0, blocksRecord[blockIndex].Count);
                    Person? getPerson = blocksRecord[blockIndex][recordIndex];
                    foundPerson = heapFile.Get(blockIndex, getPerson);
                    successfulGetList.Add((blockIndex, getPerson, foundPerson));
                }
            }
            else  // delete
            {
                double successfulDeleteChance = rand.NextDouble();
                if (successfulDeleteChance < 0.5) // delete má byž neúspešný, teda zadá sa zlý index alebo záznam, ktorý sa na danom mmieste nenachádza
                {
                    double incorrectIndex = rand.NextDouble();
                    if (incorrectIndex < 0.5)  // index bude platný, priradíme nejaký neplatný záznam
                    {
                        int blockIndex = rand.Next(0, blocksRecord.Count);
                        Person? deletedPerson;
                        deletedPerson = heapFile.Delete(blockIndex, person);
                        failedDeleteList.Add((blockIndex, person, deletedPerson));
                    }
                    else  // index nebude platný
                    {
                        int invalidBlockIndex = blocksRecord.Count + rand.Next(1, 10);
                        Person? deletedPerson, personToDelete = new Person("Name0", "Surname0", 10, 10, 2001, "0");
                        deletedPerson = heapFile.Delete(invalidBlockIndex, personToDelete);
                        failedDeleteList.Add((invalidBlockIndex, personToDelete, deletedPerson));
                    }
                }
                else // delete má byť úspešný, vyberie sa jeden "block" a z neho záznam, ktorý by v ňom mal byť
                {
                    Person? deletedPerson;
                    if (blocksRecord.Count == 0)
                    {
                        deletedPerson = heapFile.Delete(0, person);
                        failedDeleteList.Add((0, person, deletedPerson));
                        continue;
                    }

                    int blockIndex = rand.Next(0, blocksRecord.Count);
                    while (blocksRecord[blockIndex].Count == 0)  // ak je block prázdny, vyberieme iný
                    {
                        blockIndex = rand.Next(0, blocksRecord.Count);
                    }
                    int recordIndex = rand.Next(0, blocksRecord[blockIndex].Count);
                    Person? deletePerson = blocksRecord[blockIndex][recordIndex];
                    deletedPerson = heapFile.Delete(blockIndex, deletePerson);
                    successfulDeleteList.Add((blockIndex, deletePerson, deletedPerson));
                    blocksRecord[blockIndex][recordIndex] = blocksRecord[blockIndex].Last().CreateClass();  // ako v súbore sa vymení, inak nebude test dobre fungovať
                    blocksRecord[blockIndex].RemoveAt(blocksRecord[blockIndex].Count - 1);

                    if (blocksRecord[blockIndex].Count == 0)
                    {
                        freeBlocks.Add(blockIndex);
                        if (heapFile.BlockFactor > 1)
                        {
                            partiallyFreeBlocks.Remove(blockIndex);
                        }
                    }
                    else if (blocksRecord[blockIndex].Count == heapFile.BlockFactor - 1)
                    {
                        partiallyFreeBlocks.Add(blockIndex);
                    }

                    if (blocksRecord.Last().Count == 0)
                    {
                        freeBlocks.Sort();
                    }

                    while (blocksRecord.Last().Count == 0)
                    {
                        blocksRecord.RemoveAt(blocksRecord.Count - 1); // ako v samotnom súbore sa skráti o všetky prázdne "blocky" na konci listu
                        freeBlocks.RemoveAt(freeBlocks.Count - 1);
                        if (blocksRecord.Count == 0)
                        {
                            break;
                        }
                    }
                }
            }
        }

        // kontrola blokov
        List<Block<Person>> blocksAfterOperations = heapFile.GetFileContents(dataInstance);
        if (blocksAfterOperations.Count != blocksRecord.Count)
        {
            Console.WriteLine($"Heap file test failed - block counts are not equal after delete - record {blocksRecord.Count}, actual in file {blocksAfterOperations.Count}.");
            return;
        }
        if (blocksRecord.Count == 0)
        {
            if (heapFile.FreeBlocks.Count > 0 || heapFile.PartiallyFreeBlocks.Count > 0)
            {
                Console.WriteLine($"Heap file test failed - file is empty, but heap file lists contain {heapFile.FreeBlocks.Count} free blocks and {heapFile.PartiallyFreeBlocks.Count} partially free blocks.");
                return;
            }
        }

        // kontrola GET výsledkov
        foreach (var (blockIndex, getPerson, foundPerson) in successfulGetList)  // kontrola či sa našli všetky osoby, ktoré sa mali
        {
            if (foundPerson == null)
            {
                Console.WriteLine($"Get test failed - successful get - block at index {blockIndex} - got null instead of valid result.");
                return;
            }
            if (!ComparePerson(getPerson, foundPerson))
            {
                Console.WriteLine($"Get test failed - successful get - block index {blockIndex} - found person ID {foundPerson.ID} is different than expected ID {getPerson!.ID}.");
                return;
            }
        }

        foreach (var (blockIndex, getPerson, foundPerson) in failedGetList) // kontrola, či všetky neúspešné gety vrátili null
        {
            if (foundPerson != null)
            {
                Console.WriteLine($"Get test failed - failed get - block at index {blockIndex} - expected null but got valid result with ID {foundPerson.ID}.");
                return;
            }
        }

        // kontrola obsahu súboru po všetkých operáciách
        for (int i = 0; i < blocksRecord.Count; i++)  // kontrola obsahu súboru, čo zostalo po mazaní, samotné záznamy sa kontrolujú iba platné, ale kontroluje sa aj celkový počet načítaných
        {
            if (blocksAfterOperations[i].RecordsList.Count != heapFile.BlockFactor)
            {
                Console.WriteLine($"Block at index {i} has incorrect record count - {blocksAfterOperations[i].RecordsList.Count}, block factor is {heapFile.BlockFactor}.");
                return;
            }
            if (blocksAfterOperations[i].ValidCount != blocksRecord[i].Count)
            {
                Console.WriteLine($"Block at index {i} has incorrect valid records - {blocksAfterOperations[i].ValidCount}, inserted {blocksRecord[i].Count}.");
                return;
            }
            for (int j = 0; j < blocksRecord[i].Count; j++)
            {
                if (!ComparePerson(blocksAfterOperations[i].RecordsList[j], blocksRecord[i][j]))
                {
                    Console.WriteLine($"Record mismatch at block {i}, record {j}:");
                    Console.WriteLine($"Name: {blocksRecord[i][j].Name}, Name from file: {blocksAfterOperations[i].RecordsList[j].Name}");
                    Console.WriteLine($"Surname: {blocksRecord[i][j].Surname}, Surname from file: {blocksAfterOperations[i].RecordsList[j].Surname}");
                    Console.WriteLine($"Day of birth: {blocksRecord[i][j].DayOfBirth}, Day of birth from file: {blocksAfterOperations[i].RecordsList[j].DayOfBirth}");
                    Console.WriteLine($"Month of birth: {blocksRecord[i][j].MonthOfBirth}, Month of birth from file: {blocksAfterOperations[i].RecordsList[j].MonthOfBirth}");
                    Console.WriteLine($"Year of birth: {blocksRecord[i][j].YearOfBirth}, Year of birth from file: {blocksAfterOperations[i].RecordsList[j].YearOfBirth}");
                    Console.WriteLine($"ID: {blocksRecord[i][j].ID}, ID from file: {blocksAfterOperations[i].RecordsList[j].ID}");
                    return;
                }
            }
        }

        // kontrola voľných blokov
        heapFile.FreeBlocks.Sort();
        heapFile.PartiallyFreeBlocks.Sort();
        freeBlocks.Sort();
        partiallyFreeBlocks.Sort();
        if (heapFile.FreeBlocks.Count != freeBlocks.Count)  // kontrola voľných blokov
        {
            Console.WriteLine($"Heap file test failed - free block counts are not equal - record {freeBlocks.Count}, actual in file {heapFile.FreeBlocks.Count}.");
            return;
        }
        if (heapFile.PartiallyFreeBlocks.Count != partiallyFreeBlocks.Count)
        {
            Console.WriteLine($"Heap file test failed - partially free block counts are not equal - record {partiallyFreeBlocks.Count}, actual in file {heapFile.PartiallyFreeBlocks.Count}.");
            return;
        }

        for (int i = 0; i < freeBlocks.Count; i++)  // kontrola samotných indexov voľných blokov
        {
            if (heapFile.FreeBlocks[i] != freeBlocks[i])
            {
                Console.WriteLine($"Heap file test failed - free block at index {i} is not equal - record {freeBlocks[i]}, actual in file {heapFile.FreeBlocks[i]}.");
                return;
            }
        }
        for (int i = 0; i < partiallyFreeBlocks.Count; i++)
        {
            if (heapFile.PartiallyFreeBlocks[i] != partiallyFreeBlocks[i])
            {
                Console.WriteLine($"Heap file test failed - partially free block at index {i} is not equal - record {partiallyFreeBlocks[i]}, actual in file {heapFile.PartiallyFreeBlocks[i]}.");
                return;
            }
        }

        // kontrola DELETE výsledkov
        foreach (var (blockIndex, personToDelete, deletedPerson) in successfulDeleteList)  // kontrola či sa vymazali všetky osoby, ktoré sa mali
        {
            if (deletedPerson == null)
            {
                Console.WriteLine($"Heap file test failed - successful delete - block at index {blockIndex} - got null instead of valid result.");
                return;
            }
            if (!ComparePerson(personToDelete, deletedPerson))
            {
                Console.WriteLine($"Heap file test failed - successful delete - block index {blockIndex} - deleted person ID {deletedPerson.ID} is different than expected ID {personToDelete!.ID}.");
                return;
            }
        }

        foreach (var (blockIndex, personToDelete, deletedPerson) in failedDeleteList) // kontrola, či všetky neúspešné delety vrátili null
        {
            if (deletedPerson != null)
            {
                Console.WriteLine($"Heap file test failed - failed delete - block at index {blockIndex} - expected null but got valid result with ID {deletedPerson.ID}.");
                return;
            }
        }

        Console.WriteLine($"Heap file test passed, path - {filePath}, block size - {blockSize}, operations - {operations}.");

    }
}