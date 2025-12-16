// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener("DOMContentLoaded", () => {
    const dropdown = document.querySelector('[data-dropdown="currency"]');
    if (!dropdown) return;

    const button = dropdown.querySelector(".currency-button");
    const menu = dropdown.querySelector(".currency-menu");
    const items = Array.from(dropdown.querySelectorAll(".currency-item"))
        .filter(a => a.getAttribute("aria-disabled") !== "true");

    const open = () => {
        dropdown.classList.add("is-open");
        button.setAttribute("aria-expanded", "true");
    };

    const close = () => {
        dropdown.classList.remove("is-open");
        button.setAttribute("aria-expanded", "false");
    };

    const toggle = () => {
        const isOpen = dropdown.classList.contains("is-open");
        isOpen ? close() : open();
    };

    const focusItem = (index) => {
        if (!items.length) return;
        const i = (index + items.length) % items.length;
        items[i].focus();
    };

    // Toggle click
    button.addEventListener("click", (e) => {
        e.preventDefault();
        toggle();
    });

    // Close on outside click
    document.addEventListener("click", (e) => {
        if (!dropdown.contains(e.target)) close();
    });

    // Close on Escape
    document.addEventListener("keydown", (e) => {
        if (e.key === "Escape") {
            close();
            button.focus();
        }
    });

    // Keyboard navigation
    dropdown.addEventListener("keydown", (e) => {
        const isOpen = dropdown.classList.contains("is-open");

        if (e.key === "ArrowDown") {
            e.preventDefault();
            if (!isOpen) open();
            const idx = items.indexOf(document.activeElement);
            focusItem(idx === -1 ? 0 : idx + 1);
        }

        if (e.key === "ArrowUp") {
            e.preventDefault();
            if (!isOpen) open();
            const idx = items.indexOf(document.activeElement);
            focusItem(idx === -1 ? items.length - 1 : idx - 1);
        }

        if (e.key === "Enter" && isOpen && document.activeElement?.classList?.contains("currency-item")) {
            // Let the link navigate naturally
            close();
        }

        if (e.key === "Tab" && isOpen) {
            // If tabbing out of the dropdown, close it
            // small delay so focus can move first
            setTimeout(() => {
                if (!dropdown.contains(document.activeElement)) close();
            }, 0);
        }
    });

    // If menu opens, focus first available item
    button.addEventListener("keydown", (e) => {
        if (e.key === "ArrowDown") {
            e.preventDefault();
            open();
            focusItem(0);
        }
    });
});