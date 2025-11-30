using SEM_2_CORE.Files;

namespace SEM_2_CORE.Testers;

public class LinearHashFileTester
{
    private DateTime RandomDate(DateTime start, DateTime end)
    {
        Random rand = new Random();
        int range = (end - start).Days;
        return start.AddDays(rand.Next(range));
    }

    public void InsertTest(LinearHashFile<Person> linHashFile, uint insertCount)
    {
        Person dataInstance = new Person("Gordon", "Freeman", 9, 4, 1995, "3");
        for (int i = 0; i < insertCount; i++)
        {
            DateTime randomDate = RandomDate(new DateTime(1960, 1, 1), new DateTime(2023, 12, 31));
            byte day = (byte)randomDate.Day;
            byte month = (byte)randomDate.Month;
            ushort year = (ushort)randomDate.Year;
            Person person = new Person("Name" + i, "Surname" + i, day, month, year, i.ToString());

            linHashFile.Insert(person);
        }
    }
}
