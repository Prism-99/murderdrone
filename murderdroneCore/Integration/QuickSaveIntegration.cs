using MURDERDRONE;
using QuickSave.API;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace murderdroneCore.Integration
{
    internal class QuickSaveIntegration
    {
        private static IModHelper _helper;
        private static Action _presave;
        public static  void Initialize(IModHelper helper, Action PreSave)
        {
            //config = oconfig;
            //manifest = omanifest;
            _helper = helper;
            _presave = PreSave;
            helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;
        }
        public static void GameLoop_GameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            if (!_helper.ModRegistry.IsLoaded("DLX.QuickSave"))
                return;

            var api = _helper.ModRegistry.GetApi<IQuickSaveAPI>("DLX.QuickSave");
            api.SavingEvent += Api_SavingEvent;

        }
        private static void Api_SavingEvent(object sender, ISavingEventArgs e)
        {
            _presave();
        }

    }
}
