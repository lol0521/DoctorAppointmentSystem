// wwwroot/js/simple-chat.js
let connection = null;
let chatOpen = false;
let askedForSuggestions = false;
let userWantsSuggestions = false;
let isQuickResponse = false; // Flag to track if message is from quick response button

function toggleChat() {
    const chatBox = document.getElementById('chatBox');
    chatOpen = !chatOpen;

    if (chatOpen) {
        chatBox.classList.remove('hidden');
        initializeChat();
        document.getElementById('messageInput').focus();

        // Clear chat messages when opening
        document.getElementById('chatMessages').innerHTML = '';

        // Reset flags when chat is opened
        askedForSuggestions = false;
        userWantsSuggestions = false;
        isQuickResponse = false;
    } else {
        chatBox.classList.add('hidden');
    }
}

function initializeChat() {
    if (connection) return;

    connection = new signalR.HubConnectionBuilder()
        .withUrl("/simpleChatHub")
        .build();

    connection.start().then(() => {
        console.log("Connected to chat");
        // Initial greeting without buttons
        addMessage("Chat Bot", "Hello! Welcome to our medical service. How can I assist you today?", new Date());

        // Ask if user wants suggestion buttons after a short delay
        setTimeout(() => {
            addMessage("Chat Bot", "Would you like me to show some quick suggestion buttons to help you get started?", new Date());
            askedForSuggestions = true;
        }, 1000);
    }).catch(err => console.error(err));

    connection.on("ReceiveMessage", (user, message, timestamp) => {
        // Don't add the message if it's a duplicate "No problem!" response
        if (!(user === "Chat Bot" && message.includes("No problem!") && askedForSuggestions === false)) {
            addMessage(user, message, new Date(timestamp));
        }

        // Check if user responded to the suggestion question (only if not a quick response)
        if (askedForSuggestions && user === "You" && !isQuickResponse) {
            const lowerMessage = message.toLowerCase();
            if (lowerMessage.includes('yes') || lowerMessage.includes('yeah') ||
                lowerMessage.includes('sure') || lowerMessage.includes('ok') ||
                lowerMessage.includes('please') || lowerMessage.includes('show')) {
                userWantsSuggestions = true;
                addQuickResponseButtons();
            } else if (lowerMessage.includes('no') || lowerMessage.includes('not') ||
                lowerMessage.includes('dont') || lowerMessage.includes("don't")) {
                userWantsSuggestions = false;
                // Only add this message if it's not already being added by the server
                if (!message.toLowerCase().includes("no problem")) {
                    addMessage("Chat Bot", "No problem! Feel free to type your question anytime.", new Date());
                }
            }
            askedForSuggestions = false;
        }

        // Reset the quick response flag
        isQuickResponse = false;

        // Add quick response buttons after bot messages if user wants them
        if (user !== "You" && userWantsSuggestions && !message.includes("No problem!")) {
            setTimeout(addQuickResponseButtons, 300);
        }
    });
}

function sendMessage() {
    const input = document.getElementById('messageInput');
    const message = input.value.trim();

    if (message && connection) {
        const userName = "You";

        // Send to hub
        connection.invoke("SendMessage", userName, message).catch(err => console.error(err));

        // Also trigger bot response with isQuickResponse flag set to false
        connection.invoke("SendBotResponse", userName, message, false).catch(err => console.error(err));

        input.value = '';

        // Remove quick response buttons when user sends a message
        removeQuickResponseButtons();
    }
}

function sendQuickResponse(message) {
    if (connection) {
        const userName = "You";

        // Set flag to indicate this is a quick response
        isQuickResponse = true;

        // Send to hub
        connection.invoke("SendMessage", userName, message).catch(err => console.error(err));

        // Also trigger bot response with isQuickResponse flag set to true
        connection.invoke("SendBotResponse", userName, message, true).catch(err => console.error(err));

        // Remove quick response buttons
        removeQuickResponseButtons();
    }
}

function addMessage(user, message, timestamp) {
    const messagesContainer = document.getElementById('chatMessages');
    const messageDiv = document.createElement('div');

    messageDiv.className = `message ${user === 'You' ? 'user' : 'bot'}`;
    messageDiv.innerHTML = `
        <strong>${user}:</strong> ${message}
        <div class="message-time" style="font-size: 0.8em; opacity: 0.7;">
            ${timestamp.toLocaleTimeString()}
        </div>
    `;

    messagesContainer.appendChild(messageDiv);
    messagesContainer.scrollTop = messagesContainer.scrollHeight;
}

function addQuickResponseButtons() {
    // Don't add buttons if user doesn't want them
    if (!userWantsSuggestions) return;

    // Remove any existing buttons first
    removeQuickResponseButtons();

    const quickResponses = [
        "Book an appointment",
        "Doctor availability",
        "Prescription refill",
        "Location & hours",
        "Billing question",
        "Test results",
        "Contact information",
        "Emergency help"
    ];

    const buttonsContainer = document.createElement('div');
    buttonsContainer.id = 'quickResponseButtons';
    buttonsContainer.className = 'quick-response-buttons';

    // Add title above buttons
    const title = document.createElement('div');
    title.className = 'quick-response-title';
    title.textContent = 'Quick suggestions:';
    buttonsContainer.appendChild(title);

    quickResponses.forEach(response => {
        const button = document.createElement('button');
        button.className = 'quick-response-btn';
        button.textContent = response;

        // Add data attribute for emergency button
        if (response === "Emergency help") {
            button.setAttribute('data-emergency', 'true');
        }

        button.onclick = () => sendQuickResponse(response);
        buttonsContainer.appendChild(button);
    });

    const messagesContainer = document.getElementById('chatMessages');
    messagesContainer.appendChild(buttonsContainer);
    messagesContainer.scrollTop = messagesContainer.scrollHeight;
}

function removeQuickResponseButtons() {
    const existingButtons = document.getElementById('quickResponseButtons');
    if (existingButtons) {
        existingButtons.remove();
    }
}

// Allow Enter key to send message
document.addEventListener('DOMContentLoaded', function () {
    document.getElementById('messageInput')?.addEventListener('keypress', function (e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            sendMessage();
        }
    });
});