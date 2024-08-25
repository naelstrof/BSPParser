using System.Runtime.InteropServices;
namespace BSPParser;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
public struct BSPLump {
    public int nOffset;
    public int nLength;
}