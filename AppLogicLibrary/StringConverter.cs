namespace SEM_2_CORE;

public static class StringConverter
{
    public static string PadString(string _string, int stringLength)
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

    public static byte GetValidChars(string _string)
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

    public static byte[] StringToBytes(string _string)
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

    public static string StringFromBytes(byte[] bytes, byte validChars)
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