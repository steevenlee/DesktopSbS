using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopSbS.Model
{
    public struct SbSComputedVariables
    {
        public double RatioX, RatioY;

        public int DestPositionX, DestPositionY; /* Global position */
        public int DecalSbSX, DecalSbSY; /* Right side offset to left */

        public void UpdateVariables()
        {
            bool modeSbS = Options.ModeSbS;
            bool keepRatio = Options.KeepRatio;
            int destXOffsetX = (int)(Options.ScreenDestBounds.Width * Options.ViewRatioX) / (modeSbS ? 2 : 1);
            int destXOffsetY = (int)(Options.ScreenDestBounds.Height * Options.ViewRatioY) / (modeSbS ? 1 : 2);

            // Size ratio between src size and dest size
            RatioX = (modeSbS ? 2.0 : 1.0) * Options.AreaSrcBounds.Width / Options.ScreenDestBounds.Width;
            RatioX = (modeSbS ? 2.0 : 1.0) * Options.AreaSrcBounds.Width / Options.ScreenDestBounds.Width;
            RatioY = (!modeSbS ? 2.0 : 1.0) * Options.AreaSrcBounds.Height / Options.ScreenDestBounds.Height;


            DestPositionX = Options.ScreenDestBounds.Left;
            DestPositionY = Options.ScreenDestBounds.Top;

            if (keepRatio)
            {
                if (RatioX > RatioY)
                {
                    DestPositionY += (int)(Options.ScreenDestBounds.Height * (1 - RatioY / RatioX) / (!modeSbS ? 4 : 2));
                    RatioY = RatioX;
                }
                else
                {
                    DestPositionX += (int)(Options.ScreenDestBounds.Width * (1 - RatioX / RatioY) / (modeSbS ? 4 : 2));
                    RatioX = RatioY;
                }
            }

            DestPositionX -= destXOffsetX;
            DestPositionY -= destXOffsetY;

            /* Right side */
            DecalSbSX = modeSbS ? Options.ScreenDestBounds.Width / 2 : 0;
            DecalSbSY = modeSbS ? 0 : Options.ScreenDestBounds.Height / 2;
        }


    }

}
