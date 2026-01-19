using RollingCounterCheck.Models;
using System.IO;

namespace RollingCounterCheck.Services;

public static class CsvLogger
{
    private static readonly string FilePath = "canlog.csv";

    public static void Log(CanMessageRow row, ulong timestampUs)
    {
        File.AppendAllText(FilePath,
            $"{timestampUs};{row.CanId:X};{row.DLC};{row.Payload};{row.CycleTimeMs:F2}\n");
    }
}
