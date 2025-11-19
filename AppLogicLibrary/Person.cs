namespace SEM_2_CORE;

public class Person : IDataClassInterface<Person>
{
    public string Name { get; set; }
    public string Surname { get; set; }
    public byte DayOfBirth { get; set; }
    public byte MonthOfBirth { get; set; }
    public ushort YearOfBirth { get; set; }

    public string ID { get; set; }

    public Person(string name, string surname, byte dayOfBirth, byte monthOfBirth, ushort yearOfBirth, string id)
    {
        Name = PadString(name, 15);
        Surname = PadString(surname, 14);
        DayOfBirth = dayOfBirth;
        MonthOfBirth = monthOfBirth;
        YearOfBirth = yearOfBirth;
        ID = PadString(id, 10);
    }


    private string PadString(string _string, int stringLength)
    {
        if (_string.Length < stringLength)
        {
            int difference = stringLength - _string.Length;
            string pad = string.Empty;
            for (int i = 0; i < difference; i++)
            {
                pad += '*';
            }
            _string += pad;
        }

        return _string;
    }

    private byte GetValidChars(string _string)
    {
        byte validChars = 0;
        for (byte i = 0; i < _string.Length; i++)
        {
            if (_string[i] == '*')
            {
                validChars = i;
                break;
            }
        }

        return validChars;
    }

    private byte[] StringToBytes(string _string)
    {
        List<byte> byteBuffer = new List<byte>();

        for (int i = 0; i < _string.Length; i++)
        {
            byte[] bytes = BitConverter.GetBytes(_string[i]);
            for (int j = 0; j < bytes.Length; j++)
            {
                byteBuffer.Add(bytes[j]);
            }
        }

        return byteBuffer.ToArray();
    }

    public bool Equals(Person other)
    {
        return other.ID == ID;
    }

    public int GetSize()
    {
        int size = 1 + (sizeof(char) * 15); // meno (name) má max 15 znakov + 1 byte pre počet platných typu byte, to nám stačí na max 15 hodnotu
        size += 1 + (sizeof(char) * 14); // priezvisko (surname) má max 14 znakov + 1 byte -||-
        size += 2 + sizeof(ushort); // pre dátum je 1 byte pre deň a mesiac + ushort veľkosť pre rok
        size += 1 + (sizeof(char) * 10); // ID je max 10 znakov + 1 byte
        return size;
    }

    public Person CreateClass(Person self)
    {
        return new Person(Name, Surname, DayOfBirth, MonthOfBirth, YearOfBirth, ID);
    }

    public byte[] GetBytes()
    {
        List<byte> byteBuffer = new List<byte>();

        byteBuffer.Add(GetValidChars(Name));
        byteBuffer.AddRange(StringToBytes(Name));  // meno + valid count

        byteBuffer.Add(GetValidChars(Surname));
        byteBuffer.AddRange(StringToBytes(Surname)); // priezvisko + valid count

        byteBuffer.AddRange([DayOfBirth, MonthOfBirth]);
        byteBuffer.AddRange(BitConverter.GetBytes(YearOfBirth)); // dátum narodenia

        byteBuffer.Add(GetValidChars(ID));
        byteBuffer.AddRange(StringToBytes(ID));  // meno + valid count

        byte[] bytes = byteBuffer.ToArray();

        return bytes;
    }

    public void FromBytes(byte[] bytes)   // zatiaľ char 2 byte akože utf 16 c# char, asi zmením na 1 byte ASCII
    {
        byte validChars = bytes[0];
        int position = 1;
        byte[] charBytes = bytes.AsSpan(position, 15 * 2).ToArray();  // slice arrayu od indexu 1 s dĺžkou stringu pre name
        Name = StringFromBytes(charBytes, validChars);
        position += (15 * 2);

        validChars = bytes[position];  // o jeden ďalej je validná dĺžka pre surname
        position++;

        charBytes = bytes.AsSpan(position, 14 * 2).ToArray();  // slice arrayu od indexu kde zaťína surname
        Surname = StringFromBytes(charBytes, validChars);
        position += (14 * 2);

        DayOfBirth = bytes[position];
        MonthOfBirth = bytes[position + 1];
        position += 2;

        YearOfBirth = BitConverter.ToUInt16(bytes.AsSpan(position, 2).ToArray()); // vybratie roku a posun
        position += 2;

        validChars = bytes[position];
        position++;
        charBytes = bytes.AsSpan(position, 10 * 2).ToArray();
        ID = StringFromBytes(charBytes, validChars); // doplníme ID nakoniec
    }

    private string StringFromBytes(byte[] bytes, byte validChars)
    {
        string _string = string.Empty;

        for (byte i = 0; i < validChars; i++)
        {
            int charIndex = i * 2;
            byte[] charBytes = { bytes[charIndex], bytes[charIndex + 1] };
            _string += BitConverter.ToChar(charBytes);
        }
        
        return _string;
    }
}
