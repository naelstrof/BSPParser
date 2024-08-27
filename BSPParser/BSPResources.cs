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
            TryAdd(line.Trim(), new BSPResource(line.Trim(),filesource));
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
        foreach (var ent in bsp.GetEntities().Where((ent) => ent["classname"] == classname && ent.ContainsKey(key))) {
            if (ent[key].StartsWith("*")) {
                continue;
            }
            TryAdd(ent[key], new BSPResource(ent[key],new BSPResourceEntitySource(ent)));
        }
    }
    public void AddSound(string classname, string key) {
        foreach (var ent in bsp.GetEntities().Where((ent) => ent["classname"] == classname && ent.ContainsKey(key))) {
            var path = $"sound/{ent[key].TrimStart('+')}";
            TryAdd(path, new BSPResource(path, new BSPResourceEntitySource(ent)));
        }
    }
    public void AddSprite(string classname, string key) {
        foreach (var ent in bsp.GetEntities().Where((ent) => ent["classname"] == classname && ent.ContainsKey(key))) {
            TryAdd(ent[key], new BSPResource(ent[key], new BSPResourceEntitySource(ent)));
        }
    }
    
    private void CheckSkyboxAndAdd(string path, IResourceSource source) {
        if (File.Exists(Path.Combine(bsp.GetAddonDirectory().FullName,path))) {
            TryAdd(path, new BSPResource(path, source));
        }
    }

    public void AddSkybox(string classname, string key) {
        foreach (var skychange in bsp.GetEntities().Where((ent) => ent["classname"] == classname && ent.ContainsKey(key))) {
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

    public void Save(string filepath) {
        StringBuilder builder = new StringBuilder();
        foreach (var pair in this) {
            builder.Append($"{pair.Key}\r\n");
        }

        File.WriteAllText(filepath, builder.ToString());
    }

}