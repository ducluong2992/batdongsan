document.getElementById("chat-button").onclick = () => {
    const box = document.getElementById("chatbox");
    box.style.display = (box.style.display === "flex") ? "none" : "flex";
};

document.getElementById("send-btn").onclick = sendMessage;
document.getElementById("chat-input").addEventListener("keypress", e => {
    if (e.key === "Enter") sendMessage();
});

function addMessage(text, type) {
    const box = document.getElementById("chat-messages");
    const div = document.createElement("div");
    div.className = type === "user" ? "msg-user" : "msg-ai";
    div.innerText = text;
    box.appendChild(div);
    box.scrollTop = box.scrollHeight;
}

function sendMessage() {
    const input = document.getElementById("chat-input");
    const text = input.value.trim();
    if (!text) return;

    addMessage(text, "user");
    input.value = "";

    fetch("/Ai/Chat", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ message: text })
    })
        .then(res => res.json())
        .then(data => {
            addMessage(data.reply, "ai");
        });
}
