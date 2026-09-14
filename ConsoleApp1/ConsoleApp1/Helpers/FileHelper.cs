namespace ConsoleApp1.Helpers;

public static class FileHelper
{
    public static async Task<byte[]> ReadFileMagicNumberAsync(string filePath, int readLength = 8)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("指定的文件不存在", filePath);

        readLength = Math.Min(readLength, 8);
        await using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 4096, useAsync: true);
        var magicBytes = new byte[readLength];
        var bytesRead = await fs.ReadAsync(magicBytes, 0, readLength);
        if (bytesRead < readLength)
            Array.Resize(ref magicBytes, bytesRead);

        return magicBytes;
    }

    public static async void TestWriteAsyncInThisWay()
    {
        var currentThreadId = Thread.CurrentThread.ManagedThreadId;
        Console.WriteLine("In TestWriteAsyncInThisWay , ThreadId:" + currentThreadId);
        await Task.Delay(100);
        currentThreadId = Thread.CurrentThread.ManagedThreadId;
        Console.WriteLine("In TestWriteAsyncInThisWay after delay , ThreadId:" + currentThreadId);
    }
}