namespace SEM_2_CORE.Tools;

using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;


public static class CustomHash
{
    public static int GetHashCode(string value)
    {
        using var sha = SHA256.Create();
        byte[] byteArray = Encoding.UTF8.GetBytes(value);
        byte[] hashBytes = sha.ComputeHash(byteArray);

        return BitConverter.ToInt32(hashBytes, 0);
    }

    public static int GetHashCode(uint value)
    {
        using var sha = SHA256.Create();
        byte[] byteArray = BitConverter.GetBytes(value);
        byte[] hashBytes = sha.ComputeHash(byteArray);

        return BitConverter.ToInt32(hashBytes, 0);
    }
}
