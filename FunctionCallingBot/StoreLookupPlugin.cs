using System.ComponentModel;
using Microsoft.SemanticKernel;

public class StoreLookupPlugin
{

    private readonly Dictionary<string, StoreRecord> _storeDatabase = new()
    {
        ["ST001"] = new StoreRecord("ST001", "Anytown", "POS001", "John Doe", "Active"),
        ["AN456"] = new StoreRecord("AN456", "Othertown", "POS002", "Jane Smith", "Closed"),
        ["EX789"] = new StoreRecord("EX789", "Sometown", "POS003", "Alice Johnson", "Active"),
        ["XY123"] = new StoreRecord("XY123", "Newcity", "POS004", "Bob Brown", "Inactive"),
        ["LM987"] = new StoreRecord("LM987", "Oldtown", "POS005", "Charlie Davis", "Active"),
        ["QR654"] = new StoreRecord("QR654", "Uptown", "POS006", "Diana Evans", "Closed"),
        ["GH321"] = new StoreRecord("GH321", "Downtown", "POS007", "Ethan Foster", "Active"),
        ["JK098"] = new StoreRecord("JK098", "Midtown", "POS008", "Fiona Green", "Inactive"),
        ["UV567"] = new StoreRecord("UV567", "Eastside", "POS009", "George Harris", "Active"),
        ["WX234"] = new StoreRecord("WX234", "Westside", "POS010", "Hannah Irving", "Closed"),
    };

    // -------------------------------------------------------------------------------------------
    // STEP 3: Implement the plugin method that will be called by the AI model.
    // The SKFunction attribute marks this method as a plugin function that can be called by the AI model.
    // The SKFunctionName attribute specifies the name of the function as it will be called by the AI model.
    // -------------------------------------------------------------------------------------------

    [KernelFunction("get_store_pos_id")]
    [Description("Gets the POS (point of sale) system ID for a given store code, e.g. ST001. ")]
    public string GetStorePosId([Description("The store code, e.g. ST001.")] string storeCode)
    {
        return _storeDatabase.TryGetValue(storeCode.ToUpper(), out var storeRecord)
            ? storeRecord.PosId
            : $"No store found with code '{storeCode}' in the database.";
    }

    [KernelFunction("get_store_status")]
    [Description("Gets the status of a given store code, e.g. ST001. ")]
    public string GetStoreStatus([Description("The store code, e.g. ST001.")] string storeCode)
    {
        return _storeDatabase.TryGetValue(storeCode.ToUpper(), out var storeRecord)
            ? storeRecord.Status
            : $"No store found with code '{storeCode}' in the database.";
    }

    [KernelFunction("get_store_manager")]
    [Description("Gets the manager name of a given store code, e.g. ST001. ")]
    public string GetStoreManagerName([Description("The store code, e.g. ST001.")] string storeCode)
    {
        return _storeDatabase.TryGetValue(storeCode.ToUpper(), out var storeRecord)
            ? storeRecord.ManagerName
            : $"No store found with code '{storeCode}' in the database.";
    }

    [KernelFunction("list_all_stores")]
    [Description("Lists all stores in the database.")]
    public string ListAllStores()
    {
        var storeList = string.Join(Environment.NewLine, _storeDatabase.Values.Select(store => $"{store.Code} - {store.City} - {store.PosId} - {store.ManagerName} - {store.Status}"));
        return storeList;
    }
    
    private record StoreRecord(string Code, string City, string PosId, string ManagerName, string Status);
}