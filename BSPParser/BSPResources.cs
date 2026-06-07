using System.Diagnostics;
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
                TryAdd(playermodel, new BSPResource(line.Trim(), new BSPResourceInferred(resourcesFilePath)));
                TryAdd(viewmodel, new BSPResource(line.Trim(), new BSPResourceInferred(resourcesFilePath)));
                TryAdd(worldmodel, new BSPResource(line.Trim(), new BSPResourceInferred(resourcesFilePath)));
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
    
    public void AddModel(string classname, string key) {
        foreach (var ent in bsp.GetEntities().Where((ent) => ent.ContainsKey("classname") && ent["classname"] == classname && ent.ContainsKey(key))) {
            var path = ent[key];
            if (ent[key].StartsWith("*")) {
                continue;
            }
            if (string.IsNullOrEmpty(Path.GetExtension(path))) {
                var findModel = FindFileWithoutExtension(path);
                if (findModel != null) {
                    path += Path.GetExtension(findModel);
                } else {
                    path += ".mdl";
                }
            }

            var filename = Path.GetFileName(path);
            var filepath = Path.GetDirectoryName(path) ?? string.Empty;
            if (filename.StartsWith("p_") || filename.StartsWith("v_") || filename.StartsWith("w_")) {
                var playermodel = Path.Combine(filepath, "p" + filename[1..]);
                var viewmodel = Path.Combine(filepath, "v" + filename[1..]);
                var worldmodel = Path.Combine(filepath, "w" + filename[1..]);
                TryAdd(playermodel, new BSPResource(playermodel, new BSPResourceInferred($"from bsp model ent in {ent.GetParent()}")));
                TryAdd(viewmodel, new BSPResource(viewmodel, new BSPResourceInferred($"from bsp model ent in {ent.GetParent()}")));
                TryAdd(worldmodel, new BSPResource(worldmodel, new BSPResourceInferred($"from bsp model ent in {ent.GetParent()}")));
            } else {
                TryAdd(path, new BSPResource(path, new BSPResourceEntitySource(ent)));
            }
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
    
    public void AddSound(string classname, string key) {
        foreach (var ent in bsp.GetEntities().Where((ent) => ent.ContainsKey("classname") && ent["classname"] == classname && ent.ContainsKey(key))) {
            // Not a sound, we're a sentence!
            if (ent[key].StartsWith('!')) {
                continue;
            }

            // built-in sound
            if (int.TryParse(ent[key], out var number) && number is >= 0 and <= 16) {
                continue;
            }
            var path = $"sound/{ent[key].TrimStart(['+','#'])}";
            if (string.IsNullOrEmpty(Path.GetExtension(path))) {
                var findSound = FindFileWithoutExtension(path);
                if (findSound != null) {
                    path += Path.GetExtension(findSound);
                } else {
                    path += ".wav";
                }
            }
            TryAdd(path, new BSPResource(path, new BSPResourceEntitySource(ent)));
        }
    }
    public void AddSprite(string classname, string key) {
        foreach (var ent in bsp.GetEntities().Where((ent) => ent.ContainsKey("classname") && ent["classname"] == classname && ent.ContainsKey(key))) {
            var path = ent[key];
            if (string.IsNullOrEmpty(Path.GetExtension(path))) {
                var findSprite = FindFileWithoutExtension(path);
                if (findSprite != null) {
                    path += Path.GetExtension(findSprite);
                } else {
                    path += ".spr";
                }
            }
            TryAdd(path, new BSPResource(path, new BSPResourceEntitySource(ent)));
        }
    }
    
    private void CheckSkyboxAndAdd(string path, IResourceSource source) {
        if (File.Exists(Path.Combine(bsp.GetAddonDirectory().FullName,path))) {
            TryAdd(path, new BSPResource(path, source));
        }
    }

    public void AddSkybox(string classname, string key) {
        foreach (var skychange in bsp.GetEntities().Where((ent) => ent.ContainsKey("classname") && ent["classname"] == classname && ent.ContainsKey(key))) {
            var skyname = skychange[key];
            CheckSkyboxAndAdd( $"gfx/env/{skyname}bk.tga", new BSPResourceEntitySource(skychange));
            CheckSkyboxAndAdd( $"gfx/env/{skyname}bk.bmp", new BSPResourceEntitySource(skychange));
            CheckSkyboxAndAdd( $"gfx/env/{skyname}dn.tga", new BSPResourceEntitySource(skychange));
            CheckSkyboxAndAdd( $"gfx/env/{skyname}dn.bmp", new BSPResourceEntitySource(skychange));
            CheckSkyboxAndAdd( $"gfx/env/{skyname}ft.tga", new BSPResourceEntitySource(skychange));
            CheckSkyboxAndAdd( $"gfx/env/{skyname}ft.bmp", new BSPResourceEntitySource(skychange));
            CheckSkyboxAndAdd( $"gfx/env/{skyname}lf.tga", new BSPResourceEntitySource(skychange));
            CheckSkyboxAndAdd( $"gfx/env/{skyname}lf.bmp", new BSPResourceEntitySource(skychange));
            CheckSkyboxAndAdd( $"gfx/env/{skyname}rt.tga", new BSPResourceEntitySource(skychange));
            CheckSkyboxAndAdd( $"gfx/env/{skyname}rt.bmp", new BSPResourceEntitySource(skychange));
            CheckSkyboxAndAdd( $"gfx/env/{skyname}up.tga", new BSPResourceEntitySource(skychange));
            CheckSkyboxAndAdd( $"gfx/env/{skyname}up.bmp", new BSPResourceEntitySource(skychange));
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
    
    public void FixMalformedResources(DirectoryInfo addonDirectory) {
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
