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
    private string filepath;
    private string GetConfigFilePath() => $"{filepath.Substring(0, filepath.Length - 4)}.cfg";
    private string GetResourceFilePath() => $"{filepath.Substring(0, filepath.Length - 4)}.res";
    
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
        entities = new List<BSPEntity>(new BSPTokenizer(ReadString(stream, entitiesLump.nOffset, entitiesLump.nLength), this));
    }
    
    //BROKEN
    private void ParseTextures(FileStream stream, BSPHeader header) {
        var texturesLump = header.lump[LUMP_TEXTURES];
        TryReadStruct(stream, texturesLump.nOffset, out uint textureCount);
        textures = new List<BSPMipTexture>();
        for (int i = 0; i < textureCount; i++) {
            TryReadStruct(stream, texturesLump.nOffset + sizeof(int) * i + sizeof(uint), out int textureOffset);
            Console.WriteLine(textureOffset);
            TryReadStruct(stream, texturesLump.nOffset + textureOffset, out BSPMipTexture texture);
            Console.WriteLine(texture);
            textures.Add(texture);
        }
    }

    public BSP(string filePath) {
        this.filepath = filePath;
        FileStream stream = new FileStream(filePath, FileMode.Open);
        addonDirectory = new FileInfo(filePath).Directory?.Parent ?? throw new Exception("Map isn't in a directory that makes sense! Please input a map either in a game folder, or freshly unzipped within a maps/ folder.");
        TryReadStruct(stream, 0, out BSPHeader header);
        ParseEntities(stream, header);
    }

    public ICollection<BSPEntity> GetEntities() => entities;
    public ICollection<BSPMipTexture> GetTextures() => textures;

    public DirectoryInfo GetAddonDirectory() => addonDirectory;

    public BSPResources GetResources() {
        var resources = new BSPResources(this);
        resources.AddSound( "ambient_generic", "message");
        resources.AddSound( "ambient_music", "message");
        resources.AddModel( "weapon_custom_ammo", "w_model");
        resources.AddModel( "custom_precache", "model_1");
        resources.AddSound( "weapon_custom_sound", "message");
        resources.AddSound( "weapon_custom_bullet", "sounds");
        resources.AddSound( "weapon_custom_bullet", "windup_snd");
        resources.AddSound( "weapon_custom_bullet", "wind_down_snd");
        foreach (var weapon in entities.Where((ent) => ent["classname"].StartsWith("weapon_"))) {
            if (weapon.ContainsKey("sprite_directory") && weapon.TryGetValue("weapon_name", out var weaponName)) {
                var spriteTextPath = $"sprites/{weapon["sprite_directory"]}/{weaponName}.txt";
                resources.TryAdd(spriteTextPath, new BSPResource(spriteTextPath, new BSPResourceEntitySource(weapon)));
                if (!File.Exists(Path.Combine(addonDirectory.FullName, spriteTextPath))) {
                    continue;
                }
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

        foreach (var monster in entities.Where((ent) => ent["classname"].StartsWith("monster") || ent["classname"] == "squadmaker")) {
            if (monster.TryGetValue("model", out string? monsterModel)) {
                if (monsterModel.StartsWith("*")) {
                    continue;
                }
                resources.TryAdd(monsterModel, new BSPResource(monsterModel, new BSPResourceEntitySource(monster)));
            }
            if (monster.TryGetValue("soundlist", out var soundlist)) {
                var providedPath = $"sound/{soundlist.Trim().TrimStart(['.', '/'])}";
                resources.TryAdd(providedPath, new BSPResource(providedPath, new BSPResourceEntitySource(monster)));
                if (!File.Exists(Path.Combine(addonDirectory.FullName, providedPath))) {
                    continue;
                }

                foreach (var pair in new BSPTokenizer(File.ReadAllText(Path.Combine(addonDirectory.FullName, providedPath))).GetKeyValues()) {
                    if (pair.Value == "null.wav") {
                        continue;
                    }
                    resources.TryAdd($"sound/{pair.Value}", new BSPResource($"sound/{pair.Value}", new BSPResourceEntitySource(monster)));
                }
            }
        }

        resources.AddSprite( "env_sprite", "model");
        resources.AddModel( "item_generic", "model");
        resources.AddModel( "func_breakable", "gibmodel");
        resources.AddSound( "func_door", "noise1");
        resources.AddSound( "func_door", "noise2");
        resources.AddSprite( "trigger_camera", "cursor_sprite");
        resources.AddModel( "squadmaker", "new_model");
        resources.AddSkybox( "trigger_changesky", "skyname");
        resources.AddSound( "func_train", "noise");
        resources.AddModel( "weapon_custom_projectile", "projectile_mdl");
        resources.AddModel( "item_inventory", "model");
        resources.AddModel( "trigger_createentity", "-model");
        resources.AddSound( "scripted_sentence", "sentence");
        resources.AddModel( "weaponbox", "model");
        resources.AddSound( "env_shake", "message");
        resources.AddSound( "env_spritetrain", "noise");

        foreach (var file in addonDirectory.GetFiles()) {
            if (file.FullName.EndsWith(".wad")) {
                resources.TryAdd(file.Name, new BSPResource(file.Name, new BSPResourceArbitrary("by assumption")));
            }
        }

        if (File.Exists(GetConfigFilePath())) {
            var config = new SvenConfig(File.ReadAllText(GetConfigFilePath()));
            if (config.TryGetValue("globalmodellist", out var modelReplacementFilePath)) {
                if (modelReplacementFilePath.StartsWith("../")) {
                    modelReplacementFilePath = modelReplacementFilePath.Substring(3);
                }
                var gmrFile = Path.Combine(Path.GetDirectoryName(filepath) ?? throw new InvalidOperationException("Map not found in a directory..."), modelReplacementFilePath);
                if (File.Exists(gmrFile)) {
                    var keyPairs = new BSPTokenizer(File.ReadAllText(gmrFile)).GetKeyValues();
                    foreach (var pair in keyPairs) {
                        resources.TryAdd(pair.Value, new BSPResource(pair.Value, new BSPResourceFileSource(gmrFile)));
                    }
                }
            }
            if (config.TryGetValue("globalsoundlist", out var soundReplacementFilePath)) {
                if (soundReplacementFilePath.StartsWith("../")) {
                    soundReplacementFilePath = soundReplacementFilePath.Substring(3);
                }
                var gsrFile = Path.Combine(Path.GetDirectoryName(filepath) ?? throw new InvalidOperationException("Map not found in a directory..."), soundReplacementFilePath);
                if (File.Exists(gsrFile)) {
                    var keyPairs = new BSPTokenizer(File.ReadAllText(gsrFile)).GetKeyValues();
                    foreach (var pair in keyPairs) {
                        if (pair.Value == "null.wav") {
                            continue;
                        }
                        var soundPath = $"sound/{pair.Value}";
                        resources.TryAdd(soundPath, new BSPResource(soundPath, new BSPResourceFileSource(gsrFile)));
                    }
                }
            }
        }

        resources.Clean();
        return resources;
    }

    public BSPResources GetResourceFile() {
        return new BSPResources(GetResourceFilePath(), this);
    }
    public BSPResources GetMalformedResources() {
        var original_resources = GetResourceFile();
        // we assume the BSP has the correct casing.
        var assetsForgottenToBeIncluded = GetResources().Where((a) => !original_resources.ContainsKey(a.Key));
        var malformed_resources = new BSPResources(this);
        foreach (var check in assetsForgottenToBeIncluded) {
            if (File.Exists(Path.Combine(GetAddonDirectory().FullName, check.Key))) {
                continue;
            }
            var fileName = Path.GetFileName(check.Key);
            var directory = Path.GetDirectoryName(check.Key);
            if (directory == null) {
                continue;
            }
            var directoryInfo = new DirectoryInfo(Path.Combine(GetAddonDirectory().FullName, directory));
            if (!directoryInfo.Exists) {
                continue;
            }
            foreach (var file in directoryInfo.GetFiles()) {
                if (file.Name.ToLowerInvariant() == fileName.ToLowerInvariant()) {
                    malformed_resources.TryAdd(check.Key, check.Value);
                }
            }
        }
        return malformed_resources;
    }
    public override string ToString() {
        return Path.GetFileName(filepath);
    }
}