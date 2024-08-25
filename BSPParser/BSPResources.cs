namespace BSPParser;

public class BSPResources : Dictionary<string,BSPResource> {
    public BSPResources() {
    }
    public BSPResources(string filepath) {
        var filesource = new BSPResourceFileSource(filepath);
        foreach (var line in File.ReadLines(filepath)) {
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
    
    public void AddModel(BSP bsp, string classname, string key) {
        foreach (var ent in bsp.GetEntities().Where((ent) => ent["classname"] == classname && ent.ContainsKey(key))) {
            if (ent[key].StartsWith("*")) {
                continue;
            }
            TryAdd(ent[key], new BSPResource(ent[key],new BSPResourceEntitySource(ent)));
        }
    }
    public void AddSound(BSP bsp, string classname, string key) {
        foreach (var ent in bsp.GetEntities().Where((ent) => ent["classname"] == classname && ent.ContainsKey(key))) {
            var path = $"sound/{ent[key]}";
            TryAdd(path, new BSPResource(path, new BSPResourceEntitySource(ent)));
        }
    }
    public void AddSprite(BSP bsp, string classname, string key) {
        foreach (var ent in bsp.GetEntities().Where((ent) => ent["classname"] == classname && ent.ContainsKey(key))) {
            TryAdd(ent[key], new BSPResource(ent[key], new BSPResourceEntitySource(ent)));
        }
    }
    
    private void CheckSkyboxAndAdd(BSP bsp, string path, IResourceSource source) {
        if (File.Exists(Path.Combine(bsp.GetAddonDirectory().FullName,path))) {
            TryAdd(path, new BSPResource(path, source));
        }
    }

    public void AddSkybox(BSP bsp, string classname, string key) {
        foreach (var skychange in bsp.GetEntities().Where((ent) => ent["classname"] == classname && ent.ContainsKey(key))) {
            var skyname = skychange[key];
            CheckSkyboxAndAdd(bsp, $"gfx/env/{skyname}bk.tga", new BSPResourceEntitySource(skychange));
            CheckSkyboxAndAdd(bsp, $"gfx/env/{skyname}bk.bmp", new BSPResourceEntitySource(skychange));
            CheckSkyboxAndAdd(bsp, $"gfx/env/{skyname}dn.tga", new BSPResourceEntitySource(skychange));
            CheckSkyboxAndAdd(bsp, $"gfx/env/{skyname}dn.bmp", new BSPResourceEntitySource(skychange));
            CheckSkyboxAndAdd(bsp, $"gfx/env/{skyname}ft.tga", new BSPResourceEntitySource(skychange));
            CheckSkyboxAndAdd(bsp, $"gfx/env/{skyname}ft.bmp", new BSPResourceEntitySource(skychange));
            CheckSkyboxAndAdd(bsp, $"gfx/env/{skyname}lf.tga", new BSPResourceEntitySource(skychange));
            CheckSkyboxAndAdd(bsp, $"gfx/env/{skyname}lf.bmp", new BSPResourceEntitySource(skychange));
            CheckSkyboxAndAdd(bsp, $"gfx/env/{skyname}rt.tga", new BSPResourceEntitySource(skychange));
            CheckSkyboxAndAdd(bsp, $"gfx/env/{skyname}rt.bmp", new BSPResourceEntitySource(skychange));
            CheckSkyboxAndAdd(bsp, $"gfx/env/{skyname}up.tga", new BSPResourceEntitySource(skychange));
            CheckSkyboxAndAdd(bsp, $"gfx/env/{skyname}up.bmp", new BSPResourceEntitySource(skychange));
        }
    }
}