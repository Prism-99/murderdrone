using Microsoft.Xna.Framework.Graphics;
using murderdroneCore;
using murderdroneCore.Integration;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;

namespace MURDERDRONE
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        private ModConfig Config;
        private string droneName = "";
        private bool DroneLoaded = false;
        private bool LoadDroneAfterSave;
        public static IMonitor monitor;
       
        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            Config = Helper.ReadConfig<ModConfig>();
            monitor = Monitor;
            GMCMIntegration.Initialize(helper, ModManifest, Config);
            QuickSaveIntegration.Initialize(helper, HandlePreSave);

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
        //  added for SMAPI 4.0.0 compatability
        //
        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.Name.IsEquivalentTo("Sidekick/Drone") || e.Name.StartsWith("Portraits/Drone_"))
            {
                e.LoadFromModFile<Texture2D>("Assets/drone_sprite_robot.png", AssetLoadPriority.Medium);
            }
        }

        /*********
        ** Private methods
        *********/
        internal void HandlePreSave()
        {
            LoadDroneAfterSave = DroneLoaded;
            RemoveDrone(true);
        }
        private void GameLoop_SaveCreating(object? sender, SaveCreatingEventArgs e)
        {
            HandlePreSave();
        }
        private void GameLoop_Saving(object? sender, SavingEventArgs e)
        {
            LoadDroneAfterSave = DroneLoaded;
            RemoveDrone(true);
        }

        private void GameLoop_SaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            droneName = "Drone_" + Game1.player.uniqueMultiplayerID;
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
                    RemoveMyDrone(Game1.player.currentLocation);
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
            if (!LoadDroneAfterSave && (!e.IsLocalPlayer || Game1.CurrentEvent != null || !DroneLoaded))
                return;

            NPC drone = e.OldLocation.getCharacterFromName(droneName);
            if (drone != null)
            {
                e.OldLocation.characters.Remove(drone);
                //
                //  check for indoor location
                //
                if (e.OldLocation is not DecoratableLocation)
                    e.NewLocation.characters.Add(drone);
            }
            else
            {
                AddDrone();
            }
        }
        private bool RemoveAllDrones(GameLocation location)
        {
            foreach (var character in location.characters.ToList())
            {
                if (character.Name.StartsWith("Drone_"))
                {
                    location.characters.Remove(character);
                }
            }

            return true;
        }
        private bool RemoveMyDrone(GameLocation location)
        {
            foreach (var character in location.characters.ToList())
            {
                if (character.Name.Equals(droneName))
                {
                    location.characters.Remove(character);
                }
            }

            return true;
        }
        private void RemoveDrone(bool updateLoadedStatus = true)
        {
            Utility.ForEachLocation(RemoveAllDrones);

            if (updateLoadedStatus)
                DroneLoaded = false;
        }

        private void AddDrone()
        {
            if (Game1.currentLocation == null || Game1.currentLocation is DecoratableLocation)
                return;

            if (Game1.getCharacterFromName(droneName) == null)
            {
                Game1.currentLocation.addCharacter(new Drone(Config.RotationSpeed, Config.Damage, Config.ProjectileVelocity, Helper, droneName, Game1.player.uniqueMultiplayerID.Value, Config.DroneRadius));
                DroneLoaded = true;
            }
            else
                Game1.warpCharacter(Game1.getCharacterFromName(droneName), Game1.currentLocation, Game1.player.Position);

            LoadDroneAfterSave = false;
        }
    }
}