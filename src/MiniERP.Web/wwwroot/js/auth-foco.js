// Foco de luz del panel de marca del login.
//
// Lo unico que hace este guion es decirle al CSS donde esta el puntero. El movimiento,
// el retardo y el aspecto son de la hoja de estilos: aqui no se anima nada.
//
// El oyente va sobre el documento y no sobre el panel, por dos razones: el panel
// aparece y desaparece con la navegacion mejorada de Blazor, y asi el foco tambien
// reacciona mientras se escribe en el formulario, que esta del otro lado.
(() => {
    'use strict';

    // La navegacion mejorada puede reinsertar la etiqueta <script>, pero un solo
    // oyente sobre el documento sirve para toda la sesion.
    if (window.__miniErpFocoAuth) {
        return;
    }
    window.__miniErpFocoAuth = true;

    // Sin raton no hay nada que perseguir: en un telefono se queda la animacion sola.
    const punteroFino = window.matchMedia('(pointer: fine)');

    let pendiente = false;
    let ultimoX = 0;
    let ultimoY = 0;

    function colocar() {
        pendiente = false;

        const marca = document.querySelector('.auth-brand');
        if (!marca) {
            return;
        }

        const caja = marca.getBoundingClientRect();
        if (caja.width === 0 || caja.height === 0) {
            return;
        }

        // Se limita a los bordes del panel. Con el cursor sobre el formulario —que esta
        // a la derecha— el foco se queda pegado al borde en vez de salirse y desaparecer.
        const x = Math.min(Math.max(ultimoX - caja.left, 0), caja.width);
        const y = Math.min(Math.max(ultimoY - caja.top, 0), caja.height);

        marca.classList.add('con-puntero');
        marca.style.setProperty('--px', x + 'px');
        marca.style.setProperty('--py', y + 'px');
    }

    document.addEventListener('pointermove', (evento) => {
        if (!punteroFino.matches) {
            return;
        }

        ultimoX = evento.clientX;
        ultimoY = evento.clientY;

        // Se escribe una vez por cuadro: pointermove llega mucho mas seguido que eso,
        // y tocar el estilo en cada uno seria trabajo tirado.
        if (!pendiente) {
            pendiente = true;
            requestAnimationFrame(colocar);
        }
    }, { passive: true });
})();
