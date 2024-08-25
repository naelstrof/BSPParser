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
    private string filename;
    private DirectoryInfo addonDirectory;
    
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
        entities = new List<BSPEntity>(new BSPEntityTokenizer(ReadString(stream, entitiesLump.nOffset, entitiesLump.nLength), this));
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

    public BSP(string filePath) {
        this.filename = Path.GetFileName(filePath);
        FileStream stream = new FileStream(filePath, FileMode.Open);
        addonDirectory = new FileInfo(filePath).Directory?.Parent ?? throw new Exception("Map isn't in a directory that makes sense! Please input a map either in a game folder, or freshly unzipped within a maps/ folder.");
        TryReadStruct(stream, 0, out BSPHeader header);
        ParseEntities(stream, header);
    }

    public ICollection<BSPEntity> GetEntities() => entities;
    public ICollection<BSPMipTexture> GetTextures() => textures;

    public DirectoryInfo GetAddonDirectory() => addonDirectory;


    public BSPResources GetResources() {
        var resources = new BSPResources();
        resources.AddSound(this, "ambient_generic", "message");
        resources.AddSound(this, "ambient_music", "message");
        resources.AddModel(this, "weapon_custom_ammo", "w_model");
        resources.AddModel(this, "custom_precache", "model_1");
        resources.AddSound(this, "weapon_custom_sound", "message");
        resources.AddSound(this, "weapon_custom_bullet", "sounds");
        resources.AddSound(this, "weapon_custom_bullet", "windup_snd");
        resources.AddSound(this, "weapon_custom_bullet", "wind_down_snd");
        foreach (var weapon in entities.Where((ent) => ent["classname"] == "weapon_custom")) {
            if (weapon.ContainsKey("sprite_directory") && weapon.TryGetValue("weapon_name", out var weaponName)) {
                var spriteTextPath = $"sprites/{weapon["sprite_directory"]}/{weaponName}.txt";
                resources.TryAdd(spriteTextPath, new BSPResource(spriteTextPath, new BSPResourceEntitySource(weapon)));
                foreach (var line in File.ReadLines(Path.Combine(addonDirectory.FullName, spriteTextPath))) {
                    var splits = line.Split(null);
                    var count = 0;
                    foreach (var element in splits) {
                        if (string.IsNullOrEmpty(element.Trim())) {
                            continue;
                        }

                        if (count++ != 2) continue;
                        resources.TryAdd($"sprites/{element.Trim()}.spr",
                            new BSPResource($"sprites/{element.Trim()}.spr", new BSPResourceEntitySource(weapon)));
                        break;
                    }
                }
            }

            if (weapon.TryGetValue("wpn_p_model", out var pmodel) && !pmodel.StartsWith("*")) {
                resources.TryAdd(pmodel, new BSPResource(pmodel, new BSPResourceEntitySource(weapon)));
            }

            if (weapon.TryGetValue("wpn_v_model", out var vmodel) && !vmodel.StartsWith("*")) {
                resources.TryAdd(vmodel, new BSPResource(vmodel, new BSPResourceEntitySource(weapon)));
            }

            if (weapon.TryGetValue("wpn_w_model", out var wmodel) && !wmodel.StartsWith("*")) {
                resources.TryAdd(wmodel, new BSPResource(wmodel, new BSPResourceEntitySource(weapon)));
            }
        }

        resources.AddSprite(this, "env_sprite", "model");
        resources.AddModel(this, "item_generic", "model");
        resources.AddModel(this, "func_breakable", "gibmodel");
        resources.AddSound(this, "func_door", "noise1");
        resources.AddSound(this, "func_door", "noise2");
        resources.AddSprite(this, "trigger_camera", "cursor_sprite");
        resources.AddModel(this, "squadmaker", "new_model");
        resources.AddSkybox(this, "trigger_changesky", "skyname");
        resources.AddSound(this, "func_train", "noise");
        resources.AddModel(this, "weapon_custom_projectile", "projectile_mdl");
        resources.AddModel(this, "item_inventory", "model");

        resources.Clean();
        return resources;
    }
    public override string ToString() {
        return filename;
    }
}