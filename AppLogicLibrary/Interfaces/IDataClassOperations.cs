namespace SEM_2_CORE.Interfaces;

public interface IDataClassOperations<T>
{
    bool Equals(T obj);
    T CreateClass();
}
