using SEM_2_CORE.Data_classes;
using SEM_2_CORE.Files;
using SEM_2_CORE.Interfaces;
using System;

namespace SEM_2_CORE.App;

public class PCRTestDatabase
{
    public LinearHashFile<Person> peopleFile { get; private set; }
    public LinearHashFile<PCRTest> pcrTestFile { get; private set; }

    private Person personDataInstance = new Person("Gordon", "Freeman", 9, 4, 1995, "3");
    private PCRTest testDataInstance = new PCRTest(1, 1, 2025, 1, 1, "0", 0, false, 0.0, "empty");
    public PCRTestDatabase(int peopleMinMod, string peoplePPath, string peopleOPath, int peoplePBlcSize, int peopleOBlcSize, 
                           int pcrMinMod, string pcrPPath, string pcrOPath, int pcrPBlcSize, int pcrOBlcSize)
    {
        peopleFile = new LinearHashFile<Person>(peopleMinMod, peoplePPath, peopleOPath, peoplePBlcSize, peopleOBlcSize, personDataInstance);
        pcrTestFile = new LinearHashFile<PCRTest>(pcrMinMod, pcrPPath, pcrOPath, pcrPBlcSize, pcrOBlcSize, testDataInstance);
    }

    private List<string> GetPatientsTests(Person person)
    {
        List<string> testsStrings = new List<string>();
        for (int i = 0; i < person.tests.Length; i++)
        {
            if (person.tests[i] == 0)
            {
                break;
            }
            testDataInstance.ID = person.tests[i];
            PCRTest? test = pcrTestFile.Get(testDataInstance);
            if (test != null)
            {
                testsStrings.Add(test.ToString());
            }
        }

        return testsStrings;
    }

    // na výpis obsahu konkrétneho heap súboru - vráti zoznam popisu blokov rôzneho typu - primary, overflow
    public List<BlockViewData> GetBlockViewData<Block, T>(HeapFile<T> heapFile, T dataInstance) where Block : Block<T>, new() where T : IDataClassOperations<T>, IByteOperations
    {
        List<Block> blocks = heapFile.GetFileContents<Block>(dataInstance);
        List<BlockViewData> blocksData = new List<BlockViewData>();

        for (int i = 0; i < blocks.Count; i++)
        {
            blocksData.Add(new BlockViewData(i, blocks[i].RecordsCount, blocks[i].ValidCount, blocks[i].ToString()));
        }

        return blocksData;
    }

    // # 4 - Vloženie osoby
    public string InsertPerson(string name, string surname, (byte day, byte month, ushort year) dateOB, string id)
    {
        Person patient = new Person(name, surname, dateOB.day, dateOB.month, dateOB.year, id);
        peopleFile.Insert(patient);
        return $"Person with ID: {id} was added.";
    }

    // # 1 - Vloženie výsledku PCR testu
    public string InsertPCRTest((byte day, byte month, ushort year, byte minute, byte hour) dateTime, string personID, uint testID, bool result, double testValue, string note)
    {
        PCRTest test = new PCRTest(dateTime.day, dateTime.month, dateTime.year, dateTime.minute, dateTime.hour, personID, testID, result, testValue, note);
        pcrTestFile.Insert(test);
        return $"PCR test with ID: {testID} was added.";
    }
    
    // # 2 - Vyhľadanie osoby + jej testy
    public Person? GetPerson(string id, out List<string> testsStrings)
    {
        personDataInstance.ID = id;
        Person? person = peopleFile.Get(personDataInstance);
        testsStrings = new List<string>();

        if (person != null)
        {
            testsStrings = GetPatientsTests(person.CreateClass());
        }

        return person;
    }

    // # 3 - Vyhľadanie PCR testu + osoba
    public PCRTest? GetPCRTest(uint id, out Person? patient)
    {
        testDataInstance.ID = id;
        PCRTest? test = pcrTestFile.Get(testDataInstance);
        patient = null;

        if (test != null)
        {
            test = test.CreateClass();
            personDataInstance.ID = test.PersonID;
            patient = peopleFile.Get(personDataInstance);
        }

        return test;
    }

    // # 6 - Vymazanie osoby + jej testov
    public string DeletePerson(string id)
    {
        personDataInstance.ID = id;
        string result = (peopleFile.Delete(personDataInstance) == -1) ? $"Person to delete with ID: {id} was not found!" : $"Person with ID: {id} was deleted.";
        return result;
    }

    // # 5 - Vymazanie výsledku PCR testu
    public string DeletePCRTest(uint id)
    {
        testDataInstance.ID = id;
        string result = (pcrTestFile.Delete(testDataInstance) == -1) ? $"PCR test to delete with ID: {id} was not found!" : $"PCR test with ID: {id} was deleted.";
        return result;
    }

    // # 7 - Editácia údajov osoby
    public string EditPerson(string name, string surname, (byte day, byte month, ushort year) dateOB, string id)
    {
        Person patient = new Person(name, surname, dateOB.day, dateOB.month, dateOB.year, id);
        string result = (peopleFile.Update(personDataInstance) == -1) ? $"Person with ID: {id} edit failed!" : $"Person with ID: {id} was edited.";
        return result;
    }

    // # 8 - Editácia údajov PCR testu
    public string EditPCRTest((byte day, byte month, ushort year, byte minute, byte hour) dateTime, string personID, uint testID, bool testResult, double testValue, string note)
    {
        PCRTest test = new PCRTest(dateTime.day, dateTime.month, dateTime.year, dateTime.minute, dateTime.hour, personID, testID, testResult, testValue, note);
        string result = (pcrTestFile.Update(testDataInstance) == -1) ? $"PCR test with ID: {testID} edit failed!" : $"PCR test with ID: {testID} was edited.";
        return result;
    }
}