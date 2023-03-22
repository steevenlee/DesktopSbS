using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using DesktopSbS.Interop;
using DesktopSbS.View;
using DesktopSbS.Model;
using System.Drawing;

namespace DesktopSbS
{
    public class WinSbS
    {
        internal const bool
            DISPLAY_LEFT = true,
            DISPLAY_RIGHT = true;

        public string Title { get; set; }

        public IntPtr Handle { get; private set; }
        public bool Maximized = false;
        public bool Taskbar = false;

        public View.ThumbWindow ThumbLeft { get; private set; }
        public View.ThumbWindow ThumbRight { get; private set; }

        public WinSbS Owner { get; set; }

        public int OffsetLevel { get; set; } // Window level in 3D mode, 0: bottom

        public WS WinStyle { get; set; }
        public WSEX WinStyleEx { get; set; }

        public Rectangle SourceRect { get; set; }

        public WinSbS(IntPtr inHandle)
        {
            this.Handle = inHandle;
        }

        public void CopyThumbInstances(WinSbS inOriginal)
        {
            this.ThumbLeft = inOriginal.ThumbLeft;
            this.ThumbRight = inOriginal.ThumbRight;
        }

        public void RegisterThumbs()
        {
            User32.SetWindowPos(this.Handle, User32.NOT_TOPMOST, 0, 0, 0, 0, SWP.SWP_NOMOVE | SWP.SWP_NOSIZE);

            IntPtr thumbLeft = IntPtr.Zero,
                thumbRight = IntPtr.Zero;

            this.ThumbLeft = new View.ThumbWindow();
            this.ThumbRight = new View.ThumbWindow();

            this.ThumbLeft.Show();
            this.ThumbRight.Show();


            int tlRes = DISPLAY_LEFT ? DwmApi.DwmRegisterThumbnail(this.ThumbLeft.Handle, this.Handle, out thumbLeft) : 0;
            int trRes = DISPLAY_RIGHT ? DwmApi.DwmRegisterThumbnail(this.ThumbRight.Handle, this.Handle, out thumbRight) : 0;

            if (tlRes == 0 && trRes == 0)
            {
                this.ThumbLeft.Thumb = thumbLeft;
                this.ThumbRight.Thumb = thumbRight;
                this.UpdateThumbs();
            }



        }

        public void UnRegisterThumbs()
        {
            DwmApi.DwmUnregisterThumbnail(this.ThumbLeft.Thumb);
            DwmApi.DwmUnregisterThumbnail(this.ThumbRight.Thumb);

            this.ThumbLeft.Close();
            this.ThumbRight.Close();

        }

        public void UpdateThumbs(bool isTaskBar = false)
        {
            SbSComputedVariables scv = Options.ComputedVariables;
            int parallaxDecal = 2 * this.OffsetLevel * Options.ParallaxEffect;
            DwmApi.DWM_THUMBNAIL_PROPERTIES props = new DwmApi.DWM_THUMBNAIL_PROPERTIES();
            props.fVisible = true;
            props.dwFlags = DwmApi.DWM_TNP_VISIBLE | DwmApi.DWM_TNP_RECTDESTINATION | DwmApi.DWM_TNP_RECTSOURCE;
            Rectangle srcRect = Rectangle.Intersect(this.SourceRect, Options.ScreenSrcView);

            // Left

            /* Dest global position */
            Rectangle dstRectLeft = new Rectangle(
                scv.DestPositionX,
                scv.DestPositionY,
                (int)Math.Ceiling(srcRect.Width / scv.RatioX),
                (int)Math.Ceiling(srcRect.Height / scv.RatioY));
            dstRectLeft.Offset(
                (int)Math.Floor(Math.Max(0, srcRect.Left - Options.AreaSrcBounds.Left) / scv.RatioX + parallaxDecal),
                (int)Math.Floor(Math.Max(0, srcRect.Top - Options.AreaSrcBounds.Top) / scv.RatioY));
            User32.SetWindowPos(this.ThumbLeft.Handle, this.Owner?.ThumbLeft.Handle ?? IntPtr.Zero,
                dstRectLeft.X, dstRectLeft.Y, dstRectLeft.Width, dstRectLeft.Height,
                SWP.SWP_ASYNCWINDOWPOS);

            props.rcSource = RECT.fromRectangle(srcRect); /* Global location */
            props.rcDestination = new RECT(0, 0, dstRectLeft.Width, dstRectLeft.Height); /* Relative area on dest */
            DwmApi.DwmUpdateThumbnailProperties(this.ThumbLeft.Thumb, ref props);

            // Right

            /* Dest global position */
            Rectangle dstRectRight = new Rectangle(
                scv.DestPositionX + scv.DecalSbSX,
                scv.DestPositionY + scv.DecalSbSY,
                (int)Math.Ceiling(srcRect.Width / scv.RatioX),
                (int)Math.Ceiling(srcRect.Height / scv.RatioY));
            dstRectRight.Offset(
                (int)Math.Floor(Math.Max(0, srcRect.Left - Options.AreaSrcBounds.Left) / scv.RatioX - parallaxDecal),
                (int)Math.Floor(Math.Max(0, srcRect.Top - Options.AreaSrcBounds.Top) / scv.RatioY));
            User32.SetWindowPos(this.ThumbRight.Handle, this.Owner?.ThumbRight.Handle ?? IntPtr.Zero,
                dstRectRight.X, dstRectRight.Y, dstRectRight.Width, dstRectRight.Height,
                SWP.SWP_ASYNCWINDOWPOS);

            props.rcSource = RECT.fromRectangle(srcRect); /* Global location */
            props.rcDestination = new RECT(0, 0, dstRectRight.Width, dstRectRight.Height); /* Relative area on dest */
            DwmApi.DwmUpdateThumbnailProperties(this.ThumbRight.Thumb, ref props);



#if DEBUG
            //if (this.Title.Contains("About"))
            //{
            //    App.Current.Dispatcher.Invoke(() =>
            //    {
            //        DebugWindow.Instance.UpdateMessage($"Source Win {this.SourceRect}{Environment.NewLine}Src Thumb {props.rcSource}{Environment.NewLine}Dst Thumb {props.rcDestination}{Environment.NewLine}Dst Pos Left: {Math.Max(0, this.SourceRect.Left) / 2} Top: {Math.Max(0, this.SourceRect.Top)}");
            //    });
            //}

#endif

        }



        public override string ToString()
        {
            return this.Title;
        }
    }

}

