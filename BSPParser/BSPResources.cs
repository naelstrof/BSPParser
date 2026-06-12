using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace BSPParser;

public class BSPResources : Dictionary<string,BSPResource> {
    private BSP bsp;
    public BSPResources(BSP bsp) {
        this.bsp = bsp;
    }
    public BSPResources(string resourcesFilePath, BSP bsp) {
        this.bsp = bsp;
        if (!File.Exists(resourcesFilePath)) {
            return;
        }
        var filesource = new BSPResourceFileSource(resourcesFilePath);
        foreach (var line in File.ReadLines(resourcesFilePath)) {
            var trimmed = line.Trim();
            var filename = Path.GetFileName(trimmed);
            var filepath = Path.GetDirectoryName(trimmed) ?? string.Empty;
            // Sven coop automatically infers the existence of p_, v_, w_ variants of models, so we have to add them all in case the user only included one of them.
            if (filename.StartsWith("p_") || filename.StartsWith("v_") || filename.StartsWith("w_")) {
                var playermodel = Path.Combine(filepath, "p"+filename[1..]);
                var viewmodel = Path.Combine(filepath, "v"+filename[1..]);
                var worldmodel = Path.Combine(filepath, "w"+filename[1..]);
                TryAdd(playermodel.Trim(), new BSPResource(line.Trim(), new BSPResourceInferred(resourcesFilePath)));
                TryAdd(viewmodel.Trim(), new BSPResource(line.Trim(), new BSPResourceInferred(resourcesFilePath)));
                TryAdd(worldmodel.Trim(), new BSPResource(line.Trim(), new BSPResourceInferred(resourcesFilePath)));
            } else {
                TryAdd(line.Trim(), new BSPResource(line.Trim(), filesource));
            }
        }
        Clean();
    }

    public void Clean() {
        var removePairs = this.Where((pair) => string.IsNullOrEmpty(pair.Key.Trim()) || pair.Key.Trim().StartsWith("//"));
        foreach (var pair in removePairs) {
            Remove(pair.Key);
        }
    }

    public bool TryPathToModelPath(string path, out string modelPath) {
        modelPath = path.Trim();
        if (string.IsNullOrEmpty(modelPath)) {
            return false;
        }
        if (modelPath.StartsWith("*")) {
            return false;
        }
        if (!modelPath.EndsWith(".mdl")) {
            modelPath += ".mdl";
        }
        if (!modelPath.StartsWith("models/")) {
            modelPath = "models/" + modelPath;
        }
        modelPath = modelPath.Trim();
        return true;
    }
    public bool TryPathToSpritePath(string path, out string spritePath) {
        spritePath = path.TrimStart('/').Trim();
        if (string.IsNullOrEmpty(spritePath)) {
            return false;
        }
        if (!spritePath.EndsWith(".spr")) {
            spritePath += ".spr";
        }
        if (!spritePath.StartsWith("sprites/")) {
            spritePath = "sprites/" + spritePath;
        }
        spritePath = spritePath.Trim();
        return true;
    }

    public bool TryPathToSoundPath(string path, out string soundPath) {
        soundPath = path.Trim();
        if (string.IsNullOrEmpty(soundPath)) {
            return false;
        }
        
        // Is a sentence
        if (soundPath.StartsWith('!') || soundPath.StartsWith('+')) {
            return false;
        }
        
        // built-in sound
        if (int.TryParse(soundPath, out var number) && number is >= 0 and <= 25) {
            return false;
        }
        
        soundPath = soundPath.TrimStart(['#',',']);
        if (!soundPath.StartsWith("sound/") && !soundPath.StartsWith("../")) {
            soundPath = "sound/" + soundPath;
        }
        soundPath = soundPath.TrimStart(['.','/']);
        if (string.IsNullOrEmpty(Path.GetExtension(soundPath))) {
            var findSound = FindFileWithoutExtension(soundPath);
            if (findSound != null) {
                soundPath += Path.GetExtension(findSound);
            } else {
                soundPath += ".wav";
            }
        }
        soundPath = soundPath.Trim();
        return true;
    }

