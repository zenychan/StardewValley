using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Projectiles;

namespace LookToTheSky
{
    public class SkyProjectile : BasicProjectile
    {
        public static Color[] Colors = new Color[]
        {
            Color.White,
            Color.Red,
            Color.Yellow,
            Color.Orange,
            Color.Blue,
            Color.Purple,
            Color.CornflowerBlue,
            Color.GreenYellow,
            Color.Green,
            Color.Aquamarine,
        };

        public int X => (int)base.position.X + 32;
        public int Y => (int)base.position.Y + 32;

        public int ClickY;

        public StardewValley.Object Ammo;

        public SkyProjectile(int parentSheetIndex, int xPos, string collisionSound, StardewValley.Object ammo, int clickY = 0, int speed = 1) :
            base(0, parentSheetIndex, 0, 0, (float)(Math.PI / (double)(64f + (float)Game1.random.Next(-63, 64))), 0, -12 * speed, new Vector2(xPos, Game1.viewport.Height), collisionSound, "", false, false, null, Game1.player, true)
        {
            this.Ammo = ammo;
            this.ClickY = clickY;
        }

        public override void draw(SpriteBatch b)
        {
            b.Draw(Game1.objectSpriteSheet, new Rectangle(this.X - 32, this.Y - 32, 64, 64), Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, this.currentTileSheetIndex, 16, 16), Color.White); //, this.rotation, Game1.GlobalToLocal(Game1.viewport, Game1.player.position), 4, SpriteEffects.None, 0);
        }

        public bool UpdatePosition(GameTime time)
        {
            this.updatePosition(time);
            if (this.Y <= this.ClickY)
            {
                if (this.currentTileSheetIndex == 441)
                {
                    // Add firework
                    ModEntry.Instance.SkyObjects.Add(new Firework(this.X - 64, this.ClickY - 64, Colors[Game1.random.Next(Colors.Length)]));
                    Game1.playSound("explosion");
                    return true;
                }
                else if (this.currentTileSheetIndex == 387)
                {
                    // Add a star
                    ModEntry.Instance.SkyObjects.Add(new Star(this.X - 10, this.ClickY - 10));
                    return true;
                }
            }
            return false;
        }
    }
}
