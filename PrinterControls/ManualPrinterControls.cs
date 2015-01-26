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
using System.Collections.Generic;
using System.IO;

using MatterHackers.Agg;
using MatterHackers.Agg.Image;
using MatterHackers.Agg.PlatformAbstract;
using MatterHackers.Agg.UI;

using MatterHackers.Localizations;
using MatterHackers.MatterControl.CustomWidgets;
using MatterHackers.MatterControl.DataStorage;
using MatterHackers.MatterControl.PrinterControls;
using MatterHackers.MatterControl.PrinterCommunication;
using MatterHackers.MatterControl.SlicerConfiguration;
using MatterHackers.VectorMath;

namespace MatterHackers.MatterControl
{


    public class ManualPrinterControls : GuiWidget
    {
        event EventHandler unregisterEvents;

        TemperatureControls temperatureControlsContainer;
        DisableableWidget movementControlsContainer;
        DisableableWidget fanControlsContainer;
        DisableableWidget tuningAdjustmentControlsContainer;
        DisableableWidget macroControls;

        TextImageButtonFactory textImageButtonFactory = new TextImageButtonFactory();

        static public RootedObjectEventHandler AddPluginControls = new RootedObjectEventHandler();
        
        public ManualPrinterControls()
        {
            SetDisplayAttributes();
            
            FlowLayoutWidget controlsTopToBottomLayout = new FlowLayoutWidget(FlowDirection.TopToBottom);
            controlsTopToBottomLayout.HAnchor = Agg.UI.HAnchor.Max_FitToChildren_ParentWidth;
            controlsTopToBottomLayout.VAnchor = Agg.UI.VAnchor.FitToChildren;
            controlsTopToBottomLayout.Name = "ManualPrinterControls.ControlsContainer";
            controlsTopToBottomLayout.Margin = new BorderDouble(0);

            AddTemperatureControls(controlsTopToBottomLayout);
            AddMovementControls(controlsTopToBottomLayout);
            AddFanControls(controlsTopToBottomLayout);
            AddMacroControls(controlsTopToBottomLayout);
            AddAdjustmentControls(controlsTopToBottomLayout);

            AddChild(controlsTopToBottomLayout);
            AddHandlers();
            SetVisibleControls();

            if (!pluginsQueuedToAdd)
            {
                UiThread.RunOnIdle(AddPlugins);
                pluginsQueuedToAdd = true;
            }
        }

        static bool pluginsQueuedToAdd = false;
        public void AddPlugins(object state)
        {
            AddPluginControls.CallEvents(this, null);
            pluginsQueuedToAdd = false;
        }

        private void AddFanControls(FlowLayoutWidget controlsTopToBottomLayout)
        {
            fanControlsContainer = new FanControls();
            if (ActiveSliceSettings.Instance.HasFan())
            {                
                controlsTopToBottomLayout.AddChild(fanControlsContainer);
            }
        }

        private void AddMacroControls(FlowLayoutWidget controlsTopToBottomLayout)
        {
            macroControls = new MacroControls();
            controlsTopToBottomLayout.AddChild(macroControls);
        }

        private void AddMovementControls(FlowLayoutWidget controlsTopToBottomLayout)
        {
            movementControlsContainer = new MovementControls();
            controlsTopToBottomLayout.AddChild(movementControlsContainer);
        }

        private void AddTemperatureControls(FlowLayoutWidget controlsTopToBottomLayout)
        {
            temperatureControlsContainer = new TemperatureControls();
            controlsTopToBottomLayout.AddChild(temperatureControlsContainer);
        }

        private void AddAdjustmentControls(FlowLayoutWidget controlsTopToBottomLayout)
        {		
            tuningAdjustmentControlsContainer = new AdjustmentControls();
            controlsTopToBottomLayout.AddChild(tuningAdjustmentControlsContainer);
        }

        public override void OnClosed(EventArgs e)
        {
            if (unregisterEvents != null)
            {
                unregisterEvents(this, null);
            }

            base.OnClosed(e);
        }


        private void SetDisplayAttributes()
        {
            HAnchor = Agg.UI.HAnchor.Max_FitToChildren_ParentWidth;
            VAnchor = Agg.UI.VAnchor.FitToChildren;
        }

