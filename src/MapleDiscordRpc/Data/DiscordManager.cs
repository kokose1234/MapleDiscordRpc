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

using System;
using System.Collections.Generic;
using DiscordRPC;

namespace MapleDiscordRpc.Data;

public class DiscordManager
{
    #region Singleton

    private static readonly Lazy<DiscordManager> Lazy = new(() => new DiscordManager());

    public static DiscordManager Instance => Lazy.Value;

    #endregion

    private readonly DiscordRpcClient _client;
    private readonly Timestamps _timestamps = Timestamps.Now;
    private readonly Button _defaultButton = new() {Label = "플러그인 다운로드", Url = "https://github.com/kokose1234/MapleDiscordRpc"};

    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public int MapId { get; set; }
    public int ChannelId { get; set; }
    public short Job { get; set; }

    public DiscordManager()
    {
        _client = new("1058808860414062592");

#if DEBUG
        _client.Logger = new ConsoleLogger(LogLevel.Trace);
        _client.OnReady += (_, args) => Console.WriteLine("Received Ready from user {0}", args.User.Username);
        _client.OnPresenceUpdate += (_, args) => Console.WriteLine("Received Update! {0}", args.Presence);
#endif

        _client.Initialize();
        ResetPresence();
    }

    public void ResetPresence()
    {
        _client.SetPresence(new RichPresence
        {
            Timestamps = _timestamps,
            Details = "캐릭터 선택 대기중",
            Assets = new Assets
            {
                LargeImageKey = "logo"
            }
        });
    }

    public void UpdatePresence()
    {
        var map = MapleDataProvider.Instance.GetMapData(MapId);
        if (map != null)
        {
            var detailText = "";
            var buttons = new List<Button>();

            if (Config.Value.ShowChannel)
            {
                detailText += $"{GetChannelName()} ";
            }

            detailText += $"Lv.{Level} ";

            if (Config.Value.ShowCharacterName)
            {
                detailText += Name;
            }

            buttons.Add(_defaultButton);
            if (Config.Value is {ShowMapleGG: true, ShowCharacterName: true})
            {
                buttons.Add(new Button
                {
                    Label = "캐릭터 정보",
                    Url = $"https://maple.gg/u/{Uri.EscapeDataString(Name).ToUpper()}"
                });
            }

            buttons.Reverse();

            _client.SetPresence(new RichPresence
            {
                Timestamps = _timestamps,
                Details = detailText,
                State = Config.Value.ShowMap ? $"{map.StreetName} - {map.MapName}" : null,
                Assets = new Assets
                {
                    LargeImageKey = MapleDataProvider.Instance.GetJobImageKey(Job),
                    LargeImageText = MapleDataProvider.Instance.GetJobName(Job),
                    SmallImageKey = Config.Value.ShowMap ? map.MapMark.ToLower() : "logo",
                    SmallImageText = Config.Value.ShowMap ? map.MapName : null,
                },
                Buttons = buttons.ToArray()
            });
        }
    }

    private string GetChannelName()
    {
        return ChannelId switch
        {
            0 => "CH.1",
            1 => "CH.20세",
            _ => $"CH.{ChannelId}"
        };
    }
}