using SEM_2_CORE.Files;
using SEM_2_CORE.Interfaces;
using SEM_2_CORE.Testers;

namespace SEM_2_CORE.App;

public class PCRTestDatabase
{
    public LinearHashFile<Person> PeopleFile { get; private set; }
    public LinearHashFile<PCRTest> PcrTestFile { get; private set; }
    private string Name { get; set; }

    private Person personDataInstance = new Person("Gordon", "Freeman", 9, 4, 1995, "3");
    private PCRTest testDataInstance = new PCRTest(1, 1, 2025, 1, 1, "0", 0, false, 0.0, "empty");
    public PCRTestDatabase(int peopleMinMod, string peoplePPath, string peopleOPath, int peoplePBlcSize, int peopleOBlcSize, 
                           int pcrMinMod, string pcrPPath, string pcrOPath, int pcrPBlcSize, int pcrOBlcSize, string name)
    {
        PeopleFile = new LinearHashFile<Person>(peopleMinMod, peoplePPath, peopleOPath, peoplePBlcSize, peopleOBlcSize, personDataInstance);
        PcrTestFile = new LinearHashFile<PCRTest>(pcrMinMod, pcrPPath, pcrOPath, pcrPBlcSize, pcrOBlcSize, testDataInstance);
        Name = name;
    }

    public PCRTestDatabase(string pplControlFilePath, string tControlFilePath, string name)
    {
        PeopleFile = new LinearHashFile<Person>(pplControlFilePath);
        PcrTestFile = new LinearHashFile<PCRTest>(tControlFilePath);
        Name = name;
    }

    public void SaveControlData()
    {
        string filePath = Name + "_ppl_control.csv";
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            PeopleFile.SaveControlData(writer);
            writer.Close();
        }

        filePath = Name + "_test_control.csv";
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            PcrTestFile.SaveControlData(writer);
            writer.Close();
        }
    }

    private List<PCRTest> GetPatientsTests(Person person)
    {
        List<PCRTest> tests = new List<PCRTest>();
        for (int i = 0; i < person.Tests.Length; i++)
        {
            if (person.Tests[i] == 0)
            {
                break;
            }
            testDataInstance.ID = person.Tests[i];
            PCRTest? test = PcrTestFile.Get(testDataInstance);
            if (test != null)
            {
                tests.Add(test.CreateClass());
            }
        }

        return tests;
    }

    // na naplnenie súboru ľudí
    private List<Person> GeneratePeople(int peopleCount, DateTime start = default, DateTime end = default)
    {
        LinearHashFileTester tester = new LinearHashFileTester();
        List<Person> list = new List<Person>(); 
        for (int i = 1; i <= peopleCount; i++)
        {
            Person person = tester.GeneratePerson(i, start, end);
            PeopleFile.Insert(person);
            list.Add(person);
        }

        return list;
    }

    // na naplnenie súboru PCR testov
    private void GeneratePCRTests(List<Person> people, uint testsCount, DateTime start = default, DateTime end = default)
    {
        LinearHashFileTester tester = new LinearHashFileTester();

        for (uint i = 1; i <= testsCount; i++)
        {
            int personIndex = (int)((i - 1) / 2); // vydeliť počtom testov pre 1 osobu, teraz každá dostane 2 a počet testov musí byť 2*počet osôb
            int personID = int.Parse(people[personIndex].ID);
            PCRTest test = tester.GeneratePCRTest(i, personID, start, end);
            (byte, byte, ushort, byte, byte) dateTime = (test.DayOfTest, test.MonthOfTest, test.YearOfTest, test.MinuteOfTest, test.HourOfTest);
            InsertPCRTest(dateTime, test.PersonID, test.ID, test.Result, test.TestValue, test.Note);
        }
    }

    // generátor dát pre osoby a testy
    public void PopulateDatabase(int peopleCount, uint testsCount, DateTime startPpl = default, DateTime endPpl = default, DateTime startTs = default, DateTime endTs = default)
    {
        List<Person> people = GeneratePeople(peopleCount, startPpl, endPpl);
        GeneratePCRTests(people, testsCount, startTs, endTs);
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
        PeopleFile.Insert(patient);
        return $"Person with ID: {id} was added.";
    }

    // # 1 - Vloženie výsledku PCR testu
    public string InsertPCRTest((byte day, byte month, ushort year, byte minute, byte hour) dateTime, string personID, uint testID, bool result, double testValue, string note)
    {
        PCRTest test = new PCRTest(dateTime.day, dateTime.month, dateTime.year, dateTime.minute, dateTime.hour, personID, testID, result, testValue, note);
        personDataInstance.ID = personID;
        Person? person = PeopleFile.Get(personDataInstance);
        if (person == null )
        {
            return $"PCR test with ID: {testID} couldn't be added, person with {personID} is not in the database!";
        }

        PcrTestFile.Insert(test);
        person.AddTest(testID);
        PeopleFile.Update(person);
        return $"PCR test with ID: {testID} was added.";
    }
    
    // # 2 - Vyhľadanie osoby + jej testy
    public Person? GetPerson(string id, out List<PCRTest> tests)
    {
        personDataInstance.ID = id;
        Person? person = PeopleFile.Get(personDataInstance);
        tests = new List<PCRTest>();

        if (person != null)
        {
            tests = GetPatientsTests(person.CreateClass());
            return person.CreateClass();
        }

        return person;  // tu bude null
    }

    // # 3 - Vyhľadanie PCR testu + osoba
    public PCRTest? GetPCRTest(uint id, out Person? patient)
    {
        testDataInstance.ID = id;
        PCRTest? test = PcrTestFile.Get(testDataInstance);
        patient = null;

        if (test != null)
        {
            test = test.CreateClass();
            personDataInstance.ID = test.PersonID;
            patient = PeopleFile.Get(personDataInstance);
            if (patient != null)
            {
                patient = patient.CreateClass();
            }
            return test.CreateClass();
        }

        return test;
    }

    // # 7 - Editácia údajov osoby
    public string EditPerson(string name, string surname, (byte day, byte month, ushort year) dateOB, string id, uint[] tests)
    {
        Person patient = new Person(name, surname, dateOB.day, dateOB.month, dateOB.year, id, tests);
        string result = (PeopleFile.Update(patient) == -1) ? $"Person with ID: {id} edit failed!" : $"Person with ID: {id} was edited.";
        return result;
    }

    // # 8 - Editácia údajov PCR testu
    public string EditPCRTest((byte day, byte month, ushort year, byte minute, byte hour) dateTime, string personID, uint testID, bool testResult, double testValue, string note)
    {
        PCRTest test = new PCRTest(dateTime.day, dateTime.month, dateTime.year, dateTime.minute, dateTime.hour, personID, testID, testResult, testValue, note);
        string result = (PcrTestFile.Update(test) == -1) ? $"PCR test with ID: {testID} edit failed!" : $"PCR test with ID: {testID} was edited.";
        return result;
    }
}