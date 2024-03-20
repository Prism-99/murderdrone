using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using System;
namespace MURDERDRONE
{
    public class ModConfig
    {
        public bool Active { get; set; }
        public string KeyboardShortcut { get; set; }
        public SButton Keybind { get; set; }
        public int RotationSpeed { get; set; }
        public float DroneRadius { get; set; }
        public int Damage { get; set; }
        public int ProjectileVelocity { get; set; }

        public ModConfig()
        {
            this.Active = true;
            this.KeyboardShortcut = "F7";
            this.RotationSpeed = 10;
            this.Damage = -1;
            this.ProjectileVelocity = 16;
            DroneRadius = 120f;
        }
    }
}
