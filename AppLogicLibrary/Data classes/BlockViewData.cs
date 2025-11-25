namespace SEM_2_CORE;

public class BlockViewData
{
    public int BlockNumber { get; set; }
    public int RecordsCount { get; set; }
    public int ValidCount { get; set; }
    public string BlockData { get; set; }
    public BlockViewData(int blockNumber, int recordsCount, int validCount, string blockData)
    {
        BlockNumber = blockNumber;
        RecordsCount = recordsCount;
        ValidCount = validCount;
        BlockData = blockData;
    }
}
