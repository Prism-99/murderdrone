using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Projectiles;
using StardewValley.Monsters;
using StardewModdingAPI;
using Netcode;
using StardewValley.Tools;
using StardewValley.Extensions;

namespace MURDERDRONE
{
    public class Drone : NPC
    {
        //private readonly float r = 80f;
        private float t;
        private readonly float offsetY = 20f;
        private readonly float offsetX = 5f;
        private int droneSpeed;
        private bool throwing;
        private bool thrown;
        private long playerId;
        private Monster? target = null;
        private BasicProjectile? basicProjectile = null;
        private int damage;
        private readonly float projectileVelocity;
        private readonly IModHelper? helper = null;
        private float dRadius = 80f;
        private bool isRemote = false;
        public Drone()
        : base(new AnimatedSprite("Sidekick/Drone", 1, 12, 12), Game1.player.Position, 1, "Drone")
        {
            droneSpeed = 1;
            hideShadow.Value = true;
            damage = -1;
            projectileVelocity = 16;
            helper = null;
            isRemote = true;
        }

        public Drone(int speed, int damage, float projectileVelocity, IModHelper helper, string sDroneName, long playerId, float droneRadius = 80f)
        : base(new AnimatedSprite("Sidekick/Drone", 1, 12, 12), Game1.player.Position, 1, sDroneName)
        {
            droneSpeed = speed;
            this.playerId = playerId;
            hideShadow.Value = true;
            this.damage = damage;
            this.projectileVelocity = projectileVelocity;
            this.helper = helper;
            Name = sDroneName;
            dRadius = droneRadius;
            StashSettings();
        }
        private void StashSettings()
        {
            modData.Add("mdrone.speed", droneSpeed.ToString());
            modData.Add("mdrone.playerId", playerId.ToString());
            modData.Add("mdrone.radius", dRadius.ToString());
            modData.Add("mdrone.damage", damage.ToString());
            modData.Add("mdrone.name", name.Value);
        }
        private void RetrieveSettings()
        {
            if (modData.TryGetValue("mdrone.name", out string dname))
                Name = dname;

            if (modData.TryGetValue("mdrone.radius", out string remRadius))
            {
                if (float.TryParse(remRadius, out float rad))
                    dRadius = rad;
            }
            if (modData.TryGetValue("mdrone.speed", out string mspeed))
            {
                if (int.TryParse(mspeed, out int intSpeed))
                    droneSpeed = intSpeed;
            }
            if (modData.TryGetValue("mdrone.damage", out string rDamage))
            {
                if (int.TryParse(rDamage, out int remDamage))
                    damage = remDamage;
            }
            if (modData.TryGetValue("mdrone.playerId", out string pid))
            {
                if (long.TryParse(mspeed, out long remoteplayer))
                    playerId = remoteplayer;
            }
        }
        public override bool CanSocialize => false;

        public override bool canTalk() => false;

        public override void doEmote(int whichEmote, bool playSound, bool nextEventCommand = true)
        {
        }

        public override void update(GameTime time, GameLocation location)
        {
            //if (playerId != Game1.player.uniqueMultiplayerID.Value)
            //    return;
            if (isRemote)
            {
                RetrieveSettings();
                isRemote = false;
            }

            Farmer droneOwner = Game1.player;// Game1.getFarmer(playerId);
            if (droneOwner != null && droneOwner.UniqueMultiplayerID == playerId)
            {
                float newX = droneOwner.position.X + offsetX + dRadius * (float)Math.Cos(t * 2 * Math.PI);
                float newY = droneOwner.position.Y - offsetY + dRadius * (float)Math.Sin(t * 2 * Math.PI);
                position.Set(new Vector2(newX, newY));

                t = (t + (float)time.ElapsedGameTime.TotalMilliseconds / (100 * droneSpeed)) % 1;

                if (!throwing)
                {
                    for (int range = 1; range < 5; range++)
                    {
                        var monsters = Game1.currentLocation.characters.Where(p => p is Monster mon && mon.withinPlayerThreshold(range));
                        if (monsters.Any())
                        {
                            throwing = true;
                            target = (Monster)monsters.First();
                        }
                    }
                }
                if (throwing && (target?.IsMonster ?? false))
                {
#if DEBUG
                    ModEntry.monitor.Log($"*** Firing at monster {target.Name}  ***", LogLevel.Debug);
#endif
                    ShootTheBastard(time, location, target);
                }
                if (thrown && basicProjectile is BasicProjectile && (basicProjectile.destroyMe || basicProjectile.travelDistance >= basicProjectile.maxTravelDistance.Value))
                {
                    thrown = false;
                    basicProjectile = null;
                }
            }
        }

