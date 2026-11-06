using DeliveryManagement;
using DeliveryManagement.UI;

// Pass "--fresh" to start with no saved requests
bool startFresh = args.Contains("--fresh");
string dataFile = Path.Combine(AppContext.BaseDirectory, "dms-data.json");

// Make the main system, then show the menus
DeliveryManagementSystem system = new DeliveryManagementSystem(dataFile, startFresh);
ConsoleUI ui = new ConsoleUI(system);
ui.Run();
