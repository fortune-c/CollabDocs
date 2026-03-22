import signalR from "@microsoft/signalr";

// Configuration - will change these if needed
const HUB_URL = "http://localhost:5000/hubs/document";
const DOCUMENT_ID = "document-id-here";

// build a connection to the hub
const connection = new signalR.HubConnectionBuilder()
.withUrl(HUB_URL, { accessTokenFactory: () => "jwt-token-here"})
.withAutomaticReconnect()
.configureLogging(signalR.LogLevel.Information) // Debugging purpose
.build();

// Listen for the confirmation after joining
connection.on("JoinedDocument", (documentId) => {
    console.log(`Successfully joined document: ${documentId}`);
});

// Listen for the confirmation after leaving
connection.on("LeftDocument", (documentId) => {
    console.log(`Successfully left document: ${documentId}`);
});

// Listen for test messages from the server
connection.on("ReceiveTestMessage", (message) => {
    console.log(`Received test message: ${message}`);
});

// Start the connection and test the hub
async function startAndTest() {
    try {
        await connection.start();
        console.log("Connected to the hub!");

        // Join the document room
        await connection.invoke("JoinDocument", DOCUMENT_ID);

        // Send a test message to the room
        await connection.invoke("SendTestMessage", DOCUMENT_ID, "Hello from the test client!");

        // Optionally leave the room after a delay
        setTimeout(async () => {
            await connection.invoke("LeaveDocument", DOCUMENT_ID);
            await connection.stop();
            console.log("Disconnected from the hub.");
        }, 5000); // Leave after 5 seconds

    } catch (err) {
        console.error("Error:", err);
    }
}

// Run the test
startAndTest();

// Keep the script running to receive messages
process.stdin.resume();