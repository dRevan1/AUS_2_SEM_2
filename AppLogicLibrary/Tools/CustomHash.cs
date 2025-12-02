namespace SEM_2_CORE.Tools;

using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;


public static class CustomHash
{
    public static int GetHashCode(string value)
    {
        using var sha = SHA256.Create();
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        byte[] hash = sha.ComputeHash(bytes);
        int result = BitConverter.ToInt32(hash, 0) & 0x7FFFFFFF;
        return result;
    }

    public static int GetHashCode(uint value)
    {
        using var sha = SHA256.Create();
        byte[] bytes = BitConverter.GetBytes(value);
        byte[] hash = sha.ComputeHash(bytes);
        int result = BitConverter.ToInt32(hash, 0) & 0x7FFFFFFF;
        return result;
    }
}
