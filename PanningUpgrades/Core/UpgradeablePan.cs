using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Netcode;
using BirbShared;
using StardewValley.Objects;

namespace PanningUpgrades
{
    [XmlType("Mods_drbirbmd_upgradeablepan")] // SpaceCore serialisation signature
    public class UpgradeablePan : Pan
    {

        public const int MaxUpgradeLevel = 4;
        private TemporaryAnimatedSprite tempSprite;

        public UpgradeablePan() : base()
        {
            base.UpgradeLevel = 0;
            base.InstantUse = false;
        }

        public UpgradeablePan(int upgradeLevel) : base()
        {
            base.UpgradeLevel = upgradeLevel;
            base.InstantUse = false;
            base.InitialParentTileIndex = -1;
            base.IndexOfMenuItemView = -1;
        }

        public override Item getOne()
        {
            UpgradeablePan result = new()
            {
                UpgradeLevel = base.UpgradeLevel
            };
            this.CopyEnchantments(this, result);
            result._GetOneFrom(this);
            return result;
        }

        protected override string loadDisplayName()
        {
            return ModEntry.Instance.I18n.Get("tool.orepan.name").ToString();
        }

        public static bool CanBeUpgraded()
        {
            Tool pan = Game1.player.getToolFromName("Pan");
            return pan is not null && pan.UpgradeLevel != MaxUpgradeLevel;
        }

        public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
        {
            spriteBatch.Draw(
                texture: ModEntry.Assets.Sprites,
                position: location + new Vector2(32f, 32f),
                sourceRectangle: IconSourceRectangle(upgradeLevel: this.UpgradeLevel),
                color: color * transparency,
                rotation: 0f,
                origin: new Vector2(8, 8),
                scale: Game1.pixelZoom * scaleSize,
                effects: SpriteEffects.None,
                layerDepth: layerDepth);
        }

        public override bool beginUsing(GameLocation location, int x, int y, Farmer who)
        {
            this.lastUser = who;
            who.jitterStrength = 0.25f;
            who.FarmerSprite.setCurrentFrame(123);
            // who.faceDirection(2);

            int genderOffset = who.IsMale ? -1 : 0;
            this.tempSprite = new TemporaryAnimatedSprite(
                textureName: ModEntry.Assets.SpritesPath,
                sourceRect: AnimationSourceRectangle(this.UpgradeLevel),
                animationInterval: 1000,
                animationLength: 1,
                numberOfLoops: 1000,
                position: Game1.player.Position + new Vector2(0f, (ModEntry.Config.AnimationYOffset + genderOffset) * 4),
                flicker: false,
                flipped: false,
                layerDepth: 1f,
                alphaFade: 0f,
                color: Color.White,
                scale: 4f,
                scaleChange: 0f,
                rotation: 0f,
                rotationChange: 0f);

            who.currentLocation.temporarySprites.Add(this.tempSprite);

            return false;
        }

        public override void endUsing(GameLocation location, Farmer who)
        {
            who.stopJittering();
            who.canReleaseTool = false;
            who.currentLocation.removeTemporarySpritesWithID(this.tempSprite.id);
            ((FarmerSprite)who.Sprite).animateOnce(303, 50f, 4);
        }

        public static Rectangle IconSourceRectangle(int upgradeLevel)
        {
            Rectangle source = new(0, 0, 16, 16);
            source.Y += upgradeLevel * source.Height;
            return source;
        }

        public static Rectangle AnimationSourceRectangle(int upgradeLevel)
        {
            Rectangle source = new(16, 0, 16, 16);
            source.Y += upgradeLevel * source.Height;
            return source;
        }

        // Since getPanItems isn't virtual, we need to copy the below methods from Pan.cs to wrap getPanItems calls.
        // This allows us to make panning do multiple pulls for items, while still using other mods patches for getPanItems.
        public override void DoFunction(GameLocation location, int x, int y, int power, Farmer who)
        {
            // Copied from Tool.cs, replicate logic instead of calling base.DoFunction()
            this.lastUser = who;
            power = who.toolPower;
            who.stopJittering();
            Game1.recentMultiplayerRandom = new Random((short)Game1.random.Next(-32768, 32768));

            List<Vector2> tileLocations = base.tilesAffected(new Vector2(x / 64, y / 64), power, who);

            foreach (Vector2 tileLocation in tileLocations)
            {
                if (who.currentLocation.orePanPoint.X == tileLocation.X && who.currentLocation.orePanPoint.Y == tileLocation.Y)
                {
                    // Copied from Pan.cs, replicate logic instead of calling base.DoFunction()
                    location.localSound("coin");
                    who.addItemsByMenuIfNecessary(this.GetPanItemsUpgradeable(location, who));
                    location.orePanPoint.Value = Point.Zero;

                    ModEntry.Instance.Helper.Reflection.GetMethod((Pan)this, "finish").Invoke();
                    break;
                }
            }
            ModEntry.Instance.Helper.Reflection.GetMethod((Pan)this, "finish").Invoke();
        }

