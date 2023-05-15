using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using Microsoft.Xna.Framework.Graphics;
using murderdroneCore;

namespace MURDERDRONE
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        private ModConfig Config;
        private string sDroneName = "";
        private bool DroneLoaded = false;
        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            Config = Helper.ReadConfig<ModConfig>();
            DroneLoaded = Config.Active;
            GMCMIntegration.Initialize(helper, ModManifest, Config);

            if (Config.Keybind == SButton.None)
            {
                //
                //  old config try to upgrade
                //
                if (Config.KeyboardShortcut == "F7")
                {
                    Config.Keybind = SButton.F7;
                }
            }

            helper.Events.GameLoop.SaveCreating += GameLoop_SaveCreating;
            helper.Events.GameLoop.Saving += GameLoop_Saving;
            helper.Events.Input.ButtonPressed += Input_ButtonPressed;
            helper.Events.GameLoop.SaveLoaded += GameLoop_SaveLoaded;
            helper.Events.Player.Warped += PlayerEvents_Warped;
            helper.Events.GameLoop.Saved += GameLoop_Saved;
            //
            //  added for SMAPI 4
            //
            helper.Events.Content.AssetRequested += OnAssetRequested;
        }



        // removed for SMAPI 4 migration
        //
        /// <summary>Get whether this instance can load the initial version of the given asset.</summary>
        /// <param name="asset">Basic metadata about the asset being loaded.</param>
        //public bool CanLoad<T>(IAssetInfo asset)
        //{
        //    return asset.AssetNameEquals("Sidekick/Drone");
        //}
        /// <summary>Load a matched asset.</summary>
        /// <param name="asset">Basic metadata about the asset being loaded.</param>
        //public T Load<T>(IAssetInfo asset)
        //{
        //    if (asset.AssetNameEquals("Sidekick/Drone"))
        //        return Helper.Content.Load<T>("Assets/drone_sprite_robot.png", ContentSource.ModFolder);

        //    throw new InvalidOperationException($"Unexpected asset '{asset.AssetName}'.");
        //}
        //
        //  added for SMAPI compatability
        //
        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.Name.IsEquivalentTo("Sidekick/Drone"))
            {
                e.LoadFromModFile<Texture2D>("Assets/drone_sprite_robot.png", AssetLoadPriority.Medium);
            }
        }

        /*********
        ** Private methods
        *********/
        private void GameLoop_SaveCreating(object? sender, SaveCreatingEventArgs e)
        {
            //
            //  just in case
            //
            Helper.WriteConfig(Config);
            RemoveDrone();
        }
        private void GameLoop_Saving(object? sender, SavingEventArgs e)
        {
            Helper.WriteConfig(Config);
            RemoveDrone();
        }
        private void GameLoop_Saved(object? sender, SavedEventArgs e)
        {
            if (Config.Active)
            {
                AddDrone();
            }
        }
        private void GameLoop_SaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            sDroneName = "Drone_" + Game1.player.name.Value;
        }


        private void Input_ButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsPlayerFree || Game1.currentMinigame != null)
                return;

            if (e.Button == Config.Keybind)
            {
                if (DroneLoaded)
                {
                    RemoveDrone();
                    DroneLoaded = false;
                    Game1.showRedMessage("Drone deactivated.");
                }
                else
                {
                    AddDrone();
                    DroneLoaded = true;
                    Game1.addHUDMessage(new HUDMessage("Drone activated.", 4));
                }

            }
        }

        /// <summary>
        /// The method called when the player warps.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void PlayerEvents_Warped(object? sender, WarpedEventArgs e)
        {
            if (!e.IsLocalPlayer || Game1.CurrentEvent != null || !DroneLoaded)
                return;
            RemoveDrone();
            AddDrone();
        }

        private void RemoveDrone()
        {
            if (Game1.getCharacterFromName(sDroneName) is NPC drone)
            {
                for (int i = 0; i < Game1.locations.Count; i++)
                {
                    if (Game1.locations[i].characters.Contains(drone))
                    {
                        Game1.locations[i].characters.Remove(drone);
                    }
                }
            }
            DroneLoaded = false;
        }

        private void AddDrone()
        {
            if (Game1.currentLocation == null || Game1.currentLocation is DecoratableLocation)
                return;

            if (!DroneLoaded)
            {
                Game1.currentLocation.addCharacter(new Drone(Config.RotationSpeed, Config.Damage, (float)Config.ProjectileVelocity, Helper, sDroneName));
                DroneLoaded = true;
            }
            else
                Game1.warpCharacter(Game1.getCharacterFromName(sDroneName), Game1.currentLocation, Game1.player.Position);
        }
    }
}