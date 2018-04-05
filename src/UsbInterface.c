/***************************************************************************
**                                                                        **
**  FlySight firmware                                                     **
**  Copyright 2018 Michael Cooper, Will Glynn                             **
**                                                                        **
**  This program is free software: you can redistribute it and/or modify  **
**  it under the terms of the GNU General Public License as published by  **
**  the Free Software Foundation, either version 3 of the License, or     **
**  (at your option) any later version.                                   **
**                                                                        **
**  This program is distributed in the hope that it will be useful,       **
**  but WITHOUT ANY WARRANTY; without even the implied warranty of        **
**  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         **
**  GNU General Public License for more details.                          **
**                                                                        **
**  You should have received a copy of the GNU General Public License     **
**  along with this program.  If not, see <http://www.gnu.org/licenses/>. **
**                                                                        **
****************************************************************************
**  Contact: Michael Cooper                                               **
**  Website: http://flysight.ca/                                          **
****************************************************************************/

#include "Main.h"
#include "UsbInterface.h"
#include "uart.h"

USB_ClassInfo_MS_Device_t Disk_MS_Interface =
{
	.Config =
	{
		.InterfaceNumber       = INTERFACE_ID_MassStorage,
		.DataINEndpoint        =
		{
			.Address           = MASS_STORAGE_IN_EPADDR,
			.Size              = MASS_STORAGE_IO_EPSIZE,
			.Banks             = 1,
		},
		.DataOUTEndpoint       =
		{
			.Address           = MASS_STORAGE_OUT_EPADDR,
			.Size              = MASS_STORAGE_IO_EPSIZE,
			.Banks             = 1,
		},
		.TotalLUNs             = TOTAL_LUNS,
	},
};

#ifdef USE_SERIAL_INTERFACE
USB_ClassInfo_CDC_Device_t UBX_CDC_Interface =
{
	.Config =
		{
			.ControlInterfaceNumber   = INTERFACE_ID_CDC_CCI,
			.DataINEndpoint           =
				{
					.Address          = CDC_TX_EPADDR,
					.Size             = CDC_TXRX_EPSIZE,
					.Banks            = 1,
				},
			.DataOUTEndpoint =
				{
					.Address          = CDC_RX_EPADDR,
					.Size             = CDC_TXRX_EPSIZE,
					.Banks            = 1,
				},
			.NotificationEndpoint =
				{
					.Address          = CDC_NOTIFICATION_EPADDR,
					.Size             = CDC_NOTIFICATION_EPSIZE,
					.Banks            = 1,
				},
		},
};
#endif


void EVENT_USB_Device_Connect(void)
{

}

void EVENT_USB_Device_Disconnect(void)
{

}

void EVENT_USB_Device_ConfigurationChanged(void)
{
	MS_Device_ConfigureEndpoints(&Disk_MS_Interface) ;
#ifdef USE_SERIAL_INTERFACE
	CDC_Device_ConfigureEndpoints(&UBX_CDC_Interface);
#endif
}

void EVENT_USB_Device_ControlRequest(void)
{
	MS_Device_ProcessControlRequest(&Disk_MS_Interface);
#ifdef USE_SERIAL_INTERFACE
	CDC_Device_ProcessControlRequest(&UBX_CDC_Interface);
#endif
}

bool CALLBACK_MS_Device_SCSICommandReceived(
	USB_ClassInfo_MS_Device_t* const MSInterfaceInfo)
{
	bool CommandSuccess;
	
	LEDs_ChangeLEDs(LEDS_ALL_LEDS, 0);
	CommandSuccess = SCSI_DecodeSCSICommand(MSInterfaceInfo);
	LEDs_ChangeLEDs(LEDS_ALL_LEDS, Main_activeLED);
	
	return CommandSuccess;
}

void USBInterfaceTask(void)
{
#ifdef USE_SERIAL_INTERFACE
	uint16_t ch;
	
	// Pipe UART in -> CDC out
	if ((ch = uart_getc()) != UART_NO_DATA) {
		CDC_Device_SendByte(&UBX_CDC_Interface, ch & 0xff);
	}

	// Pipe CDC in -> UART out
	if ((ch = CDC_Device_ReceiveByte(&UBX_CDC_Interface)) >= 0) {
		uart_putc(ch & 0xff);
	}
	
	// Pump LUFA for both interfaces
	CDC_Device_USBTask(&UBX_CDC_Interface);
#endif
	MS_Device_USBTask(&Disk_MS_Interface);
}