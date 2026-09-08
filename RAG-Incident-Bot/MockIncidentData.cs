// =========================================================================
// MockIncidentData.cs
//
// A small, hardcoded "knowledge base" standing in for a real export of
// Application Insights exceptions or past support tickets. Themed around
// issues similar to real YOOBIC integration work, but with fictional
// specifics for this learning POC.
//
// This static class is read by Program.cs during the "ingestion" step,
// where each incident's Text is turned into an embedding and stored in
// the vector store.
// =========================================================================

public static class MockIncidentData
{
    public static readonly List<(string Id, string Text)> Incidents = new()
    {
        ("INC-1001",
            "YOOBIC API calls started failing with invalid token errors. Root cause: " +
            "the auth token has a 20-minute expiry, but our code misread the expires_in " +
            "field as 0 seconds instead of 3600 seconds, causing unnecessary refreshes on " +
            "every request. Fix: parse expires_in correctly and cache the token with a " +
            "1-minute safety buffer before its real expiry."),

        ("INC-1002",
            "Store import file uploads were getting stuck at 99.9% processing and never " +
            "completing. Root cause: the import is an asynchronous job that returns 202 " +
            "Accepted immediately, but our integration was treating it like a synchronous " +
            "call and not polling the job status endpoint. Fix: implement polling against " +
            "the jobs endpoint until the job reports completion."),

        ("INC-1003",
            "POS terminal at a franchise store went offline overnight and stayed " +
            "unreachable until a manual restart. Root cause: a firmware update triggered " +
            "an automatic reboot that failed to reconnect to the store's network. Fix: " +
            "store manager restarted the terminal manually; IT flagged the firmware " +
            "version to be excluded from future auto-updates."),

        ("INC-1004",
            "Store names containing commas were being split into multiple incorrect tags " +
            "when synced to YOOBIC. Root cause: YOOBIC's API uses commas as the delimiter " +
            "for tag arrays, so any comma in a store name was misinterpreted as a tag " +
            "separator. Fix: replaced commas with periods in store names before sending " +
            "to the API."),

        ("INC-1005",
            "Bulk user updates to YOOBIC were intermittently failing when updating more " +
            "than 1000 users in a single JSON payload. Root cause: YOOBIC enforces a hard " +
            "limit of 1000 records per JSON upload. Fix: batch large update sets into " +
            "chunks of 1000 or fewer, with a short delay between batches."),

        ("INC-1006",
            "A franchisee reported that the mobile checkout app was crashing whenever " +
            "they tried to apply a loyalty discount code. Root cause: a null reference " +
            "exception when the discount lookup returned no matching record. Fix: added " +
            "null handling and a user-facing 'invalid code' message instead of a crash."),

        ("INC-1007",
            "Nightly data sync between the HR system and YOOBIC was silently dropping " +
            "records for users without an assigned store number. Root cause: the sync " +
            "query implicitly filtered out any user with a null store number instead of " +
            "flagging them. Fix: added explicit logging for skipped records and an alert " +
            "if the skipped count exceeds a threshold."),

        ("INC-1008",
            "API calls to YOOBIC started returning 429 Too Many Requests during a large " +
            "batch update. Root cause: the batch job was firing requests in a tight loop " +
            "with no throttling, exceeding YOOBIC's rate limit. Fix: added a delay between " +
            "requests and a retry-with-backoff policy for 429 responses."),
    };
}
