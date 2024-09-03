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
    public string GetResourceFilePath() => $"{filepath.Substring(0, filepath.Length - 4)}.res";
    
    private DirectoryInfo addonDirectory;
    
    private static bool TryReadStruct<T>(Stream stream, out T? output) {
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
    private static bool TryReadStruct<T>(Stream stream, long offset, out T? output) {
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
        entities = new List<BSPEntity>();
        textures = new List<BSPMipTexture>();
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
        foreach (var weapon in entities.Where((ent) => ent.ContainsKey("classname") && ent["classname"].StartsWith("weapon_"))) {
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

        foreach (var monster in entities.Where((ent) => ent.ContainsKey("classname") && ent["classname"].StartsWith("monster") || ent.ContainsKey("classname") && ent["classname"] == "squadmaker")) {
            if (monster.TryGetValue("model", out string? monsterModel)) {
                if (monsterModel.StartsWith("*")) {
                    continue;
                }
                resources.TryAdd(monsterModel, new BSPResource(monsterModel, new BSPResourceEntitySource(monster)));
            }
        }

        resources.AddSprite( "env_sprite", "model");
        resources.AddModel( "item_generic", "model");
        resources.AddModel( "func_breakable", "gibmodel");
        resources.AddSprite( "trigger_camera", "cursor_sprite");
        resources.AddSprite( "cycler_wreckage", "model");
        resources.AddSprite( "env_beam", "texture");
        resources.AddSprite( "env_laser", "texture");
        resources.AddSprite( "env_sprite", "model");
        resources.AddModel( "squadmaker", "new_model");
        resources.AddModel( "env_beverage", "model");
        resources.AddSkybox( "trigger_changesky", "skyname");
        resources.AddSkybox( "worldspawn", "skyname");
        resources.AddSound( "func_train", "noise");
        resources.AddModel( "weapon_custom_projectile", "projectile_mdl");
        resources.AddModel( "item_inventory", "model");
        resources.AddModel( "trigger_createentity", "-model");
        resources.AddSound( "scripted_sentence", "sentence");
        resources.AddModel( "weaponbox", "model");
        resources.AddModel( "cycler", "model");
        resources.AddModel( "env_shooter", "shootmodel");
        resources.AddSound( "env_shake", "message");
        resources.AddSprite( "env_spritetrain", "model");
        resources.AddSound( "env_spritetrain", "noise");
        resources.AddSound( "env_spritetrain", "noise1");
        resources.AddSound( "env_spritetrain", "stopsnd");
        resources.AddSound( "env_spritetrain", "movesnd");
        resources.AddSound( "func_button", "sounds");
        resources.AddSound( "func_button", "noise");
        resources.AddSound( "func_button", "locked_sound_override");
        resources.AddSound( "func_button", "unlocked_sound_override");
        resources.AddSound( "func_door", "movesnd");
        resources.AddSound( "func_door", "noise1");
        resources.AddSound( "func_door", "noise2");
        resources.AddSound( "func_door", "stopsnd");
        resources.AddSound( "func_door", "locked_sound");
        resources.AddSound( "func_door", "unlocked_sound");
        resources.AddSound( "func_door", "locked_sound_override");
        resources.AddSound( "func_door", "unlocked_sound_override");
        resources.AddSound( "func_door_rotating", "movesnd");
        resources.AddSound( "func_door_rotating", "noise1");
        resources.AddSound( "func_door_rotating", "noise2");
        resources.AddSound( "func_door_rotating", "stopsnd");
        resources.AddSound( "func_door_rotating", "locked_sound");
        resources.AddSound( "func_door_rotating", "unlocked_sound");
        resources.AddSound( "func_door_rotating", "locked_sound_override");
        resources.AddSound( "func_door_rotating", "unlocked_sound_override");
        resources.AddSound( "func_healthcharger", "CustomDeniedSound");
        resources.AddSound( "func_healthcharger", "CustomStartSound");
        resources.AddSound( "func_healthcharger", "CustomLoopSound");
        resources.AddSound( "func_plat", "movesnd");
        resources.AddSound( "func_plat", "stopsnd");
        resources.AddSound( "func_plat", "noise");
        resources.AddSound( "func_plat", "noise1");
        resources.AddSound( "func_platrot", "movesnd");
        resources.AddSound( "func_platrot", "stopsnd");
        resources.AddSound( "func_platrot", "noise");
        resources.AddSound( "func_platrot", "noise1");
        resources.AddModel( "func_pushable", "gibmodel");
        resources.AddSound( "func_recharge", "CustomDeniedSound");
        resources.AddSound( "func_recharge", "CustomStartSound");
        resources.AddSound( "func_recharge", "CustomLoopSound");
        resources.AddSound( "func_rot_button", "sounds");
        resources.AddSound( "func_rot_button", "noise");
        resources.AddSound( "func_rot_button", "locked_sound_override");
        resources.AddSound( "func_rot_button", "unlocked_sound_override");
        resources.AddSound( "func_train", "movesnd");
        resources.AddSound( "func_train", "stopsnd");
        resources.AddSound( "func_train", "noise");
        resources.AddSound( "func_train", "noise1");
        resources.AddModel( "trigger_changemodel", "model");

        foreach (var tank in GetEntities().Where((ent) => ent.ContainsKey("classname") && ent["classname"] == "func_tank" || ent.ContainsKey("classname") && ent["classname"] == "func_tanklaser")) {
            if (tank.TryGetValue("spritesmoke", out var spriteSmoke)) {
                resources.TryAdd($"sprites/{spriteSmoke}", new BSPResource($"sprites/{spriteSmoke}", new BSPResourceEntitySource(tank)));
            }
            if (tank.TryGetValue("spriteflash", out var spriteFlash)) {
                resources.TryAdd($"sprites/{spriteFlash}", new BSPResource($"sprites/{spriteFlash}", new BSPResourceEntitySource(tank)));
            }
        }

        foreach (var soundListEntity in GetEntities().Where((ent) => ent.ContainsKey("soundlist"))) {
            ParseSoundReplacementFile(resources, new BSPResourceEntitySource(soundListEntity), soundListEntity["soundlist"]);
        }

        //foreach (var file in addonDirectory.GetFiles()) {
            //if (file.FullName.EndsWith(".wad")) {
                //resources.TryAdd(file.Name, new BSPResource(file.Name, new BSPResourceArbitrary("by assumption")));
            //}
        //}

        if (File.Exists(GetConfigFilePath())) {
            var config = new SvenConfig(File.ReadAllText(GetConfigFilePath()));
            if (config.TryGetValue("globalmodellist", out var modelReplacementFilePath)) {
                ParseModelReplacementFile(resources, new BSPResourceFileSource(modelReplacementFilePath), modelReplacementFilePath);
            }
            if (config.TryGetValue("globalsoundlist", out var soundReplacementFilePath)) {
                ParseSoundReplacementFile(resources, new BSPResourceFileSource(soundReplacementFilePath), soundReplacementFilePath);
            }

            if (config.TryGetValue("sentence_file", out var sentenceFilePath)) {
                var sentenceFile = Path.Combine(addonDirectory.FullName, sentenceFilePath);
                if (File.Exists(sentenceFile)) {
                    var keyPairs = new SentenceTokenizer(File.ReadAllText(sentenceFile));
                    foreach (var pair in keyPairs) {
                        // Double check we're actually using a value from the sentences.
                        if (!pair.Key.StartsWith("HEV") && !GetEntities().Any((ent) => ent.TryGetValue("sentence", out var sentenceValue) && sentenceValue.Trim('!') == pair.Key ||
                                                                                               ent.TryGetValue("UseSentence", out var useSentenceValue) && useSentenceValue.Trim('!') == pair.Key ||
                                                                                               ent.TryGetValue("locked_sentence_override", out var lockedSentenceValue) && lockedSentenceValue.Trim('!') == pair.Key ||
                                                                                               ent.TryGetValue("unlocked_sentence_override", out var unlockedSentenceOverride) && unlockedSentenceOverride.Trim('!') == pair.Key ||
                                                                                               ent.TryGetValue("locked_sentence", out var locked) && locked.Trim('!') == pair.Key ||
                                                                                               ent.TryGetValue("unlocked_sentence", out var unlocked) && unlocked.Trim('!') == pair.Key
                            )) {
                            continue;
                        }
                        if (pair.Value == "null.wav") {
                            continue;
                        }
                        var soundPath = $"sound/{pair.Value}.wav";
                        resources.TryAdd(soundPath, new BSPResource(soundPath, new BSPResourceFileSource(sentenceFile)));
                    }
                }
                
            }
        }

        resources.Clean();
        return resources;
    }

    private void ParseSoundReplacementFile(BSPResources resources, IResourceSource source, string value) {
        var mapName = Path.GetFileName(filepath);
        var startPath = Path.Combine(addonDirectory.FullName, "sound", mapName.Substring(0, mapName.Length-4));
        var providedPath = Path.Combine(startPath, value);
        var uri1 = new Uri(providedPath);
        var uri2 = new Uri(addonDirectory.FullName);
        var relativePath = uri2.MakeRelativeUri(uri1).ToString();
        if (relativePath.StartsWith(addonDirectory.Name)) {
            relativePath = relativePath.Substring(addonDirectory.Name.Length+1);
        }
        resources.TryAdd(relativePath, new BSPResource(relativePath, source));
        if (!File.Exists(providedPath)) {
            return;
        }
        foreach (var pair in new BSPTokenizer(File.ReadAllText(Path.Combine(addonDirectory.FullName, providedPath))).GetKeyValues()) {
            if (pair.Value == "null.wav") {
                continue;
            }
            resources.TryAdd($"sound/{pair.Value}", new BSPResource($"sound/{pair.Value}", source));
        }
    }
    
    private void ParseModelReplacementFile(BSPResources resources, IResourceSource source, string value) {
        var mapName = Path.GetFileName(filepath);
        var startPath = Path.Combine(addonDirectory.FullName, "models", mapName.Substring(0, mapName.Length-4));
        var providedPath = Path.Combine(startPath, value);
        var uri1 = new Uri(providedPath);
        var uri2 = new Uri(addonDirectory.FullName);
        var relativePath = uri2.MakeRelativeUri(uri1).ToString();
        if (relativePath.StartsWith(addonDirectory.Name)) {
            relativePath = relativePath.Substring(addonDirectory.Name.Length+1);
        }
        resources.TryAdd(relativePath, new BSPResource(relativePath, source));
        if (!File.Exists(providedPath)) {
            return;
        }
        foreach (var pair in new BSPTokenizer(File.ReadAllText(Path.Combine(addonDirectory.FullName, providedPath))).GetKeyValues()) {
            if (pair.Value.StartsWith("*")) {
                continue;
            }
            resources.TryAdd(pair.Value, new BSPResource(pair.Value, source));
        }
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
                if (file.Name.ToLowerInvariant() == fileName.ToLowerInvariant() && file.Name != fileName) {
                    malformed_resources.TryAdd(check.Key, check.Value);
                }
            }
        }
        return malformed_resources;
    }
    
    private static bool FileExistsCaseSensitive(string filename) {
        string? name = Path.GetDirectoryName(filename);
        return name != null && Array.Exists(Directory.GetFiles(name), s => s == Path.GetFullPath(filename));
    }
    
    public BSPResources FixMalformedResources() {
        var original_resources = GetResourceFile();
        // we assume the BSP has the correct casing.
        var assetsForgottenToBeIncluded = GetResources().Where((a) => !original_resources.ContainsKey(a.Key));
        var malformed_resources = new BSPResources(this);
        foreach (var check in assetsForgottenToBeIncluded) {
            var fileName = Path.GetFileName(check.Key);
            var directory = Path.GetDirectoryName(check.Key);
            if (directory == null) {
                continue;
            }
            var directoryInfo = new DirectoryInfo(Path.Combine(GetAddonDirectory().FullName, directory));
            if (!directoryInfo.Exists) {
                continue;
            }
            // We hit something, even if it might be the wrong thing, there's no way to know....
            if (FileExistsCaseSensitive(Path.Combine(directoryInfo.FullName,fileName))) {
                continue;
            }
            foreach (var file in directoryInfo.GetFiles()) {
                if (file.Name.ToLowerInvariant() == fileName.ToLowerInvariant() && file.Name != fileName) {
                    Console.WriteLine($"Renaming {file.FullName} to {Path.Combine(directoryInfo.FullName, fileName)}");
                    File.Move(file.FullName, Path.Combine(directoryInfo.FullName, fileName));
                }
            }
        }
        return malformed_resources;
    }
    public override string ToString() {
        return Path.GetFileName(filepath);
    }
}
