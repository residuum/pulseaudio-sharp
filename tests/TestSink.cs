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
        private Helper helper;
        [SetUp]
        public void SetUp ()
        {
            helper = new Helper ();
        }
        [TearDown]
        public void TearDown ()
        {
            helper.Dispose ();
            GC.Collect ();
        }

        [Test()]
        public void TestGetName ()
        {
            string sinkDescription = "Just some description string";
            helper.AddSink ("Test sink", sinkDescription);
            Context c = new Context ();
            c.ConnectAndWait ();
            var sinks = new List<Sink> ();
            using (Operation o = c.EnumerateSinks ((Sink s) => sinks.Add (s))) {
                o.Wait ();
            }
            Assert.Contains (sinkDescription, (from sink in sinks select sink.Description).ToList ());
        }

        [Test()]
        public void TestVolumeChangedCallbackRuns ()
        {
            string testSinkName = "VolumeChangeTestSink";
            helper.AddSink (testSinkName);
            Context c = new Context ();
            c.ConnectAndWait ();

            ManualResetEvent callbackTriggered = new ManualResetEvent (false);

            var sinks = new List<Sink> ();
            using (Operation o = c.EnumerateSinks ((Sink sink) => sinks.Add (sink))) {
                o.Wait ();
            }
            Sink volumeTestSink = sinks.First ((Sink s) => s.Name == testSinkName);
            volumeTestSink.VolumeChanged += (_, __) => {
                callbackTriggered.Set ();
            };
            Volume vol = new Volume ();
            using (Operation o = volumeTestSink.GetVolume ((Volume v) => vol = v)) {
                o.Wait ();
            }
            vol.Modify (0.1);
            using (Operation o = volumeTestSink.SetVolume (vol, (_) => {;})) {
                o.Wait ();
            }
            Helper.RunUntilEventSignal (() => {;}, callbackTriggered, "Timeout waiting for VolumeChanged signal");
        }

        [Test]
        public void DisposeUnhooksVolumeCallbacks ()
        {
            Context c = new Context ();
            c.ConnectAndWait ();

            helper.AddSink ("dispose_test_sink");

            var sinks = new List<Sink> ();
            using (Operation o = c.EnumerateSinks ((Sink sink) => sinks.Add (sink))) {
                o.Wait ();
            }
            Sink testSink = sinks.Where ((Sink s) => s.Name == "dispose_test_sink").First ();

            testSink.VolumeChanged += delegate(object sender, Sink.VolumeChangedEventArgs e) {
                Assert.Fail ("VolumeChanged callback run after Sink was disposed");
            };

            Helper.DrainEventLoop ();
            testSink.Dispose ();

            // Find the sink again...
            sinks.Clear ();
            using (Operation o = c.EnumerateSinks ((Sink sink) => sinks.Add (sink))) {
                o.Wait ();
            }
            testSink = sinks.Where ((Sink s) => s.Name == "dispose_test_sink").First ();
            Volume vol = new Volume ();
            using (Operation o = testSink.GetVolume (v => vol = v)) {
                o.Wait ();
            }
            vol.Modify (0.5);
            using (Operation o = testSink.SetVolume (vol, (o) => {;})) {
                o.Wait ();
            }
            //Flush the mainloop
            Helper.DrainEventLoop ();
        }

        [Test]
        public void VolumePropertyUpdatedWithVolumeChange ()
        {
            string testSinkName = "VolumeChangeTestSink";
            helper.AddSink (testSinkName);
            Context c = new Context ();
            c.ConnectAndWait ();

            var sinks = new List<Sink> ();
            using (Operation o = c.EnumerateSinks ((Sink sink) => sinks.Add (sink))) {
                o.Wait ();
            }
            Sink volumeTestSink = sinks.First ((Sink s) => s.Name == testSinkName);

            Volume vol = volumeTestSink.Volume;
            vol.Modify (0.1);
            Assert.IsFalse (vol.Equals (volumeTestSink.Volume));
            using (Operation o = volumeTestSink.SetVolume (vol, (_) => {;})) {
                o.Wait ();
            }

            Helper.DrainEventLoop ();
            // We need a little time to let the volume changed events bubble through.
            Thread.Sleep (100);
            Helper.DrainEventLoop ();
            Thread.Sleep (100);
            Helper.DrainEventLoop ();

            Assert.AreEqual (vol, volumeTestSink.Volume);
        }

        [Test]
        public void TestSinkPropertiesContainsDeviceClass ()
        {
            const string testSinkName = "TestSink";
            helper.AddSink (testSinkName);

            Context c = new Context ();
            c.ConnectAndWait ();

            Sink addedSink = null;
            using (Operation o = c.EnumerateSinks ((Sink s) => { if (s.Name == testSinkName) addedSink = s; })) {
                o.Wait ();
            }

            Assert.IsNotNull (addedSink, "Failed to find known sink.");

            Assert.AreEqual ("abstract", addedSink.Properties[Properties.DeviceClass]);
        }

        [Test]
        public void SinkChannelMapPropertyReturnsCopy ()
        {
            const string testSinkName = "TestSink";
            helper.AddSink (testSinkName);

            Context c = new Context ();
            c.ConnectAndWait ();

            Sink addedSink = null;
            using (Operation o = c.EnumerateSinks ((Sink s) => { if (s.Name == testSinkName) addedSink = s; })) {
                o.Wait ();
            }
            Assert.IsNotNull (addedSink);

            ChannelMap sinkMap = addedSink.ChannelMap;
            sinkMap.channels++;
            Assert.AreNotEqual (addedSink.ChannelMap.channels, sinkMap.channels);
        }

                [Test]
        public void SinkSampleMapPropertyReturnsCopy ()
        {
            const string testSinkName = "TestSink";
            helper.AddSink (testSinkName);

            Context c = new Context ();
            c.ConnectAndWait ();

            Sink addedSink = null;
            using (Operation o = c.EnumerateSinks ((Sink s) => { if (s.Name == testSinkName) addedSink = s; })) {
                o.Wait ();
            }
            Assert.IsNotNull (addedSink);

            SampleSpec sinkSample = addedSink.SampleSpec;
            sinkSample.rate++;
            Assert.AreNotEqual (addedSink.SampleSpec.rate, sinkSample.rate);
        }
    }
}
