namespace SEM_2_CORE;

public class Block<T>
{
    public List<T> DataList { get; set; }
    public int DataCount { get; set; }
    public int ValidCount { get; set; }

    public Block(int blockFactor)
    {
        DataCount = blockFactor;
        ValidCount = 0;
        DataList = new List<T>();
    }
}
