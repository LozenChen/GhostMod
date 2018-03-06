﻿using Celeste.Mod;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Detour;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Ghost.Net {
    public class GhostNetModule : EverestModule {

        public static GhostNetModule Instance;

        public GhostNetModule() {
            Instance = this;
        }

        public override Type SettingsType => typeof(GhostNetModuleSettings);
        public static GhostNetModuleSettings Settings => (GhostNetModuleSettings) Instance._Settings;

        public bool IsRunning { get; protected set; } = false;

        public override void Load() {
        }

        public override void Unload() {
        }

        public void Start() {
            if (IsRunning)
                Stop();
            IsRunning = true;

        }

        public void Stop() {
            IsRunning = false;

        }

    }
}
