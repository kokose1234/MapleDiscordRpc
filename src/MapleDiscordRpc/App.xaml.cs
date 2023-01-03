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
using System.Text;
using System.Windows.Forms;
using SharpPcap.LibPcap;

namespace MapleDiscordRpc
{
    public partial class App
    {
        public App()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

            try
            {
                if (LibPcapLiveDeviceList.Instance.Count == 0) throw new Exception();
            }
            catch
            {
                MessageBox.Show(null, "WinPcap을 설치해 주세요.", "MapleDiscordRpc", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Process.Start("http://www.winpcap.org/install/default.htm");
                Environment.Exit(0);
            }
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            //TODO: 로그
        }
    }
}