using System.Runtime.InteropServices;
using System.Text;

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

    public List<string> GetMapExits() {
        List<string> mapExits = new List<string>();
        foreach (var ent in GetEntities().Where((ent) => ent.ContainsKey("classname") && ent["classname"] == "trigger_changelevel" && ent.ContainsKey("map"))) {
            mapExits.Add(ent["map"]);
        }
        return mapExits;
    }

    public string GetEntitiesString() {
        StringBuilder builder = new StringBuilder();
        foreach (var entity in entities) {
            builder.Append(entity);
        }
        return builder.ToString();
    }

    private bool TryMatchEntityToWeaponSpriteText(string weaponName, HashSet<string> files, out string weaponSpriteTextPath) {
        foreach (var file in files) {
            var filename = Path.GetFileNameWithoutExtension(file);
            var fileExtension = Path.GetExtension(file);
            if (!file.StartsWith("sprites")) {
                continue;
            }
            if (fileExtension != ".txt") {
                continue;
            }
            if (filename == weaponName) {
                weaponSpriteTextPath = file;
                return true;
            }
        }
        weaponSpriteTextPath = "";
        return false;
    }

    private void ParseSpriteText(BSPResources resources, string weaponSpriteTextPath, IResourceSource source) {
        if (!weaponSpriteTextPath.EndsWith(".txt")) {
            weaponSpriteTextPath += ".txt";
        }

        var realPath = Path.Combine(addonDirectory.FullName, weaponSpriteTextPath);
        if (!File.Exists(realPath)) {
            Console.Error.WriteLine($"Couldn't find weapon sprite text file {realPath} case-sensitivity issue?...");
            return;
        }
        
        resources.TryAdd(weaponSpriteTextPath, new BSPResource(weaponSpriteTextPath, source));
        var weaponHudTokenizer = new WeaponHudTokenizer(File.ReadAllText(realPath));
        foreach (var sprite in weaponHudTokenizer.GetAllSprites()) {
            resources.AddSprite(sprite, new BSPResourceFileSource($"from {source}, found {weaponSpriteTextPath}"));
        }
    }

    private void HandleWeaponName(BSPResources resources, string weaponName, HashSet<string> allFiles, IResourceSource source) {
        if (TryMatchEntityToWeaponSpriteText(weaponName, allFiles, out var weaponSpriteTextPath)) {
            ParseSpriteText(resources, weaponSpriteTextPath, source);
        }
    }

    private BSPResources GetResources(HashSet<string> allFiles) {
        var resources = new BSPResources(this);
        resources.AddSoundFromEntityAndKey( "ambient_generic", "message");
        resources.AddSoundFromEntityAndKey( "ambient_music", "message");
        resources.AddModelFromEntityAndKey( "weapon_custom_ammo", "w_model");
        resources.AddModelFromEntityAndKey( "custom_precache", "model_1");
        resources.AddSoundFromEntityAndKey( "weapon_custom_sound", "message");
        resources.AddSoundFromEntityAndKey( "weapon_custom_bullet", "sounds");
        resources.AddSoundFromEntityAndKey( "weapon_custom_bullet", "windup_snd");
        resources.AddSoundFromEntityAndKey( "weapon_custom_bullet", "wind_down_snd");
        
        foreach (var squadmakerThatMakesWeapons in entities.Where((ent) => ent.ContainsKey("classname") && ent["classname"] == "squadmaker" && ent.ContainsKey("monstertype") && ent["monstertype"].StartsWith("weapon_"))) {
            HandleWeaponName(resources, squadmakerThatMakesWeapons["monstertype"], allFiles, new BSPResourceEntitySource(squadmakerThatMakesWeapons));
        }
        
        foreach (var createEntityThatMakesWeapons in entities.Where((ent) => ent.ContainsKey("classname") && ent["classname"] == "trigger_createentity" && ent.ContainsKey("m_iszCrtEntChildClass") && ent["m_iszCrtEntChildClass"].StartsWith("weapon_"))) {
            HandleWeaponName(resources, createEntityThatMakesWeapons["m_iszCrtEntChildClass"], allFiles, new BSPResourceEntitySource(createEntityThatMakesWeapons));
        }
        
        foreach (var weapon in entities.Where((ent) => ent.ContainsKey("classname") && ent["classname"].StartsWith("weapon_"))) {
            if (weapon.TryGetValue("CustomSpriteDir", out var spriteDir)) {
                ParseSpriteText(resources, $"sprites/{spriteDir}/{weapon["classname"]}.txt", new BSPResourceEntitySource(weapon));
            } else {
                HandleWeaponName(resources, weapon["classname"], allFiles, new BSPResourceEntitySource(weapon));
            }
            
            if (weapon.TryGetValue("wpn_p_model", out var pmodel) && !pmodel.StartsWith("*")) {
                resources.AddModel(pmodel, new BSPResourceEntitySource(weapon));
            }

            if (weapon.TryGetValue("wpn_v_model", out var vmodel) && !vmodel.StartsWith("*")) {
                resources.AddModel(vmodel, new BSPResourceEntitySource(weapon));
            }

            if (weapon.TryGetValue("wpn_w_model", out var wmodel) && !wmodel.StartsWith("*")) {
                resources.AddModel(wmodel, new BSPResourceEntitySource(weapon));
            }
        }

        foreach (var monster in entities.Where((ent) => ent.ContainsKey("classname") && ent["classname"].StartsWith("monster") || ent.ContainsKey("classname") && ent["classname"] == "squadmaker")) {
            if (monster.TryGetValue("model", out string? monsterModel)) {
                if (monsterModel.StartsWith("*")) {
                    continue;
                }
                resources.AddModel(monsterModel, new BSPResourceEntitySource(monster));
            }
        }

        resources.AddModelFromEntityAndKey( "item_generic", "model");
        resources.AddModelFromEntityAndKey( "func_breakable", "gibmodel");
        resources.AddSpriteFromEntityAndKey( "trigger_camera", "cursor_sprite");
        resources.AddSpriteFromEntityAndKey( "cycler_wreckage", "model");
        resources.AddSpriteFromEntityAndKey( "env_beam", "texture");
        resources.AddSpriteFromEntityAndKey( "env_laser", "texture");
        
        foreach (var envSprite in GetEntities().Where((ent) => ent.ContainsKey("classname") && ent["classname"] == "env_sprite" && ent.ContainsKey("model"))) {
            var model = envSprite["model"];
            if (resources.TryPathToModelPath(model, out var modelPath)) {
                if (File.Exists(Path.Combine(addonDirectory.FullName, modelPath))) {
                    resources.AddModel(model, new BSPResourceEntitySource(envSprite));
                }
            }
            if (resources.TryPathToSpritePath(model, out var spritePath)) {
                if (File.Exists(Path.Combine(addonDirectory.FullName, spritePath))) {
                    resources.AddSprite(spritePath, new BSPResourceEntitySource(envSprite));
                }
            }
        }

        resources.AddModelFromEntityAndKey( "squadmaker", "new_model");
        resources.AddModelFromEntityAndKey( "env_beverage", "model");
        resources.AddSkyboxFromEntityAndKey( "trigger_changesky", "skyname");
        resources.AddSkyboxFromEntityAndKey( "worldspawn", "skyname");
        resources.AddSoundFromEntityAndKey( "func_train", "noise");
        resources.AddModelFromEntityAndKey( "weapon_custom_projectile", "projectile_mdl");
        resources.AddModelFromEntityAndKey( "item_inventory", "model");
        resources.AddModelFromEntityAndKey( "trigger_createentity", "-model");
        resources.AddSoundFromEntityAndKey( "scripted_sentence", "sentence");
        resources.AddModelFromEntityAndKey( "weaponbox", "model");
        resources.AddModelFromEntityAndKey( "cycler", "model");
        resources.AddModelFromEntityAndKey( "env_shooter", "shootmodel");
        resources.AddSoundFromEntityAndKey( "env_shake", "message");
        resources.AddSpriteFromEntityAndKey( "env_spritetrain", "model");
        resources.AddSoundFromEntityAndKey( "env_spritetrain", "noise");
        resources.AddSoundFromEntityAndKey( "env_spritetrain", "noise1");
        resources.AddSoundFromEntityAndKey( "env_spritetrain", "stopsnd");
        resources.AddSoundFromEntityAndKey( "env_spritetrain", "movesnd");
        resources.AddSoundFromEntityAndKey( "func_button", "sounds");
        resources.AddSoundFromEntityAndKey( "func_button", "noise");
        resources.AddSoundFromEntityAndKey( "func_button", "locked_sound_override");
        resources.AddSoundFromEntityAndKey( "func_button", "unlocked_sound_override");
        resources.AddSoundFromEntityAndKey( "func_door", "movesnd");
        resources.AddSoundFromEntityAndKey( "func_door", "noise1");
        resources.AddSoundFromEntityAndKey( "func_door", "noise2");
        resources.AddSoundFromEntityAndKey( "func_door", "stopsnd");
        resources.AddSoundFromEntityAndKey( "func_door", "locked_sound");
        resources.AddSoundFromEntityAndKey( "func_door", "unlocked_sound");
        resources.AddSoundFromEntityAndKey( "func_door", "locked_sound_override");
        resources.AddSoundFromEntityAndKey( "func_door", "unlocked_sound_override");
        resources.AddSoundFromEntityAndKey( "func_door_rotating", "movesnd");
        resources.AddSoundFromEntityAndKey( "func_door_rotating", "noise1");
        resources.AddSoundFromEntityAndKey( "func_door_rotating", "noise2");
        resources.AddSoundFromEntityAndKey( "func_door_rotating", "stopsnd");
        resources.AddSoundFromEntityAndKey( "func_door_rotating", "locked_sound");
        resources.AddSoundFromEntityAndKey( "func_door_rotating", "unlocked_sound");
        resources.AddSoundFromEntityAndKey( "func_door_rotating", "locked_sound_override");
        resources.AddSoundFromEntityAndKey( "func_door_rotating", "unlocked_sound_override");
        resources.AddSoundFromEntityAndKey( "func_healthcharger", "CustomDeniedSound");
        resources.AddSoundFromEntityAndKey( "func_healthcharger", "CustomStartSound");
        resources.AddSoundFromEntityAndKey( "func_healthcharger", "CustomLoopSound");
        resources.AddSoundFromEntityAndKey( "func_plat", "movesnd");
        resources.AddSoundFromEntityAndKey( "func_plat", "stopsnd");
        resources.AddSoundFromEntityAndKey( "func_plat", "noise");
        resources.AddSoundFromEntityAndKey( "func_plat", "noise1");
        resources.AddSoundFromEntityAndKey( "func_platrot", "movesnd");
        resources.AddSoundFromEntityAndKey( "func_platrot", "stopsnd");
        resources.AddSoundFromEntityAndKey( "func_platrot", "noise");
        resources.AddSoundFromEntityAndKey( "func_platrot", "noise1");
        resources.AddModelFromEntityAndKey( "func_pushable", "gibmodel");
        resources.AddSoundFromEntityAndKey( "func_recharge", "CustomDeniedSound");
        resources.AddSoundFromEntityAndKey( "func_recharge", "CustomStartSound");
        resources.AddSoundFromEntityAndKey( "func_recharge", "CustomLoopSound");
        resources.AddSoundFromEntityAndKey( "func_rot_button", "sounds");
        resources.AddSoundFromEntityAndKey( "func_rot_button", "noise");
        resources.AddSoundFromEntityAndKey( "func_rot_button", "locked_sound_override");
        resources.AddSoundFromEntityAndKey( "func_rot_button", "unlocked_sound_override");
        resources.AddSoundFromEntityAndKey( "func_train", "movesnd");
        resources.AddSoundFromEntityAndKey( "func_train", "stopsnd");
        resources.AddSoundFromEntityAndKey( "func_train", "noise");
        resources.AddSoundFromEntityAndKey( "func_train", "noise1");
        resources.AddModelFromEntityAndKey( "trigger_changemodel", "model");

        foreach (var tank in GetEntities().Where((ent) => ent.ContainsKey("classname") && ent["classname"] == "func_tank" || ent.ContainsKey("classname") && ent["classname"] == "func_tanklaser")) {
            if (tank.TryGetValue("spritesmoke", out var spriteSmoke)) {
                resources.AddSprite(spriteSmoke, new BSPResourceEntitySource(tank));
            }
            if (tank.TryGetValue("spriteflash", out var spriteFlash)) {
                resources.AddSprite(spriteFlash, new BSPResourceEntitySource(tank));
            }
        }

        foreach (var soundListEntity in GetEntities().Where((ent) => ent.ContainsKey("soundlist"))) {
            ParseSoundReplacementFile(resources, new BSPResourceEntitySource(soundListEntity), soundListEntity["soundlist"]);
        }

        // We automatically detect if we're scanning an individual addon or not, and include wads by assumption if we are.
        if (addonDirectory.Name != "svencoop_addon" && addonDirectory.Name != "svencoop" && addonDirectory.Name != "svencoop_downloads") {
            foreach (var file in addonDirectory.GetFiles()) {
                if (file.FullName.EndsWith(".wad")) {
                    resources.TryAdd(file.Name, new BSPResource(file.Name, new BSPResourceArbitrary("by assumption")));
                }
            }
        }

        if (File.Exists(GetConfigFilePath())) {
            var config = new SvenConfigTokenizer(File.ReadAllText(GetConfigFilePath()));
            foreach (var pair in config) {
                if (pair.Key.StartsWith("weapon_")) {
                    HandleWeaponName(resources, pair.Key, allFiles, new BSPResourceFileSource(GetConfigFilePath()));
                }
            }
            if (config.TryGetValue("globalmodellist", out var modelReplacementFilePath)) {
                ParseModelReplacementFile(resources, new BSPResourceFileSource(GetConfigFilePath()), modelReplacementFilePath);
            }
            if (config.TryGetValue("globalsoundlist", out var soundReplacementFilePath)) {
                ParseSoundReplacementFile(resources, new BSPResourceFileSource(GetConfigFilePath()), soundReplacementFilePath);
            }

            if (config.TryGetValue("map_script", out var mapScriptFolder)) {
                var workingDirectory = Path.Combine(addonDirectory.FullName, "scripts", "maps");
                ParseAngelScript(resources, mapScriptFolder, new DirectoryInfo(workingDirectory),0);
            }

            if (config.TryGetValue("sentence_file", out var sentenceFilePath)) {
                resources.TryParseSentenceFile(sentenceFilePath);
            }
        }

        resources.Clean();
        return resources;
    }

    private void ParseAngelScript(BSPResources resources, string scriptPath, DirectoryInfo workingDir, int depth) {
        if (depth > 64) {
            Console.Error.WriteLine($"Found a 64 deep include chain with script {scriptPath}, giving up, cyclical dependency?");
            return;
        }
        if (!scriptPath.EndsWith(".as")) {
            scriptPath += ".as";
        }
        var path = Path.Combine(workingDir.FullName, scriptPath);
        FileInfo file = new FileInfo(path);
        if (!workingDir.Exists || !file.Exists) {
            Console.Error.WriteLine($"Couldn't find angel script {file.FullName} case-sensitivity issue or default asset?...skipping");
            return;
        }
        var tokenizer = new AngelScriptTokenizer(File.ReadAllText(file.FullName));
        var includes = new HashSet<string>(tokenizer.GetAllIncludes());
        foreach (var include in includes) {
            if (file.Directory != null) {
                ParseAngelScript(resources, include, file.Directory, depth+1);
            }
        }
        var strings = new HashSet<string>(tokenizer.GetAllStrings());
        foreach (var str in strings) {
            var testString = str.TrimStart('/');
            if (resources.TryPathToSoundPath(testString, out var soundPath)) {
                if (File.Exists(Path.Combine(addonDirectory.FullName, soundPath))) {
                    resources.AddSound(testString, new BSPResourceFileSource($"AngelScript: {scriptPath}"));
                }
            }
            if (resources.TryPathToModelPath(testString, out var modelPath)) {
                if (File.Exists(Path.Combine(addonDirectory.FullName, modelPath))) {
                    resources.AddModel(modelPath, new BSPResourceFileSource($"AngelScript: {scriptPath}"));
                }
            }
            if (resources.TryPathToSpritePath(testString, out var spritePath)) {
                if (File.Exists(Path.Combine(addonDirectory.FullName, spritePath))) {
                    resources.AddSprite(spritePath, new BSPResourceFileSource($"AngelScript: {scriptPath}"));
                }
            }
            resources.AddSkybox(testString, new BSPResourceFileSource($"AngelScript: {scriptPath}"));
        }
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
            resources.AddSound(pair.Value, new BSPResourceFileSource(value));
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
            resources.AddModel(pair.Value, new BSPResourceFileSource(value));
        }
    }

    public void FixResourcesInPlace(HashSet<string> defaultKeys, HashSet<string> allFiles) {
        BSPResources generated_resources = GetResources(allFiles);
        BSPResources original_resources = GetResourceFile();
        
        generated_resources.RemoveBatch(defaultKeys);
        original_resources.RemoveBatch(defaultKeys);

        // Assets that we missed, possibly referred to by script, or erroneously included by the map creator. Impossible to differentiate. So we add them all.
        foreach (var resource in
                 original_resources.Where((a) => !generated_resources.ContainsKeyCaseInsensitive(a.Key))) {
            generated_resources.TryAdd(resource.Key, resource.Value);
        }

        generated_resources.FixMalformedResources(GetAddonDirectory());

        foreach (var missingResource in generated_resources.Where((a) =>
                     !File.Exists(Path.Combine(GetAddonDirectory().FullName, a.Key)))) {
            if (!missingResource.Value.source.GetInferred()) {
                Console.WriteLine($"\tRemoving due to missing from disk: {missingResource.Value}");
            }
            generated_resources.Remove(missingResource.Key);
        }

        foreach (var resource in generated_resources.Where((a) =>
                     !original_resources.ContainsKey(a.Key) &&
                     File.Exists(Path.Combine(GetAddonDirectory().FullName, a.Key)))) {
            Console.WriteLine($"\tAdding: {resource.Value}");
        }

        generated_resources.Save(GetResourceFilePath());
    }

    public BSPResources GetResourceFile() {
        return new BSPResources(GetResourceFilePath(), this);
    }
    public override string ToString() {
        return Path.GetFileName(filepath);
    }
}
