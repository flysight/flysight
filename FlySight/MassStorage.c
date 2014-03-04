#include "Config/AppConfig.h"
#include "Main.h"
#include "MassStorage.h"

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

void EVENT_USB_Device_Connect(void)
{

}

void EVENT_USB_Device_Disconnect(void)
{

}

void EVENT_USB_Device_ConfigurationChanged(void)
{
	MS_Device_ConfigureEndpoints(&Disk_MS_Interface) ;
}

void EVENT_USB_Device_UnhandledControlRequest(void)
{
	MS_Device_ProcessControlRequest(&Disk_MS_Interface);
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
