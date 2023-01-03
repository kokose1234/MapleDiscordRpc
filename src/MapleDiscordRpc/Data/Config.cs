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

using System.IO;
using Newtonsoft.Json;

namespace MapleDiscordRpc.Data;

[JsonObject(MemberSerialization.OptIn, ItemNullValueHandling = NullValueHandling.Ignore)]
public sealed class Config
{
    private static Config? s_config;

    public static Config Value
    {
        get => s_config ??= Load();
        set
        {
            s_config = value;
            Save();
        }
    }

    [JsonProperty("startMinimized")]
    public bool StartMinimized { get; set; }

    [JsonProperty("showCharacterName")]
    public bool ShowCharacterName { get; set; } = true;

    [JsonProperty("showMap")]
    public bool ShowMap { get; set; } = true;

    [JsonProperty("showChannel")]
    public bool ShowChannel { get; set; } = true;

    [JsonProperty("showMapleGG")]
    public bool ShowMapleGG { get; set; } = true;

    private static Config Load()
    {
        if (!File.Exists("./Config.json"))
        {
            s_config = new();
            Save();
        }

        return JsonConvert.DeserializeObject<Config>(File.ReadAllText("./Config.json")) ?? new();
    }

    private static void Save()
    {
        var json = JsonConvert.SerializeObject(s_config, Formatting.Indented);
        File.WriteAllText("./Config.json", json);
    }
}