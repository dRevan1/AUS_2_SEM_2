namespace SEM_2_CORE;

public interface IDataClassOperations<T>
{
    bool Equals(T obj);
    T CreateClass();
}
