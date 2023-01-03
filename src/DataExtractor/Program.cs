//  Copyright 2023 Jonguk Kim
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using reWZ;
using reWZ.WZProperties;

namespace DataExtractor
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var mapDirectory = new DirectoryInfo("C:\\Nexon\\Maple\\Data\\Map\\Map");
            var mapDirectories = mapDirectory.GetDirectories("Map*", SearchOption.TopDirectoryOnly);
            var maps = new Dictionary<int, MapData>();

            using (var stringWz = new WZFile("C:\\Nexon\\Maple\\Data\\String\\String_000.wz", WZVariant.BMS, false, WZReadSelection.EagerParseStrings))
            {
                foreach (var category in stringWz.MainDirectory["Map.img"])
                {
                    foreach (var map in category)
                    {
                        if (!int.TryParse(map.Name, out var id)) continue;

                        var mapData = new MapData
                        {
                            Id = id,
                            MapName = map.Get("mapName", ""),
                            StreetName = map.Get("streetName", "")
                        };

                        if (!maps.ContainsKey(id))
                        {
                            maps.Add(id, mapData);
                        }
                    }
                }
            }

            foreach (var directory in mapDirectories)
            {
                foreach (var file in directory.GetFiles("*.wz").Where(x => x.Length >= 1024))
                {
                    using (var wz = new WZFile(file.FullName, WZVariant.BMS, false, WZReadSelection.EagerParseStrings))
                    {
                        foreach (var map in wz.MainDirectory)
                        {
                            if (!int.TryParse(map.Name.Remove(9, 4), out var id)) continue;
                            if (!maps.ContainsKey(id)) continue;
                            if (!map.IsPresent("info/mapMark")) continue;

                            maps[id].MapMark = map.Get("info/mapMark", "");
                        }
                    }
                }
            }

            File.WriteAllText("./data.json", JsonConvert.SerializeObject(maps, Formatting.Indented));
        }

        private static T Get<T>(this WZObject wzObject, string path, T defaultValue)
        {
            try
            {
                return wzObject.ResolvePath(path).ValueOrDefault(defaultValue);
            }
            catch
            {
                return defaultValue;
            }
        }

        private static bool IsPresent(this WZObject wzObject, string path)
        {
            try
            {
                return wzObject.ResolvePath(path) != null;
            }
            catch
            {
                return false;
            }
        }
    }
}