// Chat functionality
let conversationHistory = [];

document.addEventListener('DOMContentLoaded', function() {
    const chatInput = document.getElementById('chatInput');
    const sendButton = document.getElementById('sendButton');
    
    if (sendButton) {
        sendButton.addEventListener('click', sendMessage);
    }
    
    if (chatInput) {
        chatInput.addEventListener('keypress', function(e) {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                sendMessage();
            }
        });
    }
});

function sendExample(message) {
    document.getElementById('chatInput').value = message;
    sendMessage();
}

async function sendMessage() {
    const chatInput = document.getElementById('chatInput');
    const chatMessages = document.getElementById('chatMessages');
    const sendButton = document.getElementById('sendButton');
    
    const message = chatInput.value.trim();
    
    if (!message) {
        return;
    }
    
    // Add user message to UI
    addMessageToUI('user', message);
    
    // Clear input
    chatInput.value = '';
    
    // Disable send button
    sendButton.disabled = true;
    sendButton.textContent = 'Sending...';
    
    try {
        const response = await fetch('/api/chat', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                message: message,
                history: conversationHistory
            })
        });
        
        const data = await response.json();
        
        if (data.success) {
            // Add assistant response to UI
            addMessageToUI('assistant', data.message);
            
            // Update conversation history
            conversationHistory.push({ role: 'user', content: message });
            conversationHistory.push({ role: 'assistant', content: data.message });
        } else {
            addMessageToUI('assistant', 'Sorry, I encountered an error: ' + (data.error || 'Unknown error'));
        }
    } catch (error) {
        addMessageToUI('assistant', 'Sorry, I could not connect to the chat service. Please try again.');
        console.error('Chat error:', error);
    } finally {
        // Re-enable send button
        sendButton.disabled = false;
        sendButton.textContent = 'Send';
    }
}

function addMessageToUI(role, content) {
    const chatMessages = document.getElementById('chatMessages');
    
    const messageDiv = document.createElement('div');
    messageDiv.className = `chat-message ${role}`;
    
    const contentDiv = document.createElement('div');
    contentDiv.className = 'message-content';
    
    if (role === 'assistant') {
        contentDiv.innerHTML = '<strong>AI Assistant:</strong> ' + formatMessage(content);
    } else {
        contentDiv.innerHTML = '<strong>You:</strong> ' + escapeHtml(content);
    }
    
    messageDiv.appendChild(contentDiv);
    chatMessages.appendChild(messageDiv);
    
    // Scroll to bottom
    chatMessages.scrollTop = chatMessages.scrollHeight;
}

function formatMessage(text) {
    // Escape HTML first
    text = escapeHtml(text);
    
    // Convert markdown-style formatting
    // Bold
    text = text.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');
    
    // Line breaks
    text = text.replace(/\n/g, '<br>');
    
    // Lists (simple detection)
    text = text.replace(/^(\d+)\.\s+(.*)$/gm, '<li>$2</li>');
    text = text.replace(/^[\*\-]\s+(.*)$/gm, '<li>$1</li>');
    
    // Wrap consecutive <li> elements in <ul>
    text = text.replace(/(<li>.*<\/li>(?:\s*<li>.*<\/li>)*)/g, '<ul>$1</ul>');
    
    return text;
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}