        public List<Item> GetPanItemsUpgradeable(GameLocation location, Farmer who)
        {
            List<Item> panItems = new();

            float dailyLuck = (float)who.DailyLuck * ModEntry.Config.DailyLuckMultiplier;
            Log.Debug($"Daily Luck {who.DailyLuck} * Multiplier {ModEntry.Config.DailyLuckMultiplier} = Weighted Daily Luck {dailyLuck}");
            float buffLuck = who.LuckLevel * ModEntry.Config.LuckLevelMultiplier;
            Log.Debug($"Buff Luck {who.LuckLevel} * Multiplier {ModEntry.Config.LuckLevelMultiplier} = Weighted Buff Luck {buffLuck}");
            float chance = ModEntry.Config.ExtraDrawBaseChance + dailyLuck + buffLuck;
            Log.Debug($"Chance of Extra Draw {chance} = Base Chance {ModEntry.Config.ExtraDrawBaseChance} + Weighted Daily Luck {dailyLuck} + Weighted Buff Luck {buffLuck}");
            int panCount = 1;

            panItems.AddRange(base.getPanItems(location, who));
            for (int i = 0; i < this.UpgradeLevel; i++)
            {
                if (chance > Game1.random.NextDouble())
                {
                    // location is used to seed the random number for selecting which treasure is received in vanilla.
                    // do something to reseed it so that we aren't just getting the same treasure up to 5 times.
                    location.orePanPoint.X++;
                    panCount++;
                    panItems.AddRange(base.getPanItems(location, who));
                }
            }
            Log.Debug($"Did {panCount} draws using level {this.UpgradeLevel} pan.");
            return panItems;
        }

        
        // Fix bug where holding action key will begin showing AoE for pan, similar to charging watering can.
        public override bool doesShowTileLocationMarker()
        {
            return false;
        }

        public override bool canBeTrashed()
        {
            return false;
        }

        public override bool actionWhenPurchased()
        {
            if (this.UpgradeLevel > 0 && Game1.player.toolBeingUpgraded.Value == null)
            {
                Tool t = Game1.player.getToolFromName("Pan");
                Game1.player.removeItemFromInventory(t);
                if (t is not UpgradeablePan)
                {
                    t = new UpgradeablePan(upgradeLevel: 2);
                } else
                {
                    t.UpgradeLevel++;
                }
                Game1.player.toolBeingUpgraded.Value = t;
                Game1.player.daysLeftForToolUpgrade.Value = ModEntry.Config.UpgradeDays;
                Game1.playSound("parry");
                Game1.exitActiveMenu();
                Game1.drawDialogue(Game1.getCharacterFromName("Clint"), Game1.content.LoadString("Strings\\StringsFromCSFiles:Tool.cs.14317"));
                return true;
            }
            return base.actionWhenPurchased();
        }

        public static void AddToShopStock(Dictionary<ISalable, int[]> itemPriceAndStock, Farmer who)
        {
            if (who == Game1.player && CanBeUpgraded())
            {
                int quantity = 1;
                int upgradeLevel = who.getToolFromName("Pan").UpgradeLevel + 1;
                if (who.getToolFromName("Pan") is not UpgradeablePan)
                {
                    upgradeLevel = 2;
                }
                int upgradePrice = ModEntry.Instance.Helper.Reflection.GetMethod(
                    typeof(Utility), "priceForToolUpgradeLevel")
                    .Invoke<int>(upgradeLevel);
                upgradePrice = (int)(upgradePrice * ModEntry.Config.UpgradeCostMultiplier);
                int extraMaterialIndex = ModEntry.Instance.Helper.Reflection.GetMethod(
                    typeof(Utility), "indexOfExtraMaterialForToolUpgrade")
                    .Invoke<int>(upgradeLevel);
                int upgradeCostBars = ModEntry.Config.UpgradeCostBars;
                itemPriceAndStock.Add(
                    new UpgradeablePan(upgradeLevel: upgradeLevel),
                    new int[] { upgradePrice, quantity, extraMaterialIndex, upgradeCostBars});
            }
        }

        public static Hat PanToHat(UpgradeablePan pan)
        {
            return pan.UpgradeLevel switch
            {
                0 => new Hat(ModEntry.JsonAssets.GetHatId("Pan")),
                1 => new Hat(71),
                2 => new Hat(ModEntry.JsonAssets.GetHatId("Steel Pan")),
                3 => new Hat(ModEntry.JsonAssets.GetHatId("Gold Pan")),
                4 => new Hat(ModEntry.JsonAssets.GetHatId("Iridium Pan")),
                _ => new Hat(ModEntry.JsonAssets.GetHatId("Pan")),
            };
        }
    }
}
