using System.Runtime.InteropServices;

namespace ConsoleApp1;

public class MyCustomizeResourceProxy(IntPtr invalidHandleValue, bool ownsHandle)
    : SafeHandle(invalidHandleValue, ownsHandle)
{
    protected override bool ReleaseHandle()
    {
        throw new NotImplementedException();
    }

    public override bool IsInvalid { get; }
}