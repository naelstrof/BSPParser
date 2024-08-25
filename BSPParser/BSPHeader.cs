using System.Runtime.InteropServices;

namespace BSPParser;


[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
public struct BSPHeader {
    
    public int nVersion;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = BSP.HEADER_LUMPS)]
    public BSPLump[] lump;
}