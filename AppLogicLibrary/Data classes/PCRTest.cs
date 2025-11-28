using SEM_2_CORE.Interfaces;
using SEM_2_CORE.Tools;

namespace SEM_2_CORE.Data_classes;

public class PCRTest : IDataClassOperations<PCRTest>, IByteOperations
{
    private const byte PersonIDLength = 10;
    private const byte NoteLength = 11;
    public byte DayOfTest { get; set; }
    public byte MonthOfTest { get; set; }
    public ushort YearOfTest { get; set; }
    public byte MinuteOfTest { get; set; }
    public byte HourOfTest { get; set; }
    public string PersonID { get; set; }
    public uint ID { get; set; }
    public bool Result { get; set; }
    public double TestValue { get; set; }
    public string Note { get; set; }

    public PCRTest(byte dayOfTest, byte monthOfTest, ushort yearOfTest,
                   byte minuteOfTest, byte hourOfTest, string personID, uint testID, bool result, double testValue, string note)
    {
        DayOfTest = dayOfTest;
        MonthOfTest = monthOfTest;
        YearOfTest = yearOfTest;
        MinuteOfTest = minuteOfTest;
        HourOfTest = hourOfTest;
        PersonID = personID;
        ID = testID;
        Result = result;
        TestValue = testValue;
        Note = note;
    }

    public override string ToString()
    {
        string _string = string.Empty;
        _string += $"PCR Test date: {DayOfTest}.{MonthOfTest}.{YearOfTest}\n";
        _string += $"PCR Test time: {HourOfTest}:{MinuteOfTest}\n";
        _string += $"Person ID: {PersonID}\n";
        _string += $"Test ID: {ID}\n";
        _string += $"Test result: {(Result ? "Positive" : "Negative")}\n";
        _string += $"Test value: {TestValue}\n";
        _string += $"Note: {Note}\n";

        return _string;
    }

    public override int GetHashCode()
    {
        return CustomHash.GetHashCode(ID);
    }

    public bool Equals(PCRTest obj)
    {
        return ID == obj.ID;
    }

    public PCRTest CreateClass()
    {
        return new PCRTest(DayOfTest, MonthOfTest, YearOfTest, MinuteOfTest, HourOfTest, PersonID, ID, Result, TestValue, Note);
    }

    public int GetSize()  // dokopy vychádza na 63 bytov
    {
        int size = 6; // byty na dátum a čas majú po 1 byte okrem roku, ten je ushort - 2 byty, dokopy 6
        size += 13; // 4 byty pre ID, 1 byte pe výsledok, 8 bytov pre double hodnotu testu
        size += 1 + (sizeof(ushort) * PersonIDLength); // každý char sa násobí svojou veľkosťou, tu máme 10 znakov + 1 byte pre počet platných
        size += 1 + (sizeof(char) * NoteLength); // ako vyššie, toto je zase pre poznámku, tu bude 11 znakov
        return size;
    }

    public byte[] GetBytes()
    {
        List<byte> byteBuffer = new List<byte>();
        PersonID = StringConverter.PadString(PersonID, PersonIDLength);
        Note = StringConverter.PadString(Note, NoteLength);

        byteBuffer.AddRange([DayOfTest, MonthOfTest]);
        byteBuffer.AddRange(BitConverter.GetBytes(YearOfTest));  // dátum testu
        byteBuffer.AddRange([MinuteOfTest, HourOfTest]);  // čas testu

        byteBuffer.Add(StringConverter.GetValidChars(PersonID));
        byteBuffer.AddRange(StringConverter.StringToBytes(PersonID));  // id pacienta a valid count jeho znakov

        byteBuffer.AddRange(BitConverter.GetBytes(ID));
        byteBuffer.AddRange(BitConverter.GetBytes(Result));
        byteBuffer.AddRange(BitConverter.GetBytes(TestValue));  // id testu, jeho výsledok a hodnota

        byteBuffer.Add(StringConverter.GetValidChars(Note));
        byteBuffer.AddRange(StringConverter.StringToBytes(Note)); // poznámka a valid count jej znakov

        byte[] bytes = byteBuffer.ToArray();

        return bytes;
    }

    public void FromBytes(byte[] bytes)
    {
        byte validChars;
        int position;
        byte[] charBytes;
        DayOfTest = bytes[0];
        MonthOfTest = bytes[1];
        YearOfTest = BitConverter.ToUInt16(bytes.AsSpan(2, 2));
        MinuteOfTest = bytes[4];
        HourOfTest = bytes[5];
        position = 6;   // načítanie dátumu a času a nastavenie indexu/pozície

        validChars = bytes[position]; // počet platných znakov stringu pre ID pacienta
        position++;

        charBytes = bytes.AsSpan(position, PersonIDLength * 2).ToArray();  // slice arrayu od indexu kde začína ID pacienta
        PersonID = StringConverter.StringFromBytes(charBytes, validChars);
        position += (PersonIDLength * 2);   // načítanie PersonID a posun na ďalšiu pozíciu

        ID = BitConverter.ToUInt32(bytes.AsSpan(position, 4));
        Result = BitConverter.ToBoolean(bytes.AsSpan(position + 4, 1));
        position += 5;
        TestValue = BitConverter.ToDouble(bytes.AsSpan(position, 8));  // načítanie ID, výsledku a hodnoty testu
        position += 8;

        validChars = bytes[position];
        position++;
        charBytes = bytes.AsSpan(position, NoteLength * 2).ToArray();
        Note = StringConverter.StringFromBytes(charBytes, validChars);  // nakoniec načítanie poznámky
    }
}