    public void AddModel(string path, IResourceSource source) {
        if (!TryPathToModelPath(path, out var modelPath)) {
            return;
        }
        var filename = Path.GetFileName(modelPath);
        var filepath = Path.GetDirectoryName(modelPath) ?? string.Empty;
        if (filename.StartsWith("p_") || filename.StartsWith("v_") || filename.StartsWith("w_")) {
            var playermodel = Path.Combine(filepath, "p" + filename[1..]);
            var viewmodel = Path.Combine(filepath, "v" + filename[1..]);
            var worldmodel = Path.Combine(filepath, "w" + filename[1..]);
            TryAdd(playermodel.Trim(), new BSPResource(playermodel, new BSPResourceInferred($"inferred from: {source}")));
            TryAdd(viewmodel.Trim(), new BSPResource(viewmodel, new BSPResourceInferred($"inferred from {source}")));
            TryAdd(worldmodel.Trim(), new BSPResource(worldmodel, new BSPResourceInferred($"inferred from {source}")));
        } else {
            TryAdd(modelPath.Trim(), new BSPResource(modelPath, source));
        }
    }
    
    public void AddModelFromEntityAndKey(string classname, string key) {
        foreach (var ent in bsp.GetEntities().Where((ent) => ent.ContainsKey("classname") && ent["classname"] == classname && ent.ContainsKey(key))) {
            AddModel(ent[key], new BSPResourceEntitySource(ent));
        }
    }

    private string? FindFileWithoutExtension(string path) {
        var folder = Path.GetDirectoryName(path);
        if (folder == null) return null;
        if (!Directory.Exists(folder)) {
            return null;
        }
        foreach (var file in Directory.GetFiles(folder)) {
            if (Path.GetFileNameWithoutExtension(file) == Path.GetFileNameWithoutExtension(path)) {
                return file;
            }
        }
        return null;
    }

    public void TryParseSentenceFile(string sentencePath) {
        sentencePath = sentencePath.Trim().TrimStart(['!','+','#','.',',']);
        if (!sentencePath.StartsWith("sound/")) {
            sentencePath = "sound/"+sentencePath;
        }

        var ext = Path.GetExtension(sentencePath);
        if (string.IsNullOrEmpty(ext)) {
            sentencePath += ".txt";
        } else if (ext != ".txt") {
            return;
        }
        
        sentencePath = Path.Combine(bsp.GetAddonDirectory().FullName, sentencePath);
        if (!File.Exists(sentencePath)) {
            return;
        }
        var keyPairs = new SentenceTokenizer(File.ReadAllText(sentencePath));
        foreach (var pair in keyPairs) {
            // Double check we're actually using a value from the sentences.
            if (!pair.Key.StartsWith("HEV") && !bsp.GetEntities().Any((ent) => { return ent.Any(innerPair => innerPair.Value.StartsWith('!') && innerPair.Value.Trim('!') == pair.Key); } )) {
                continue;
            }
            AddSound(pair.Value, new BSPResourceFileSource(sentencePath));
        }
    }

    public void AddSound(string soundPath, IResourceSource source) {
        TryParseSentenceFile(soundPath);
        if (!TryPathToSoundPath(soundPath, out var sound)) {
            return;
        }
        TryAdd(sound, new BSPResource(soundPath, source));
    }

    public void AddSoundFromEntityAndKey(string classname, string key) {
        foreach (var ent in bsp.GetEntities().Where((ent) => ent.ContainsKey("classname") && ent["classname"] == classname && ent.ContainsKey(key))) {
            AddSound(ent[key], new BSPResourceEntitySource(ent));
        }
    }

    public void AddSkybox(string name, IResourceSource source) {
        var skyname = name;
        CheckSkyboxAndAdd( $"gfx/env/{skyname}bk.tga", source);
        CheckSkyboxAndAdd( $"gfx/env/{skyname}bk.bmp", source);
        CheckSkyboxAndAdd( $"gfx/env/{skyname}dn.tga", source);
        CheckSkyboxAndAdd( $"gfx/env/{skyname}dn.bmp", source);
        CheckSkyboxAndAdd( $"gfx/env/{skyname}ft.tga", source);
        CheckSkyboxAndAdd( $"gfx/env/{skyname}ft.bmp", source);
        CheckSkyboxAndAdd( $"gfx/env/{skyname}lf.tga", source);
        CheckSkyboxAndAdd( $"gfx/env/{skyname}lf.bmp", source);
        CheckSkyboxAndAdd( $"gfx/env/{skyname}rt.tga", source);
        CheckSkyboxAndAdd( $"gfx/env/{skyname}rt.bmp", source);
        CheckSkyboxAndAdd( $"gfx/env/{skyname}up.tga", source);
        CheckSkyboxAndAdd( $"gfx/env/{skyname}up.bmp", source);
    }

