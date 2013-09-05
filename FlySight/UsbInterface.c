#include "Main.h"
#include "UsbInterface.h"
#include "uart.h"

USB_ClassInfo_MS_Device_t Disk_MS_Interface =
{
	.Config =
	{
		.InterfaceNumber       = 0,
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

USB_ClassInfo_CDC_Device_t UBX_CDC_Interface =
{
	.Config =
		{
			.ControlInterfaceNumber   = 0,
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


void EVENT_USB_Device_Connect(void)
{

}

void EVENT_USB_Device_Disconnect(void)
{

}

void EVENT_USB_Device_ConfigurationChanged(void)
{
	MS_Device_ConfigureEndpoints(&Disk_MS_Interface) ;
	CDC_Device_ConfigureEndpoints(&UBX_CDC_Interface);
}

void EVENT_USB_Device_ControlRequest(void)
{
	MS_Device_ProcessControlRequest(&Disk_MS_Interface);
	CDC_Device_ProcessControlRequest(&UBX_CDC_Interface);
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
	MS_Device_USBTask(&Disk_MS_Interface);
}