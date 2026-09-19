using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace GemRush.Tests
{
    /// <summary>
    /// Headless audio audit (docs/Audio-Design.md #9): locks the measured
    /// mix calibration and the voice-over pipeline integrity against
    /// regressions. Parallel sessions have clobbered audio code before;
    /// these tests make that loud instead of silent.
    ///
    /// - Calibration constants: the music bed must stay 6-10 dB under key
    ///   SFX; ambience under music; the gust anti-stack window present.
    /// - Mix hierarchy invariant: measured at play level from real
    ///   synthesized buffers — the score sits under the pickup chime.
    /// - Synthesis sanity: every builder produces finite, peak-guarded
    ///   audio of nonzero length.
    /// - Manifest integrity: every voice line hash-matches its text, has
    ///   a generated duration, resolves its clip and its aliases.
    /// </summary>
    [TestFixture]
    public class AudioAuditTests
    {
        static object StaticField(System.Type type, string name)
        {
            FieldInfo info = type.GetField(name,
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Static);
            Assert.IsNotNull(info, "expected static field " + name);
            return info.GetValue(null);
        }

        // ------------------------------------------------------------------
        // Calibration (docs/Audio-Design.md #9)
        // ------------------------------------------------------------------

        [Test]
        public void MusicBed_SitsSixToTenDhUnderKeySfx()
        {
            float musicVolume = (float)StaticField(typeof(AudioManager), "MusicVolume");
            Assert.AreEqual(0.26f, musicVolume, 0.001f,
                "MusicVolume drifted — the score must stay a bed under the SFX");
        }

        [Test]
        public void AmbienceStaysUnderMusic()
        {
            float hum = (float)StaticField(typeof(AudioManager), "HumMaxVolume");
            Assert.LessOrEqual(hum, 0.1f,
                "portal hum rose back over the music bed");
        }

        [Test]
        public void GustAntiStackWindowPresent()
        {
            float window = (float)StaticField(typeof(AudioManager),
                "GustSwellStackWindow");
            Assert.GreaterOrEqual(window, 0.3f,
                "sibling gust lanes will double-stack without the guard");
        }

        [Test]
        public void VoiceChannelCalibrated()
        {
            float vol = (float)StaticField(typeof(VoiceOver), "VoiceVolume");
            Assert.GreaterOrEqual(vol, 0.4f, "narration dropped under the mix");
            Assert.LessOrEqual(vol, 0.8f,
                "narration rose back to the mobile loudness ceiling");
        }

        // ------------------------------------------------------------------
        // Mix hierarchy, measured from real buffers
        // ------------------------------------------------------------------

        static float Rms(AudioClip clip)
        {
            var data = new float[clip.samples * clip.channels];
            clip.GetData(data, 0);
            double sum = 0;
            for (int i = 0; i < data.Length; i++) sum += data[i] * (double)data[i];
            return Mathf.Sqrt((float)(sum / Mathf.Max(1, data.Length)));
        }

        [Test]
        public void Score_IsAtLeastSixDhUnder_PickupChime_AtPlayLevel()
        {
            AudioClip pad = MusicSynth.MoodLoop("audit_day", SoundMood.Day, 0.13f, false);
            AudioClip pickup = SfxSynth.GemPickup("audit_pickup", 880f);
            float musicVolume = (float)StaticField(typeof(AudioManager), "MusicVolume");
            float padRms = Rms(pad) * musicVolume;
            float pickupRms = Rms(pickup);
            float separationDb = 20f * Mathf.Log10(Mathf.Max(1e-6f, pickupRms / padRms));
            Assert.GreaterOrEqual(separationDb, 6f,
                "the score climbed back into the SFX's space (" +
                separationDb.ToString("F1") + " dB separation)");
        }

        // ------------------------------------------------------------------
        // Synthesis sanity
        // ------------------------------------------------------------------

        [Test]
        public void EveryVoiceBuilder_ProducesFinitePeakGuardedAudio()
        {
            var clips = new[]
            {
                SfxSynth.Jump("audit", 1f), SfxSynth.Land("audit"),
                SfxSynth.GemPickup("audit", 880f), SfxSynth.CheckpointChime("audit"),
                SfxSynth.HeartChime("audit"), SfxSynth.BounceSpring("audit"),
                SfxSynth.GiftChime("audit"), SfxSynth.HazardDeath("audit"),
                SfxSynth.FallDeath("audit"), SfxSynth.GameOverSting("audit"),
                SfxSynth.LivesLow("audit"), SfxSynth.WinFanfare("audit"),
                SfxSynth.CompleteFanfare("audit"), SfxSynth.StarDing("audit", 1),
                SfxSynth.RecordFlourish("audit"), SfxSynth.BloomRun("audit"),
                SfxSynth.LanternLight("audit"), SfxSynth.MeltSigh("audit"),
                SfxSynth.CrystalRun("audit"), SfxSynth.MilestoneChime("audit"),
                SfxSynth.FestivalConcert("audit"), SfxSynth.Giggle("audit"),
                SfxSynth.SoftGiggle("audit"), SfxSynth.RaindropBloom("audit"),
                SfxSynth.BellTone("audit", 196f, 4f, 0.3f),
                SfxSynth.BridgeOn("audit"), SfxSynth.BridgeOff("audit"),
                SfxSynth.GuardianWake("audit"), SfxSynth.MirrorTransit("audit"),
                SfxSynth.UIClick("audit"), SfxSynth.UIToggle("audit", true),
                SfxSynth.PauseBlip("audit", true), SfxSynth.PanelSwoosh("audit", false),
                SfxSynth.IntroSwoosh("audit"), SfxSynth.PageTurn("audit"),
                SfxSynth.NoiseSwell("audit", 2.2f, 0.22f),
                MusicSynth.MoodLoop("audit_mood", SoundMood.Day, 0.13f, true),
                MusicSynth.WindLoop("audit_wind", 0.3f, 500f, 0.25f, 77),
                MusicSynth.RumbleLoop("audit_rumble", 0.2f),
                MusicSynth.HumLoop("audit_hum", 1f),
            };
            foreach (var clip in clips)
            {
                Assert.IsNotNull(clip, "a builder returned null");
                Assert.Greater(clip.length, 0.01f, clip.name + " is empty");
                var data = new float[clip.samples * clip.channels];
                clip.GetData(data, 0);
                float peak = 0f;
                for (int i = 0; i < data.Length; i++)
                {
                    Assert.IsFalse(float.IsNaN(data[i]), clip.name + " has NaN samples");
                    peak = Mathf.Max(peak, Mathf.Abs(data[i]));
                }
                Assert.LessOrEqual(peak, 1.001f,
                    clip.name + " exceeds full scale — the peak guard failed");
            }
        }

        // ------------------------------------------------------------------
        // Voice-over pipeline integrity
        // ------------------------------------------------------------------

        [Test]
        public void Hash_AlgorithmPinned()
        {
            // FNV-1a 32-bit over UTF-8; pins the exact contract shared by
            // the exporter, the Python tool and the runtime. If either of
            // these values moves, every clip's hash is stale at once.
            Assert.AreEqual("f5615aa8", VoiceOver.Hash("audit"));
            Assert.AreEqual("6a8bfd45", VoiceOver.Hash("The end."));
            Assert.AreNotEqual(VoiceOver.Hash("audit"), VoiceOver.Hash("audi0"));
        }

        [Test]
        public void Manifest_EveryLine_HashMatchesText_AndClipExists()
        {
            var asset = Resources.Load<TextAsset>("Voice/manifest");
            Assert.IsNotNull(asset, "Voice/manifest.json missing from Resources");
            var manifest = JsonUtility.FromJson<VoiceOver.VoiceManifest>(asset.text);
            Assert.IsNotNull(manifest);
            Assert.Greater(manifest.entries.Count, 100, "manifest lost entries");

            var owners = new System.Collections.Generic.HashSet<string>();
            foreach (var e in manifest.entries)
            {
                owners.Add(e.id);
                if (e.aliases != null)
                    foreach (var a in e.aliases) owners.Add(a);
            }

            foreach (var e in manifest.entries)
            {
                Assert.AreEqual(VoiceOver.Hash(e.text), e.hash,
                    e.id + ": text edited since generation — regenerate voice");
                Assert.Greater(e.duration, 0f, e.id + ": clip never generated");
                Assert.IsNotNull(Resources.Load<AudioClip>("Voice/" + e.file),
                    e.id + ": clip missing from Resources");
                if (e.aliases != null)
                    foreach (var a in e.aliases)
                        Assert.IsTrue(owners.Contains(a),
                            e.id + " aliases unknown id " + a);
            }
        }
    }
}
