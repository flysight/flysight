#import "MainController.h"
#import "FSWindowController.h"
#import "FSDataManager.h"

@implementation MainController

-(id) init
{
	self = [super init];
	if (self != nil)
	{
		windowControllers = [[NSMutableArray alloc] init];		
	
		licenseText = @"FlySight Viewer copyright (c) 2010, Jason Cooper\n\n"
					   "CorePlot copyright (c) 2010, Drew McCormack, Brad Larson, Eric Skroch, "
		               "Barry Wark, Dirkjan Krijnders, Rick Maddy, Vijay Kalusani\n"
					   "All rights reserved.\n\n"
		               "Redistribution and use without modification, is permitted provided that the"
					   "following conditions are met:\n\n"
					   "Redistributions must reproduce the above copyright notice, this list of "
					   "conditions and the following disclaimer in the documentation and/or other "
					   "materials provided with the distribution. Neither the name of the Core Plot "
		               "Project nor the names of its contributors may be used to endorse or promote "
		               "products derived from this software without specific prior written permission. "
		               "THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS \"AS IS\" "
		               "AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE "
		               "IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE "
		               "DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE "
		               "FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL "
		               "DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR "
					   "SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER "
		               "CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, "
		               "OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE "
					   "OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.";
	}
	
	return self;
}

-(void) dealloc
{
	[windowControllers release];
	
	[super dealloc];
}

-(void) awakeFromNib
{
	[self open: self];
}

-(IBAction) open: (id) sender
{
	NSOpenPanel *openDialog = [NSOpenPanel openPanel];
	[openDialog setCanChooseFiles: YES];
	[openDialog setCanChooseDirectories: NO];
	[openDialog setAllowsMultipleSelection: NO];
	
	if ([openDialog runModal] == NSFileHandlingPanelOKButton)
	{
		NSURL *url = [openDialog URL];
		if ([url isFileURL])
		{

// Create dataset
			
			NSString *dataString = [NSString stringWithContentsOfFile: [url path] 
															encoding: NSASCIIStringEncoding 
															   error: NULL];
			FSDataManager *dataManager;
			dataManager = [[FSDataManager alloc] initWithNMEAString: dataString];
			if (dataManager == nil) dataManager = [[FSDataManager alloc] initWithCSVString: dataString];
			if (dataManager == nil)
			{
				NSRunAlertPanel(@"Invalid file", @"The selected file does not seem to be in a supported type.  Please select an NMEA file or a CSV file.", nil, nil, nil);
			}
			else
			{
			
// Initialize window controller
			
				FSWindowController *windowController = [[FSWindowController alloc] initWithDataManager: dataManager];
				windowController.window.title = [[url path] lastPathComponent];
				[windowControllers addObject: windowController];
				[windowController loadWindow];

				[dataManager release];
			}
		}
	}
}

-(IBAction) runPageLayout: (id) sender
{
	NSPageLayout *pageLayout = [NSPageLayout pageLayout];
	[pageLayout runModal];
}

@end
