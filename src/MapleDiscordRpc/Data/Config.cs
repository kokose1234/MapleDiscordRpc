//  Copyright 2022 Jonguk Kim
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

using Newtonsoft.Json;

namespace MapleDiscordRpc.Data;

[JsonObject(MemberSerialization.OptIn, ItemNullValueHandling = NullValueHandling.Ignore)]
public sealed record Config
{
    [JsonProperty("mapleStoryPath")]
    public string MapleStoryPath { get; init; } = "C:\\Nexon\\Maple"; // Default Path

    [JsonProperty("startMinimized")]
    public bool StartMinimized { get; init; }

    [JsonProperty("showCharacterName")]
    public bool ShowCharacterName { get; init; }

    [JsonProperty("showMapName")]
    public bool ShowMapName { get; init; } = true;

    [JsonProperty("showWorldName")]
    public bool ShowWorldName { get; init; }

    [JsonProperty("showChannel")]
    public bool ShowChannel { get; init; }

    [JsonProperty("showMapleGG")]
    public bool ShowMapleGG { get; init; } = true;
}