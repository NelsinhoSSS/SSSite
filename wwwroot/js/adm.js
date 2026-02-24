// wwwroot/js/adm.js

// 1. Funções de Controle do Modal
function openSecretModal() {
    const modal = document.getElementById('secretModal');
    if (modal) {
        modal.style.display = 'flex';
        const input = document.getElementById('secretPass');
        if (input) input.focus();
    }
}

function closeSecretModal() {
    const modal = document.getElementById('secretModal');
    if (modal) {
        modal.style.display = 'none';
        const errorMsg = document.getElementById('secretError');
        if (errorMsg) errorMsg.style.display = 'none';
    }
}

// 2. Lógica de Autenticação
function checkSecret() {
    const passInput = document.getElementById('secretPass');
    const pass = passInput ? passInput.value : "";

    if (pass === "newgods") { //senha
        sessionStorage.setItem('isAdmin', 'true');
        window.location.reload();
    } else {
        const errorMsg = document.getElementById('secretError');
        if (errorMsg) errorMsg.style.display = 'block';
        if (passInput) passInput.value = '';
    }
}

// 3. Gerenciamento de Status (Visual e Mensagens)
function updateAdmStatus() {
    const isAdmin = sessionStorage.getItem('isAdmin') === 'true';

    // Atualiza o botão ??? para Verde Neon
    const btn = document.getElementById('mysteryBtn');
    if (isAdmin && btn) {
        btn.classList.add('adm-active');
        btn.innerText = "!!!";
    }

    // Se estiver no Lobby, mostra a mensagem "BEM VINDO THE UR DRAGON"
    const lobbyMsg = document.getElementById('admWelcome');
    if (isAdmin && lobbyMsg) {
        lobbyMsg.style.display = 'block';
    }
}

// Inicializa quando o DOM estiver pronto
document.addEventListener('DOMContentLoaded', updateAdmStatus);

// Atalho de teclado: ESC para fechar o modal
window.onkeydown = function (event) {
    if (event.keyCode == 27) closeSecretModal();
};