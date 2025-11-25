namespace SEM_2_CORE.Interfaces;

public interface IByteOperations
{
    int GetSize();
    byte[] GetBytes();
    void FromBytes(byte[] bytes);
}