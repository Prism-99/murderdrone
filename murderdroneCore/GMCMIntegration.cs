using MURDERDRONE;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using GenericModConfigMenu;
using System;

namespace murderdroneCore
{
    internal static class GMCMIntegration
    {
        private static ModConfig config;
        private static IManifest manifest;
        private static IModHelper helper;
        private static void ResetValues()
        {
            config.Active = true;
            config.KeyboardShortcut = "F7";
            config.RotationSpeed = 10;
            config.Damage = -1;
            config.ProjectileVelocity = 16;
            config.Keybind = SButton.F7;
            config.DroneRadius = 120;
        }
        public static void Initialize(IModHelper ohelper, IManifest omanifest, ModConfig oconfig)
        {
            config = oconfig;
            manifest = omanifest;
            helper = ohelper;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }
        private static void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;


            // register mod
            configMenu.Register(
                mod: manifest,
                reset: ResetValues,
                save: () => helper.WriteConfig(config),
                 titleScreenOnly: false
            );
            //
            //  create config GUI
            //
            //  Realty Options
            //
            configMenu.AddSectionTitle(
                mod: manifest,
                text: () => "Combat Drone Options",
                tooltip: () => ""
            );

            configMenu.AddKeybind(
                 mod: manifest,
                 name: () => "Activation Key",
                 tooltip: () => "",
                  getValue: () => config.Keybind,
                  setValue: value => config.Keybind = value

              );
            configMenu.AddBoolOption(
                mod: manifest,
                name: () => "Active At Start of Day",
                tooltip: () => "",
                getValue: () => config.Active,
                setValue: value => config.Active = value
                );

            configMenu.AddNumberOption(
                 mod: manifest,
                 name: () => "Rotation Speed",
                 tooltip: () => "The lower the number, the faster the drone",
                 getValue: () => config.RotationSpeed,
                 setValue: value => config.RotationSpeed = Math.Max(value, 1)
             );

            configMenu.AddNumberOption(
                 mod: manifest,
                 name: () => "Rotation Radius",
                 tooltip: () => "The larger the number, the bigger the flying circle",
                 getValue: () => config.DroneRadius,
                 setValue: value => config.DroneRadius = value
             );
            configMenu.AddNumberOption(
                 mod: manifest,
                 name: () => "Damage",
                 tooltip: () => "",
                 getValue: () => config.Damage,
                 setValue: value => config.Damage = value
             );

            configMenu.AddNumberOption(
                  mod: manifest,
                  name: () => "Projectile Velocity",
                  tooltip: () => "",
                  getValue: () => config.ProjectileVelocity,
                  setValue: value => config.ProjectileVelocity = value
              );
        }
    }
}
