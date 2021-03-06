﻿/*
Copyright (c) 2014, Lars Brubaker
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met: 

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer. 
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution. 

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

The views and conclusions contained in the software and documentation are those
of the authors and should not be interpreted as representing official policies, 
either expressed or implied, of the FreeBSD Project.
*/

using System;
using System.IO;
using MatterHackers.Agg;
using MatterHackers.Agg.Image;
using MatterHackers.Agg.PlatformAbstract;
using MatterHackers.Agg.UI;
using MatterHackers.MatterControl.DataStorage;
using MatterHackers.MatterControl.SlicerConfiguration;
using MatterHackers.MeshVisualizer;
using MatterHackers.VectorMath;

namespace MatterHackers.MatterControl.PartPreviewWindow
{
    public class Cover : GuiWidget
    {
        public Cover(HAnchor hAnchor = HAnchor.None, VAnchor vAnchor = VAnchor.None)
            : base(hAnchor, vAnchor)
        {
        }
    }

    public class PartPreview3DWidget : PartPreviewWidget
    {
        protected static readonly int DefaultScrollBarWidth = 120;

        protected bool autoRotating = false;
        protected bool allowAutoRotate = false;
        public MeshViewerWidget meshViewerWidget;
        event EventHandler unregisterEvents;

        protected ViewControls3D viewControls3D;

		bool needToRecretaeBed = false;

        public PartPreview3DWidget()
        {
            SliceSettingsWidget.RegisterForSettingsChange("bed_size", SetFlagToRecreateBedAndPartPosition, ref unregisterEvents);
            SliceSettingsWidget.RegisterForSettingsChange("print_center", SetFlagToRecreateBedAndPartPosition, ref unregisterEvents);
            SliceSettingsWidget.RegisterForSettingsChange("build_height", SetFlagToRecreateBedAndPartPosition, ref unregisterEvents);
            SliceSettingsWidget.RegisterForSettingsChange("bed_shape", SetFlagToRecreateBedAndPartPosition, ref unregisterEvents);
            SliceSettingsWidget.RegisterForSettingsChange("center_part_on_bed", SetFlagToRecreateBedAndPartPosition, ref unregisterEvents);
#if false
            "extruder_offset",
#endif

            ActivePrinterProfile.Instance.ActivePrinterChanged.RegisterEvent(SetFlagToRecreateBedAndPartPosition, ref unregisterEvents);
        }

        void SetFlagToRecreateBedAndPartPosition(object sender, EventArgs e)
        {
			needToRecretaeBed = true;
        }

		void RecreateBed()
		{
			double buildHeight = ActiveSliceSettings.Instance.BuildHeight;

			UiThread.RunOnIdle((state) =>
			{
				meshViewerWidget.CreatePrintBed(
					new Vector3(ActiveSliceSettings.Instance.BedSize, buildHeight),
					ActiveSliceSettings.Instance.BedCenter,
					ActiveSliceSettings.Instance.BedShape);
				PutOemImageOnBed();
			});
		}

		public override void OnDraw(Graphics2D graphics2D)
		{
			if(needToRecretaeBed)
			{
				needToRecretaeBed = false;
				RecreateBed();
			}
			base.OnDraw(graphics2D);
		}

        protected void PutOemImageOnBed()
        {
            // this is to add an image to the bed
            string imagePathAndFile = Path.Combine("OEMSettings", "bedimage.png");
            if (allowAutoRotate && StaticData.Instance.FileExists(imagePathAndFile))
            {
                ImageBuffer wattermarkImage = StaticData.Instance.LoadImage(imagePathAndFile);

                ImageBuffer bedImage = meshViewerWidget.BedImage;
                Graphics2D bedGraphics = bedImage.NewGraphics2D();
                bedGraphics.Render(wattermarkImage,
                    new Vector2((bedImage.Width - wattermarkImage.Width) / 2, (bedImage.Height - wattermarkImage.Height) / 2));
            }
        }
        
        public override void OnClosed(EventArgs e)
        {
            if (unregisterEvents != null)
            {
                unregisterEvents(this, null);
            }
            base.OnClosed(e);
        }

        protected static SolidSlider InsertUiForSlider(FlowLayoutWidget wordOptionContainer, string header, double min = 0, double max = .5)
        {
            double scrollBarWidth = 10;
            TextWidget spacingText = new TextWidget(header, textColor: ActiveTheme.Instance.PrimaryTextColor);
            spacingText.Margin = new BorderDouble(10, 3, 3, 5);
            spacingText.HAnchor = HAnchor.ParentLeft;
            wordOptionContainer.AddChild(spacingText);
            SolidSlider namedSlider = new SolidSlider(new Vector2(), scrollBarWidth, 0, 1);
            namedSlider.TotalWidthInPixels = DefaultScrollBarWidth;
            namedSlider.Minimum = min;
            namedSlider.Maximum = max;
            namedSlider.Margin = new BorderDouble(3, 5, 3, 3);
            namedSlider.HAnchor = HAnchor.ParentCenter;
            namedSlider.View.BackgroundColor = new RGBA_Bytes();
            wordOptionContainer.AddChild(namedSlider);

            return namedSlider;
        }
    }
}
