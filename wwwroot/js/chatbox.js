
let isFirstOpen = true;

const chatBox = document.getElementById("chatbox");
const chatButton = document.getElementById("chat-button");
const chatClose = document.getElementById("chat-close");
const sendBtn = document.getElementById("send-btn");
const chatInput = document.getElementById("chat-input");

/* ===== MỞ CHAT ===== */
chatButton.onclick = () => {
    chatBox.style.display = "flex";

    // AI chào 1 lần duy nhất
    if (isFirstOpen) {
        addMessage(
            "Em chào Anh/Chị 👋\n" +
            "Em là trợ lý ảo của HHL RealEstate.\n" +
            "Em có thể hỗ trợ tìm tin nhà đất, hướng dẫn đăng tin hoặc tư vấn bất động sản tổng quát.\n" +
            "Anh/Chị cần em hỗ trợ gì ạ?",
            "ai"
        );
        isFirstOpen = false;
    }
};

/* ===== ĐÓNG CHAT (DẤU -) ===== */
chatClose.onclick = () => {
    chatBox.style.display = "none";
};

/* ===== GỬI TIN ===== */
sendBtn.onclick = sendMessage;
chatInput.addEventListener("keypress", e => {
    if (e.key === "Enter") sendMessage();
});

/* ===== HIỂN THỊ TIN NHẮN ===== */
function addMessage(text, type) {
    const box = document.getElementById("chat-messages");
    const div = document.createElement("div");
    div.className = type === "user" ? "msg-user" : "msg-ai";
    div.innerText = text;
    box.appendChild(div);
    box.scrollTop = box.scrollHeight;
}

/* ===== GỬI LÊN AI ===== */
function sendMessage() {
    const text = chatInput.value.trim();
    if (!text) return;

    addMessage(text, "user");
    chatInput.value = "";

    // Hiệu ứng AI đang nhập
    const typing = document.createElement("div");
    typing.className = "msg-ai";
    typing.innerText = "Trợ lý đang nhập...";
    document.getElementById("chat-messages").appendChild(typing);

    fetch("/Ai/Chat", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ message: text })
    })
        .then(res => res.json())
        .then(data => {
            typing.remove();
            addMessage(
                data.reply || "Em chưa hiểu rõ, Anh/Chị có thể hỏi lại giúp em không ạ?",
                "ai"
            );
        })
        .catch(() => {
            typing.remove();
            addMessage(
                "Hệ thống đang bận, Anh/Chị vui lòng thử lại sau ạ.",
                "ai"
            );
        });
}
