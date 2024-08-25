using System.Runtime.InteropServices;

namespace BSPParser;

public class BSP {
    public const int LUMP_ENTITIES = 0;
    public const int LUMP_PLANES        = 1;
    public const int LUMP_TEXTURES      = 2;
    public const int LUMP_VERTICES      = 3;
    public const int LUMP_VISIBILITY    = 4;
    public const int LUMP_NODES         = 5;
    public const int LUMP_TEXINFO       = 6;
    public const int LUMP_FACES         = 7;
    public const int LUMP_LIGHTING      = 8;
    public const int LUMP_CLIPNODES     = 9;
    public const int LUMP_LEAVES = 10;
    public const int LUMP_MARKSURFACES = 11;
    public const int LUMP_EDGES = 12;
    public const int LUMP_SURFEDGES  = 13;
    public const int LUMP_MODELS = 14;
    public const int HEADER_LUMPS = 15;
    public const int MAXTEXTURENAME = 16;
    public const int MIPLEVELS = 4;
    private List<BSPEntity> entities;
    private List<BSPMipTexture> textures;
    
    private static bool TryReadStruct<T>(Stream stream, out T output) {
        byte[] buffer = new byte[Marshal.SizeOf(typeof(T))];
        var read = stream.Read(buffer, 0, Marshal.SizeOf(typeof(T)));
        GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        T? typedStruct = (T?)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
        if (typedStruct == null) {
            handle.Free();
            output = default;
            return false;
        }
        handle.Free();
        output = typedStruct;
        return true;
    }
    private static bool TryReadStruct<T>(Stream stream, long offset, out T output) {
        byte[] buffer = new byte[Marshal.SizeOf(typeof(T))];
        stream.Seek(offset, SeekOrigin.Begin);
        var read = stream.Read(buffer, 0, Marshal.SizeOf(typeof(T)));
        GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        T? typedStruct = (T?)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
        if (typedStruct == null) {
            handle.Free();
            output = default;
            return false;
        }
        handle.Free();
        output = typedStruct;
        return true;
    }

    private static string ReadString(Stream stream, int offset, int size) {
        byte[] buffer = new byte[size];
        stream.Seek(offset, SeekOrigin.Begin);
        var read = stream.Read(buffer, 0, size);
        return System.Text.Encoding.UTF8.GetString(buffer);
    }

    private void ParseEntities(FileStream stream, BSPHeader header) {
        var entitiesLump = header.lump[LUMP_ENTITIES];
        entities = new List<BSPEntity>(new BSPEntityTokenizer(ReadString(stream, entitiesLump.nOffset, entitiesLump.nLength)));
    }
    
    //BROKEN
    private void ParseTextures(FileStream stream, BSPHeader header) {
        var texturesLump = header.lump[LUMP_TEXTURES];
        TryReadStruct(stream, texturesLump.nOffset, out uint textureHeader);
        Console.WriteLine($"Found {textureHeader} textures. Contained within {texturesLump.nLength}, starting at {texturesLump.nOffset}");
        textures = new List<BSPMipTexture>();
        List<int> textureOffsets = new List<int>();
        for (int i = 0; i < textureHeader; i++) {
            TryReadStruct(stream, out int mipOffset);
            textureOffsets.Add(mipOffset);
        }
        foreach (var offset in textureOffsets) {
            TryReadStruct(stream, texturesLump.nOffset + offset, out BSPMipTexture texture);
            textures.Add(texture);
        }
    }

    public BSP(string filename) {
        FileStream stream = new FileStream(filename, FileMode.Open);
        TryReadStruct(stream, 0, out BSPHeader header);
        ParseEntities(stream, header);
    }

    public ICollection<BSPEntity> GetEntities() => entities;
    public ICollection<BSPMipTexture> GetTextures() => textures;
}