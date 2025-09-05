using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Gacha.ViewModels;

namespace Gacha.Services
{
    public static class ProjectService
    {
        sealed class ProjectDto
        {
            public string version { get; set; } = "1";
            public List<Item> items { get; set; } = new();
            public sealed class Item
            {
                public string setKey { get; set; } = "";
                public string slotKey { get; set; } = "";
                public int rarity { get; set; }
                public int level { get; set; }
                public string main { get; set; } = ""; // 表示用テキスト（MainText）
                public double CR { get; set; }
                public double CD { get; set; }
                public double ATKp { get; set; }
                public double HPp { get; set; }
                public double DEFp { get; set; }
                public double EM { get; set; }
                public bool isLocked { get; set; }
            }
        }

        public static void Save(string path, IEnumerable<ArtifactVM> artifacts)
        {
            var dto = new ProjectDto();
            foreach (var a in artifacts)
            {
                dto.items.Add(new ProjectDto.Item
                {
                    setKey = a.SetKey,
                    slotKey = a.SlotKey,
                    rarity = a.Rarity,
                    level = a.Level,
                    main = a.MainText,
                    CR = a.CR, CD = a.CD, ATKp = a.ATKp, HPp = a.HPp, DEFp = a.DEFp, EM = a.EM,
                    isLocked = a.IsLocked
                });
            }
            var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        public static List<ArtifactVM> Load(string path)
        {
            var json = File.ReadAllText(path);
            var dto = JsonSerializer.Deserialize<ProjectDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                      ?? new ProjectDto();
            var list = new List<ArtifactVM>();
            foreach (var it in dto.items)
            {
                var (paths, has) = ImageProvider.Resolve(it.setKey, it.slotKey);
                list.Add(new ArtifactVM
                {
                    SetKey = it.setKey,
                    SlotKey = it.slotKey,
                    Rarity = it.rarity,
                    Level = it.level,
                    MainText = it.main,
                    CR = it.CR, CD = it.CD, ATKp = it.ATKp, HPp = it.HPp, DEFp = it.DEFp, EM = it.EM,
                    IsLocked = it.isLocked,
                    ImagePath = paths.Path,
                    PlaceholderPath = paths.Placeholder,
                    HasImage = has
                });
            }
            return list;
        }
    }
}
