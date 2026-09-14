using ConsoleApp1.Helpers;
using OpenCvSharp;

var exeDir = AppContext.BaseDirectory;
// 示例：程序同目录下的config文件夹，动态获取完整地址
// var imageDir = Path.Combine(AppContext.BaseDirectory, "images");
// var catImage = Path.Combine(imageDir, "cat.jpg");
//
// var taskA = FileHelper.ReadFileMagicNumberAsync(catImage);
// taskA.Wait();
// var hexWithSpace = BitConverter.ToString(taskA.Result);
// Console.WriteLine(hexWithSpace);
var currentThreadId = Thread.CurrentThread.ManagedThreadId;
Console.WriteLine("In Main ThreadId:" + currentThreadId);
FileHelper.TestWriteAsyncInThisWay();
Console.ReadKey();

// JPG：FF D8 FF 
// PNG：89 50 4E 47
// BMP：42 4D
// GIF：47 49 46
// PGM 灰度图 二进制格式（P5）是最优选择：它的魔数是0x50 0x35
// WebP：52 49 46 46（前4字节）

// using var catMat = new Mat(catImage);
// var bytes = catMat.ToBytes();
// var bytesImagePath = Path.Combine(imageDir, "cat2.png");
// File.WriteAllBytes(bytesImagePath, bytes);