using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace SEM_2_CORE;

public class HeapFileTester
{
    private List<List<Person>> InsertData(HeapFile<Person> heapFile, int dataCount)  // naplní file a vráti list "blockov" (list listov)
    {
        Person dataInstance = new Person("Gordon", "Freeman", 9, 4, 1995, "3");
        List<List<Person>> insertedBlocksRecord = new List<List<Person>>();
        int blockFactor = heapFile.BlockFactor;
        for (int i = 0; i < dataCount; i++)
        {
            Person person = new Person("Name" + i, "Surname" + i, 10, 10, 2001, i.ToString()); // doplniť random dátum in range
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
                    deletedPerson = heapFile.Get(invalidBlockIndex, personToDelete);
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
                Person? getPerson = insertedBlocksRecord[blockIndex][recordIndex];
                deletedPerson = heapFile.Delete(blockIndex, getPerson);
                successfulDeleteList.Add((blockIndex, getPerson, deletedPerson));
                insertedBlocksRecord[blockIndex].RemoveAt(recordIndex);

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
}