        public override void draw(SpriteBatch b)
        {
            if (currentLocation != null && currentLocation == Game1.player.currentLocation)
                base.draw(b);
        }

        public virtual void ShootTheBastard(GameTime time, GameLocation location, Monster monster)
        {
            if (!thrown)
            {
                if (damage == -1)
                {
                    damage = monster.Health;
                }

                BasicProjectile.onCollisionBehavior collisionBehavior = new BasicProjectile.onCollisionBehavior(
                    delegate (GameLocation loc, int x, int y, Character who)
                    {
                        Tool? currentTool = null;

                        if (Game1.player.CurrentTool != null && Game1.player.CurrentTool is Tool)
                            currentTool = Game1.player.CurrentTool;

                        if (monster is Bug bug && bug.isArmoredBug.Value)
                            helper?.Reflection.GetField<NetBool>(bug, "isArmoredBug").SetValue(new NetBool(false));

                        if (monster is RockCrab rockCrab)
                        {
#if v16
                            if (Game1.player.CurrentTool != null && Game1.player.CurrentTool is Tool && currentTool != null && Game1.player.CurrentTool is Pickaxe)
                                Game1.player.CurrentTool = new MeleeWeapon("4");
#else
                            if (Game1.player.CurrentTool != null && Game1.player.CurrentTool is Tool && currentTool != null && Game1.player.CurrentTool is Pickaxe)
                                Game1.player.CurrentTool = new MeleeWeapon(4);
#endif

                            helper?.Reflection.GetField<NetBool>(rockCrab, "shellGone").SetValue(new NetBool(true));
                            helper?.Reflection.GetField<NetInt>(rockCrab, "shellHealth").SetValue(new NetInt(0));
                        }

                        loc.damageMonster(monster.GetBoundingBox(), damage, damage + 1, true, !(who is Farmer) ? Game1.player : who as Farmer);

                        if (Game1.player.CurrentTool != null && Game1.player.CurrentTool is Tool && currentTool != null)
                            Game1.player.CurrentTool = currentTool;
                    }
                );

                string collisionSound = "hitEnemy";

                Vector2 velocityTowardMonster = Utility.getVelocityTowardPoint(Position, monster.Position, projectileVelocity);
#if v16
                basicProjectile = new BasicProjectile(
                   damage,
                   Projectile.shadowBall,
                   0,
                   0,
                   0,
                   velocityTowardMonster.X,
                   velocityTowardMonster.Y,
                   position.Value,
                   collisionSound,
                   firingSound: "daggerswipe",
                   explode: false,
                   damagesMonsters: true,
                   location: location,
                   firer: this,
                 collisionBehavior: collisionBehavior
               )
                {
                    IgnoreLocationCollision = (Game1.currentLocation.currentEvent != null)

                };
                basicProjectile.maxTravelDistance.Value = 200;
#else
                basicProjectile = new BasicProjectile(
                    damage,
                    Projectile.shadowBall,
                    0,
                    0,
                    0,
                    velocityTowardMonster.X,
                    velocityTowardMonster.Y,
                    position.Value,
                    collisionSound,
                    firingSound: "daggerswipe",
                    explode: false,
                    damagesMonsters: true,
                    location: location,
                    firer: this,
                    false,
                    collisionBehavior
                )
                {
                    IgnoreLocationCollision = (Game1.currentLocation.currentEvent != null)
                };
#endif
                location.projectiles.Add(basicProjectile);

                thrown = true;
                throwing = false;
            }
        }
    }
}