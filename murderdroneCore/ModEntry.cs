using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using Microsoft.Xna.Framework.Graphics;
using murderdroneCore;
using StardewValley.Buildings;

namespace MURDERDRONE
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        private ModConfig Config;
        private string sDroneName = "";
        private bool DroneLoaded = false;
        private bool LoadDroneAfterSave;
        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            Config = Helper.ReadConfig<ModConfig>();
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
            helper.Events.GameLoop.DayStarted += GameLoop_DayStarted;
            //
            //  added for SMAPI 4
            //
            helper.Events.Content.AssetRequested += OnAssetRequested;
        }

        private void GameLoop_DayStarted(object? sender, DayStartedEventArgs e)
        {
            DroneLoaded = false;
            LoadDroneAfterSave = LoadDroneAfterSave || Config.Active;
            if (LoadDroneAfterSave)
            {
                AddDrone();
            }
        }


        //
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
        //  added for SMAPI 4.0.0 compatability
        //
        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.Name.IsEquivalentTo("Sidekick/Drone")|| e.Name.StartsWith("Portraits/Drone_"))
            {
                e.LoadFromModFile<Texture2D>("Assets/drone_sprite_robot.png", AssetLoadPriority.Medium);
            }
        }

        /*********
        ** Private methods
        *********/
        private void GameLoop_SaveCreating(object? sender, SaveCreatingEventArgs e)
        {
            LoadDroneAfterSave = DroneLoaded;
            RemoveDrone();
        }
        private void GameLoop_Saving(object? sender, SavingEventArgs e)
        {
            LoadDroneAfterSave = DroneLoaded;
            RemoveDrone();
        }
        private void GameLoop_Saved(object? sender, SavedEventArgs e)
        {
        }
        private void GameLoop_SaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            sDroneName = "Drone_" + Game1.player.name.Value;
            LoadDroneAfterSave = Config.Active;
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
            if (!LoadDroneAfterSave &&( !e.IsLocalPlayer || Game1.CurrentEvent != null || !DroneLoaded))
                return;
            RemoveDrone(false);
            AddDrone();
        }

        private void RemoveDrone(bool updateLoadedStatus = true)
        {
            if (Game1.getCharacterFromName(sDroneName) is NPC drone)
            {
                if(Game1.currentLocation!=null && Game1.currentLocation.characters.Contains(drone))
                {
                    Game1.currentLocation.characters.Remove(drone);
                }
                //
                //  remove the drone from game locations
                //
                for (int i = 0; i < Game1.locations.Count; i++)
                {
                    if (Game1.locations[i].characters.Contains(drone))
                    {
                        Game1.locations[i].characters.Remove(drone);
                    }
#if v16
                    if (Game1.locations[i].IsBuildableLocation())
                    {
                        GameLocation gl = Game1.locations[i];
#else
                    if (Game1.locations[i] is BuildableGameLocation gl)
                    {
#endif
                        //
                        //  remove the drone from building indoors
                        //
                        foreach (Building bl in gl.buildings)
                        {
                            if (bl.indoors.Value != null)
                            {
                                if (bl.indoors.Value.characters.Contains(drone))
                                {
                                    bl.indoors.Value.characters.Remove(drone);
                                }
                            }
                        }

                    }
                }
            }
            if (updateLoadedStatus)
                DroneLoaded = false;
        }

        private void AddDrone()
        {
            if (Game1.currentLocation == null || Game1.currentLocation is DecoratableLocation)
                return;

            if (!DroneLoaded || Game1.getCharacterFromName(sDroneName) == null)
            {
                Game1.currentLocation.addCharacter(new Drone(Config.RotationSpeed, Config.Damage, Config.ProjectileVelocity, Helper, sDroneName,Config.DroneRadius));
                DroneLoaded = true;
            }
            else
                Game1.warpCharacter(Game1.getCharacterFromName(sDroneName), Game1.currentLocation, Game1.player.Position);
            
            LoadDroneAfterSave = false;
        }
    }
}