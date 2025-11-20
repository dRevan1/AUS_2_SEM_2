namespace SEM_2_CORE;

public interface IByteOperations
{
    int GetSize();
    byte[] GetBytes();
    void FromBytes(byte[] bytes);
}