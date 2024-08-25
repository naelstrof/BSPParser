using System.Runtime.InteropServices;
using System.Text;

namespace BSPParser;

    
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
public struct BSPMipTexture {
    
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = BSP.MAXTEXTURENAME)]
    public char[] szName;

    private uint width;
    private uint height;
    
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = BSP.MIPLEVELS)]
    private uint[] nOffsets;

    public override string ToString() {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(szName);
        stringBuilder.Append($": {width}x{height}\n");
        for (int i = 0; i < BSP.MIPLEVELS; i++) {
            stringBuilder.Append($"\t{nOffsets[i]}\n");
        }

        return stringBuilder.ToString();
    }
}