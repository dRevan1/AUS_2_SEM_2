namespace SEM_2_CORE;

public interface IDataClassInterface<T>
{
    bool Equals(T obj);
    int GetSize();
    T CreateClass(T self);
    byte[] GetBytes();
    void FromBytes(byte[] bytes);
}
