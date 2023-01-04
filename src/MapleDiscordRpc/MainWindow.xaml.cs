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
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Controls;
using MapleDiscordRpc.Data;
using ReactiveUI;

namespace MapleDiscordRpc
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new MainViewModel();

            if (Config.Value.StartMinimized) WindowState = WindowState.Minimized;

            this.WhenActivated(disposable =>
            {
                this.OneWayBind(ViewModel, vm => vm.NetworkDeviceList, v => v.NetworkDeviceList.ItemsSource)
                    .DisposeWith(disposable);
                this.OneWayBind(ViewModel, vm => vm.SelectedNetworkDevice, v => v.NetworkDeviceList.SelectedIndex)
                    .DisposeWith(disposable);

                this.Bind(ViewModel, vm => vm.StartMinimized, v => v.StartMinimized.IsChecked)
                    .DisposeWith(disposable);
                this.Bind(ViewModel, vm => vm.ShowCharacterName, v => v.ShowCharacter.IsChecked)
                    .DisposeWith(disposable);
                this.Bind(ViewModel, vm => vm.ShowMap, v => v.ShowMap.IsChecked)
                    .DisposeWith(disposable);
                this.Bind(ViewModel, vm => vm.ShowChannel, v => v.ShowChannel.IsChecked)
                    .DisposeWith(disposable);
                this.Bind(ViewModel, vm => vm.ShowMapleGG, v => v.ShowGG.IsChecked)
                    .DisposeWith(disposable);
            });

            NetworkDeviceList.Events().PreviewMouseDown.Subscribe(_ =>
            {
                Config.Value.NetworkDevice = NetworkDeviceList.Text;
                Config.Save();
            });
            SaveButton.Events().PreviewMouseDown.Subscribe(_ =>
            {
                Config.Value.NetworkDevice = NetworkDeviceList.Text;
                Config.Value.StartMinimized = StartMinimized.IsChecked ?? false;
                Config.Value.ShowCharacterName = ShowCharacter.IsChecked ?? false;
                Config.Value.ShowMap = ShowMap.IsChecked ?? false;
                Config.Value.ShowChannel = ShowChannel.IsChecked ?? false;
                Config.Value.ShowMapleGG = ShowGG.IsChecked ?? false;
                Config.Save();
            });
            HelpButton.Events().PreviewMouseDown.Subscribe(_ => Process.Start("explorer.exe", "https://github.com/kokose1234/MapleDiscordRpc/blob/main/README.md"));
        }
    }
}