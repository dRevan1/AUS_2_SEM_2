using SEM_2_CORE.Interfaces;
using System.Xml.Linq;

namespace SEM_2_CORE;

public class Person : IDataClassOperations<Person>, IByteOperations
{
    public const byte NameLength = 15;
    public const byte SurnameLength = 14;
    public const byte IDLength = 10;
    public string Name { get; set; }
    public string Surname { get; set; }
    public byte DayOfBirth { get; set; }
    public byte MonthOfBirth { get; set; }
    public ushort YearOfBirth { get; set; }

    public string ID { get; set; }

    public Person(string name, string surname, byte dayOfBirth, byte monthOfBirth, ushort yearOfBirth, string id)
    {
        Name = name;
        Surname = surname;
        DayOfBirth = dayOfBirth;
        MonthOfBirth = monthOfBirth;
        YearOfBirth = yearOfBirth;
        ID = id;
    }

    public override string ToString()
    {
        return $"Name: {Name}\nSurname: {Surname}\nDate of Birth: {DayOfBirth}.{MonthOfBirth}.{YearOfBirth}\nID: {ID}\n";
    }

    public bool Equals(Person other)
    {
        return other.ID == ID;
    }

    public int GetSize()  // teraz je to 85 bytov
    {
        int size = 1 + (sizeof(char) * NameLength); // meno (name) má max 15 znakov + 1 byte pre počet platných typu byte, to nám stačí na max 15 hodnotu
        size += 1 + (sizeof(char) * SurnameLength); // priezvisko (surname) má max 14 znakov + 1 byte -||-
        size += 2 + sizeof(ushort); // pre dátum je 1 byte pre deň a mesiac + ushort veľkosť pre rok
        size += 1 + (sizeof(char) * IDLength); // ID je max 10 znakov + 1 byte
        return size;
    }

    public Person CreateClass()
    {
        return new Person(Name, Surname, DayOfBirth, MonthOfBirth, YearOfBirth, ID);
    }

    public byte[] GetBytes()
    {
        List<byte> byteBuffer = new List<byte>();
        Name = StringConverter.PadString(Name, NameLength);
        Surname = StringConverter.PadString(Surname, SurnameLength);
        ID = StringConverter.PadString(ID, IDLength);

        byteBuffer.Add(StringConverter.GetValidChars(Name));
        byteBuffer.AddRange(StringConverter.StringToBytes(Name));  // meno + valid count

        byteBuffer.Add(StringConverter.GetValidChars(Surname));
        byteBuffer.AddRange(StringConverter.StringToBytes(Surname)); // priezvisko + valid count

        byteBuffer.AddRange([DayOfBirth, MonthOfBirth]);
        byteBuffer.AddRange(BitConverter.GetBytes(YearOfBirth)); // dátum narodenia

        byteBuffer.Add(StringConverter.GetValidChars(ID));
        byteBuffer.AddRange(StringConverter.StringToBytes(ID));  // meno + valid count

        byte[] bytes = byteBuffer.ToArray();

        return bytes;
    }

    public void FromBytes(byte[] bytes)   // zatiaľ char 2 byte akože utf 16 c# char, asi zmením na 1 byte ASCII
    {
        byte validChars = bytes[0];
        int position = 1;
        byte[] charBytes = bytes.AsSpan(position, NameLength * 2).ToArray();  // slice arrayu od indexu 1 s dĺžkou stringu pre name
        Name = StringConverter.StringFromBytes(charBytes, validChars);
        position += (15 * 2);

        validChars = bytes[position];  // o jeden ďalej je validná dĺžka pre surname
        position++;

        charBytes = bytes.AsSpan(position, SurnameLength * 2).ToArray();  // slice arrayu od indexu kde začína surname
        Surname = StringConverter.StringFromBytes(charBytes, validChars);
        position += (14 * 2);

        DayOfBirth = bytes[position];
        MonthOfBirth = bytes[position + 1];
        position += 2;

        YearOfBirth = BitConverter.ToUInt16(bytes.AsSpan(position, 2).ToArray()); // vybratie roku a posun
        position += 2;

        validChars = bytes[position];
        position++;
        charBytes = bytes.AsSpan(position, IDLength * 2).ToArray();
        ID = StringConverter.StringFromBytes(charBytes, validChars); // doplníme ID nakoniec
    }
}