        private void SetVisibleControls()
        {
            if (ActivePrinterProfile.Instance.ActivePrinter == null)
            {
                // no printer selected
                foreach (DisableableWidget extruderTemperatureControlWidget in temperatureControlsContainer.ExtruderWidgetContainers)
                {
                    extruderTemperatureControlWidget.SetEnableLevel(DisableableWidget.EnableLevel.Disabled);
                }
                temperatureControlsContainer.BedTemperatureControlWidget.SetEnableLevel(DisableableWidget.EnableLevel.Disabled);
                movementControlsContainer.SetEnableLevel(DisableableWidget.EnableLevel.Disabled);
                fanControlsContainer.SetEnableLevel(DisableableWidget.EnableLevel.Disabled);
                tuningAdjustmentControlsContainer.SetEnableLevel(DisableableWidget.EnableLevel.Disabled);
				
                macroControls.SetEnableLevel(DisableableWidget.EnableLevel.Disabled);
            }
            else // we at least have a printer selected
            {
                switch (PrinterConnectionAndCommunication.Instance.CommunicationState)
                {
                    case PrinterConnectionAndCommunication.CommunicationStates.Disconnecting:
                    case PrinterConnectionAndCommunication.CommunicationStates.ConnectionLost:
                    case PrinterConnectionAndCommunication.CommunicationStates.Disconnected:
                    case PrinterConnectionAndCommunication.CommunicationStates.AttemptingToConnect:
                    case PrinterConnectionAndCommunication.CommunicationStates.FailedToConnect:
                        foreach (DisableableWidget extruderTemperatureControlWidget in temperatureControlsContainer.ExtruderWidgetContainers)
                        {
                            extruderTemperatureControlWidget.SetEnableLevel(DisableableWidget.EnableLevel.ConfigOnly);
                        }
                        temperatureControlsContainer.BedTemperatureControlWidget.SetEnableLevel(DisableableWidget.EnableLevel.ConfigOnly);
                        movementControlsContainer.SetEnableLevel(DisableableWidget.EnableLevel.ConfigOnly);
                        fanControlsContainer.SetEnableLevel(DisableableWidget.EnableLevel.Disabled);
                        tuningAdjustmentControlsContainer.SetEnableLevel(DisableableWidget.EnableLevel.Disabled);                        
                        macroControls.SetEnableLevel(DisableableWidget.EnableLevel.ConfigOnly);
                        break;

                    case PrinterConnectionAndCommunication.CommunicationStates.FinishedPrint:
                    case PrinterConnectionAndCommunication.CommunicationStates.Connected:
                        foreach (DisableableWidget extruderTemperatureControlWidget in temperatureControlsContainer.ExtruderWidgetContainers)
                        {
                            extruderTemperatureControlWidget.SetEnableLevel(DisableableWidget.EnableLevel.Enabled);
                        }
                        temperatureControlsContainer.BedTemperatureControlWidget.SetEnableLevel(DisableableWidget.EnableLevel.Enabled);
                        movementControlsContainer.SetEnableLevel(DisableableWidget.EnableLevel.Enabled);
                        fanControlsContainer.SetEnableLevel(DisableableWidget.EnableLevel.Enabled);                        
                        macroControls.SetEnableLevel(DisableableWidget.EnableLevel.Enabled);
                        tuningAdjustmentControlsContainer.SetEnableLevel(DisableableWidget.EnableLevel.Disabled);
                        break;

                    case PrinterConnectionAndCommunication.CommunicationStates.PrintingToSd:
                        foreach (DisableableWidget extruderTemperatureControlWidget in temperatureControlsContainer.ExtruderWidgetContainers)
                        {
                            extruderTemperatureControlWidget.SetEnableLevel(DisableableWidget.EnableLevel.Disabled);
                        }
                        temperatureControlsContainer.BedTemperatureControlWidget.SetEnableLevel(DisableableWidget.EnableLevel.Disabled);
                        movementControlsContainer.SetEnableLevel(DisableableWidget.EnableLevel.ConfigOnly);
                        fanControlsContainer.SetEnableLevel(DisableableWidget.EnableLevel.Disabled);
                        macroControls.SetEnableLevel(DisableableWidget.EnableLevel.ConfigOnly);
                        tuningAdjustmentControlsContainer.SetEnableLevel(DisableableWidget.EnableLevel.Disabled);
                        break;

                    case PrinterConnectionAndCommunication.CommunicationStates.PrintingFromSd:
                        foreach (DisableableWidget extruderTemperatureControlWidget in temperatureControlsContainer.ExtruderWidgetContainers)
                        {
                            extruderTemperatureControlWidget.SetEnableLevel(DisableableWidget.EnableLevel.Enabled);
                        }
                        temperatureControlsContainer.BedTemperatureControlWidget.SetEnableLevel(DisableableWidget.EnableLevel.Enabled);
                        movementControlsContainer.SetEnableLevel(DisableableWidget.EnableLevel.ConfigOnly);
                        fanControlsContainer.SetEnableLevel(DisableableWidget.EnableLevel.Enabled);
                        macroControls.SetEnableLevel(DisableableWidget.EnableLevel.ConfigOnly);
                        tuningAdjustmentControlsContainer.SetEnableLevel(DisableableWidget.EnableLevel.Disabled);
                        break;

                    case PrinterConnectionAndCommunication.CommunicationStates.PreparingToPrint:
                    case PrinterConnectionAndCommunication.CommunicationStates.PreparingToPrintToSd:
                    case PrinterConnectionAndCommunication.CommunicationStates.Printing:
                        switch (PrinterConnectionAndCommunication.Instance.PrintingState)
                        {
                            case PrinterConnectionAndCommunication.DetailedPrintingState.HomingAxis:
                            case PrinterConnectionAndCommunication.DetailedPrintingState.HeatingBed:
                            case PrinterConnectionAndCommunication.DetailedPrintingState.HeatingExtruder:
                            case PrinterConnectionAndCommunication.DetailedPrintingState.Printing:
                                foreach (DisableableWidget extruderTemperatureControlWidget in temperatureControlsContainer.ExtruderWidgetContainers)
                                {
                                    extruderTemperatureControlWidget.SetEnableLevel(DisableableWidget.EnableLevel.Enabled);
                                }
                                temperatureControlsContainer.BedTemperatureControlWidget.SetEnableLevel(DisableableWidget.EnableLevel.Enabled);
                                movementControlsContainer.SetEnableLevel(DisableableWidget.EnableLevel.ConfigOnly);
                                fanControlsContainer.SetEnableLevel(DisableableWidget.EnableLevel.Enabled);
                                tuningAdjustmentControlsContainer.SetEnableLevel(DisableableWidget.EnableLevel.Enabled);                                
                                macroControls.SetEnableLevel(DisableableWidget.EnableLevel.ConfigOnly);
                                break;

                            default:
                                throw new NotImplementedException();
                        }
                        break;

                    case PrinterConnectionAndCommunication.CommunicationStates.Paused:
                        foreach (DisableableWidget extruderTemperatureControlWidget in temperatureControlsContainer.ExtruderWidgetContainers)
                        {
                            extruderTemperatureControlWidget.SetEnableLevel(DisableableWidget.EnableLevel.Enabled);
                        }
                        temperatureControlsContainer.BedTemperatureControlWidget.SetEnableLevel(DisableableWidget.EnableLevel.Enabled);
                        movementControlsContainer.SetEnableLevel(DisableableWidget.EnableLevel.Enabled);
                        fanControlsContainer.SetEnableLevel(DisableableWidget.EnableLevel.Enabled);
                        tuningAdjustmentControlsContainer.SetEnableLevel(DisableableWidget.EnableLevel.Enabled);                        
                        macroControls.SetEnableLevel(DisableableWidget.EnableLevel.Enabled);
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
        }        
        
        private void AddHandlers()
        {
            PrinterConnectionAndCommunication.Instance.CommunicationStateChanged.RegisterEvent(onPrinterStatusChanged, ref unregisterEvents);
            PrinterConnectionAndCommunication.Instance.EnableChanged.RegisterEvent(onPrinterStatusChanged, ref unregisterEvents);
        }

        private void onPrinterStatusChanged(object sender, EventArgs e)
        {
            SetVisibleControls();
			UiThread.RunOnIdle(invalidateWidget);
            
        }
			
		private void invalidateWidget(object state)
		{
			this.Invalidate();
		}

       
    }
}
