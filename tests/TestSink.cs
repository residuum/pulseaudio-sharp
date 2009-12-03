//
//  Copyright © 2009 Christopher James Halse Rogers <raof@ubuntu.com>
//
//  TestSink.cs is a part of Pulseaudio#
//
//  Pulseaudio# is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  Pulseaudio# is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with Pulseaudio#.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;

using g = GLib;
using Pulseaudio.GLib;

namespace Pulseaudio
{


    [TestFixture()]
    public class TestSink
    {

        private void RunUntilEventSignal (Action action, EventWaitHandle until, string timeoutMessage)
        {
            var timeout = new EventWaitHandle (false, EventResetMode.AutoReset);
            g::Timeout.Add (1000, () =>
            {
                timeout.Set ();
                return false;
            });
            action ();
            while (!until.WaitOne (0, true)) {
                g::MainContext.Iteration (false);
                if (timeout.WaitOne (0, true)) {
                    Assert.Fail (timeoutMessage);
                }
            }
        }

        [Test()]
        public void TestGetName ()
        {
            Context c = new Context ();
            c.ConnectAndWait ();
            var sinks = new List<Sink> ();
            using (Operation o = c.EnumerateSinks ((Sink s) => sinks.Add (s))) {
                o.Wait ();
            }
            Assert.Contains ("Internal Audio Analog Stereo", (from sink in sinks select sink.Description).ToList ());
        }

        [Test()]
        public void TestVolumeChangedCallbackRuns ()
        {
            Context c = new Context ();
            c.ConnectAndWait ();

            ManualResetEvent callbackTriggered = new ManualResetEvent (false);

            var sinks = new List<Sink> ();
            using (Operation o = c.EnumerateSinks ((Sink sink) => sinks.Add (sink))) {
                o.Wait ();
            }
            sinks[0].VolumeChanged += (_, __) => {
                callbackTriggered.Set ();
            };
            Volume oldVol = new Volume ();
            Volume newVol;
            using (Operation o = sinks[0].GetVolume ((Volume v) => oldVol = v)) {
                o.Wait ();
            }
            newVol = oldVol.Copy ();
            newVol.Set (0);
            using (Operation o = sinks[0].SetVolume (newVol, (_) => {;})) {
                o.Wait ();
            }
            RunUntilEventSignal (() => {;}, callbackTriggered, "Timeout waiting for VolumeChanged signal");
            using (Operation o = sinks[0].SetVolume (oldVol, (_) => {;})) {
                o.Wait ();
            }
        }
    }
}