    public void AddSprite(string name, IResourceSource source) {
        var path = name.TrimStart('/').Trim();
        if (string.IsNullOrEmpty(path)) {
            return;
        }
        if (!path.EndsWith(".spr")) {
            path += ".spr";
        }
        if (!path.StartsWith("sprites/")) {
            path = "sprites/" + path;
        }
        TryAdd(path.Trim(), new BSPResource(path, source));
    }
    
    public void AddSpriteFromEntityAndKey(string classname, string key) {
        foreach (var ent in bsp.GetEntities().Where((ent) => ent.ContainsKey("classname") && ent["classname"] == classname && ent.ContainsKey(key))) {
            AddSprite(ent[key], new BSPResourceEntitySource(ent));
        }
    }
    
    private void CheckSkyboxAndAdd(string path, IResourceSource source) {
        if (File.Exists(Path.Combine(bsp.GetAddonDirectory().FullName,path))) {
            TryAdd(path.Trim(), new BSPResource(path, source));
        }
    }

    public void AddSkyboxFromEntityAndKey(string classname, string key) {
        foreach (var skychange in bsp.GetEntities().Where((ent) => ent.ContainsKey("classname") && ent["classname"] == classname && ent.ContainsKey(key))) {
            AddSkybox(skychange[key], new BSPResourceEntitySource(skychange));
        }
    }

    public bool ContainsKeyCaseInsensitive(string key) {
        foreach (var otherKey in Keys) {
            if (string.Equals(otherKey, key, StringComparison.InvariantCultureIgnoreCase)) {
                return true;
            }
        }
        return false;
    }

    private bool TryGetInvalidFolderCasing(string filePath, [NotNullWhen(true)] out string? invalidFolderName, [NotNullWhen(true)] out DirectoryInfo? workingDir) {
        var dir = Path.GetDirectoryName(filePath);
        while (!string.IsNullOrEmpty(dir) && !Directory.Exists(Path.Combine(bsp.GetAddonDirectory().FullName,dir))) {
            var dirName = Path.GetFileName(dir);
            dir = Path.GetDirectoryName(dir);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(Path.Combine(bsp.GetAddonDirectory().FullName,dir))) {
                workingDir = new DirectoryInfo(Path.Combine(bsp.GetAddonDirectory().FullName,dir));
                invalidFolderName = dirName;
                return true;
            }
        }
        workingDir = null;
        invalidFolderName = null;
        return false;
    }

    private bool TryGetCorrectFolderCasing(string invalidFolderName, DirectoryInfo workingDir, out string? correctFolderName) {
        foreach (var folder in workingDir.GetDirectories()) {
            if (folder.Name.Equals(invalidFolderName, StringComparison.InvariantCultureIgnoreCase)) {
                correctFolderName = folder.Name;
                return true;
            }
        }

        correctFolderName = null;
        return false;
    }

    public void RemoveBatch(ICollection<string> keys) {
        foreach (var key in keys) {
            Remove(key);
        }
    }
    
    public void FixMalformedResources(DirectoryInfo addonDirectory) {
        var keys = Keys;
        foreach (var key in keys) {
            while (TryGetInvalidFolderCasing(key, out var invalidFolderName, out var workingDir)) {
                if (!TryGetCorrectFolderCasing(invalidFolderName, workingDir, out var correctFolderName)) break;
                var newKey = key.Replace(invalidFolderName, correctFolderName);
                var keyValue = this[key];
                Remove(key);
                Add(newKey, keyValue);
                Console.Error.WriteLine($"Fixing incorrect casing on {key}, for folder {invalidFolderName} -> {correctFolderName}");
            }
        }
        
        List<string> paths = new List<string>();
        foreach (var key in Keys) {
            paths.Add(Path.Combine(addonDirectory.FullName, key));
        }
        CaseSensitivityTools.FixMalformedCasing(paths);
    }

    public void Save(string filepath) {
        if (Count == 0 ) {
            if (File.Exists(filepath)) {
                File.Delete(filepath);
            }
            return;
        }
        File.WriteAllText(filepath, ToString());
    }

    public override string ToString() {
        StringBuilder builder = new StringBuilder();
        foreach (var pair in this) {
            builder.Append($"{pair.Key}\r\n");
        }
        return builder.ToString();
    }
}
